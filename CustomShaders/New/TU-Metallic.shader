Shader "TU/Metallic"
{
	Properties 
	{
		//texture input slots
		_MainTex("_MainTex (RGB)", 2D) = "white" {}
		_MetallicGlossMap("_MetallicGlossMap (RGB)", 2D) = "white" {}
		_MaskTex("_MaskTex (Grayscale)", 2D) = "black" {}
		_BumpMap("_BumpMap (NRM)", 2D) = "bump" {}
		_AOMap("_AOMap (Grayscale)", 2D) = "white" {}
		_Emissive("_Emission", 2D) = "black" {}
		_Thickness("_Thickness", 2D) = "black" {}
		
		//recoloring input color values
		_MaskColor1 ("Mask Color 1", Color) = (1,1,1,1)
		_MaskColor2 ("Mask Color 2", Color) = (1,1,1,1)
		_MaskColor3 ("Mask Color 3", Color) = (1,1,1,1)
		_MaskMetallic ("Mask Metals", Vector) = (0,0,0,1)
		
		//sub-surface scattering shader parameters		
        _SubSurfAmbient("SubSurf Ambient", Range(0, 1)) = 0
        _SubSurfScale("SubSurf Scale", Range(0, 10)) = 1
        _SubSurfPower("SubSurf Falloff Power", Range(0, 10)) = 1
        _SubSurfDistort("SubSurf Distortion", Range(0, 1)) = 0
        _SubSurfAtten("SubSurf Attenuation", Range(0, 1)) = 1
		
		//stock KSP compatibility properties -- used for emission/glow, part-highlighting, part-thermal overlay, and part 'burn' discoloring
		_EmissiveColor("EmissionColor", Color) = (0,0,0)
		_Opacity("Emission Opacity", Range(0,1) ) = 1
		_RimFalloff("_RimFalloff", Range(0.01,5) ) = 0.1
		_RimColor("_RimColor", Color) = (0,0,0,0)
		_TemperatureColor("_TemperatureColor", Color) = (0,0,0,0)
		_BurnColor ("Burn Color", Color) = (1,1,1,1)
	}
	
	SubShader
	{
		Tags {"RenderType"="Opaque"}
		ZWrite On
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM

		#pragma surface surf TU keepalpha
		#pragma target 3.0
		#pragma multi_compile __ TU_EMISSIVE
		//#pragma multi_compile __ TU_BUMPMAP
		#pragma multi_compile __ TU_RECOLOR
		#pragma multi_compile __ TU_SUBSURF
		#pragma multi_compile __ TU_STOCK_SPEC
		
        #include "HLSLSupport.cginc"
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"
        #include "UnityPBSLighting.cginc"
		#include "SSTUShaders.cginc"
		
		#define TU_BUMPMAP 1
		
		//Texture samplers for all possible texture slots
		sampler2D _MainTex;
		sampler2D _MaskTex;
		sampler2D _MetallicGlossMap;
		sampler2D _BumpMap;		
		sampler2D _AOMap;
		sampler2D _Emissive;
		sampler2D _Thickness;

		//standard KSP shader property values
		float _Opacity;
		float4 _EmissiveColor;
		float4 _TemperatureColor;
		float4 _RimColor;
		float _RimFalloff;
		
		//recoloring property values
		float4 _MaskColor1;
		float4 _MaskColor2;
		float4 _MaskColor3;
		float4 _MaskMetallic;
		
		//subsurf property values
        float _SubSurfAmbient;
		float _SubSurfScale;
		float _SubSurfPower;
		float _SubSurfDistort;
		float _SubSurfAtten;
		
		//basic trimmed-down input struct with only UVs for main texture and view direction; absolute minimum needed
		struct Input
		{
			float2 uv_MainTex;
			float3 viewDir;
		};
		
		//custom surface output struct used to enable subsurf lighting params
		struct SurfaceOutputTU
        {
            fixed3 Albedo;		// base (diffuse or specSampleular) color
			fixed3 Normal;		// tangent space normal, if written
			half3 Emission;		// emissive / glow color
            half Metallic;		// 0=non-metal, 1=metal
            half Smoothness;	// 0=rough, 1=smooth
			half Occlusion;		// occlusion (default 1)
            fixed Alpha;		// alpha for transparencies
			half4 Backlight;	// backlight emissive glow color(RGB) and ambient light value (A)
			half4 SubSurfParams;// subsurface scattering parameters R = Scale, G = Power, B = Scale, A = Attenuation
        };
		
		//replacement for Unity bridge method to call GI with custom structs
        inline void LightingTU_GI (SurfaceOutputTU s, UnityGIInput data, inout UnityGI gi)
        {
            UNITY_GI(gi, s, data);
        }	
        
		//custom lighting function to enable SubSurf functionality
        inline half4 LightingTU(SurfaceOutputTU s, half3 viewDir, UnityGI gi)
        {
			#if TU_SUBSURF			            
				//SSS implementation from:  https://colinbarrebrisebois.com/2011/03/07/gdc-2011-approximating-translucency-for-a-fast-cheap-and-convincing-subsurface-scattering-look/
				
				s.Normal = normalize(s.Normal);
				half fLTScale = s.SubSurfParams.r;//main output scalar
				half iLTPower = s.SubSurfParams.g;//exponent used in power
				half fLTDistortion = s.SubSurfParams.b;//how much the surface normal distorts the outgoing light
				half fLightAttenuation = s.SubSurfParams.a;//how much light attenuates while traveling through the surface (gets multiplied by distance)            
				half fLTAmbient = s.Backlight.a;//ambient from texture/material
				half3 fLTThickness = s.Backlight.rgb;//sampled from texture
				
				float3 H = normalize(gi.light.dir + s.Normal * fLTDistortion);
				float vdh = pow(saturate(dot(viewDir, -H)), iLTPower) * fLTScale;
				float3 I = fLightAttenuation * (vdh + fLTAmbient) * fLTThickness;
				half3 backColor = I * gi.light.color;			
				
				half oneMinusReflectivity;
				half3 specSampleColor;
				s.Albedo = DiffuseAndSpecularFromMetallic(s.Albedo, s.Metallic, /*out*/ specSampleColor, /*out*/ oneMinusReflectivity);

				//shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
				//this is necessary to handle transparency in physically correct way - only diffuse component gets affected by alpha
				
				half outputAlpha;
				s.Albedo = PreMultiplyAlpha (s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);

				half4 c = UNITY_BRDF_PBS (s.Albedo, specSampleColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
				c.rgb += UNITY_BRDF_GI (s.Albedo, specSampleColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, s.Occlusion, gi);
				c.rgb += backColor;
				c.a = outputAlpha;
				return c;
			#else
				//Unity 'Standard' lighting function, unabridged
				s.Normal = normalize(s.Normal);
				half oneMinusReflectivity;
				half3 specSampleColor;
				s.Albedo = DiffuseAndSpecularFromMetallic(s.Albedo, s.Metallic, /*out*/ specSampleColor, /*out*/ oneMinusReflectivity);
				half outputAlpha;
				s.Albedo = PreMultiplyAlpha (s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);
				half4 c = UNITY_BRDF_PBS (s.Albedo, specSampleColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
				c.rgb += UNITY_BRDF_GI (s.Albedo, specSampleColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, s.Occlusion, gi);
				c.a = outputAlpha;
				return c;
			#endif
        }
		
		void surf (Input IN, inout SurfaceOutputTU o)
		{
			//standard texture samplers used regardless of keywords...
			fixed4 color = tex2D(_MainTex,(IN.uv_MainTex));			
			fixed4 specSample = tex2D(_MetallicGlossMap, (IN.uv_MainTex));
			
			//metal ALWAYS comes from MetallicGlossMap.r channel
			fixed metal = specSample.r;
			
			//if 'stock specular' mode is enabled, pull spec value from alpha channel of diffuse shader
			//else pull it from the alpha channel of the metallic gloss map
			#if TU_STOCK_SPEC
				fixed smooth = color.a;
			#else
				fixed smooth = specSample.a;
			#endif
			
			#if TU_BUMPMAP			
				fixed3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
				o.Normal = normal;
			#else				
				fixed3 normal = fixed3(0,0,1);
			#endif
			
			#if TU_RECOLOR
				fixed3 mask = tex2D(_MaskTex, (IN.uv_MainTex));
				fixed m = saturate(1 - (mask.r + mask.g + mask.b));
				fixed3 userColor = mask.rrr * _MaskColor1.rgb + mask.ggg * _MaskColor2.rgb + mask.bbb * _MaskColor3.rgb;
				fixed3 userspecSample = mask.r * _MaskColor1.a + mask.g * _MaskColor2.a + mask.b * _MaskColor3.a;
				fixed userMetallic = mask.r * _MaskMetallic.r + mask.g * _MaskMetallic.g + mask.b * _MaskMetallic.b;
					
				fixed3 diffuseColor = color.rgb * m;
				fixed3 basespecSample = specSample.rgb * m;
				fixed baseMetallic = specSample.a * m;
						
				#if TINTING_MODE
					fixed3 detailColor = color.rgb * (1 - m);
					fixed3 detailspecSample = specSample.rgb * (1 - m);
					fixed detailMetallic = specSample.a * (1 - m);
					o.Albedo = saturate(userColor * detailColor + diffuseColor);
					o.Smoothness = saturate(userspecSample * detailspecSample + basespecSample).r;
					o.Metallic = saturate(userMetallic * detailMetallic + baseMetallic);
				#else
					fixed3 detailColor = (color.rgb - 0.5) * (1 - m);
					fixed3 detailspecSample = (specSample.rgb - 0.5) * (1 - m);
					fixed detailMetallic = (specSample.a - 0.5) * (1 - m);
					o.Albedo = saturate(userColor + diffuseColor + detailColor);
					o.Smoothness = saturate(userspecSample + basespecSample + detailspecSample).r;
					o.Metallic = saturate(userMetallic + baseMetallic + detailMetallic);
				#endif
			#else
				o.Albedo = color.rgb;
				o.Smoothness = specSample.a;
				o.Metallic = specSample.r;
			#endif
			
			#if TU_SUBSURF
				fixed4 thick = tex2D(_Thickness, (IN.uv_MainTex));
				o.Backlight.rgb = thick.rgb;
				o.Backlight.a = _SubSurfAmbient;
				o.SubSurfParams = half4(_SubSurfScale, _SubSurfPower, _SubSurfDistort, _SubSurfAtten);
			#endif
			
			#if TU_AOMAP
				fixed4 ao = tex2D(_AOMap, (IN.uv_MainTex));
				o.Occlusion = ao.r;
			#endif
			
			#if TU_EMISSIVE
				fixed4 glow = tex2D(_Emissive, (IN.uv_MainTex));
				o.Emission = glow.rgb * glow.aaa * _EmissiveColor.rgb *_EmissiveColor.aaa + stockEmit(IN.viewDir, normal, _RimColor, _RimFalloff, _TemperatureColor) * _Opacity;
			#endif
			
			o.Alpha = _Opacity;
		}
		ENDCG
	}
	Fallback "Bumped specSampleular"
}