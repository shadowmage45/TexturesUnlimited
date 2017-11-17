Shader "SSTU/PBR/Solar"
{
	Properties 
	{
		_MainTex("_MainTex (RGB)", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_MetallicGlossMap("_MetallicGlossMap (RGB)", 2D) = "white" {}
		_BumpMap("_BumpMap (NRM)", 2D) = "bump" {}
		_AOMap("_AOMap (Grayscale)", 2D) = "white" {}
		_Emissive("Emission", 2D) = "black" {}
        _Thickness("Thickness (RGB)", 2D) = "black" {}
        _SubSurfAmbient("SubSurf Ambient", Range(0, 1)) = 0
        _SubSurfScale("SubSurf Scale", Range(0, 10)) = 1
        _SubSurfPower("SubSurf Falloff Power", Range(0, 10)) = 1
        _SubSurfDistort("SubSurf Distortion", Range(0, 1)) = 0
        _SubSurfAtten("SubSurf Attenuation", Range(0, 1)) = 1
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

		#pragma surface surf Standard2 keepalpha
		#pragma target 3.0
        #include "HLSLSupport.cginc"
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"
        #include "UnityPBSLighting.cginc"
        #include "SSTUShaders.cginc"
				
		sampler2D _MainTex;
		sampler2D _Emissive;
		sampler2D _Thickness;
		sampler2D _MetallicGlossMap;
		sampler2D _BumpMap;		
		sampler2D _AOMap;

		float _Opacity;
		float4 _Color;
		float4 _EmissiveColor;
		float4 _TemperatureColor;
		float4 _RimColor;
		float _RimFalloff;
		
        float _SubSurfAmbient;
		float _SubSurfScale;
		float _SubSurfPower;
		float _SubSurfDistort;
		float _SubSurfAtten;
		
		struct Input
		{
			float2 uv_MainTex;
			float3 viewDir;
		};

		struct SurfaceOutputStandard2
        {
            fixed3 Albedo;		// base (diffuse or specular) color
            fixed3 Normal;		// tangent space normal, if written
            half3 Emission;
            half4 Backlight;	// backlight emissive glow color(RGB) and ambient light value (A)
			half4 SubSurfParams;// subsurface scattering parameters R = Scale, G = Power, B = Scale, A = Attenuation
            half Metallic;		// 0=non-metal, 1=metal
            half Smoothness;	// 0=rough, 1=smooth
            half Occlusion;		// occlusion (default 1)
            fixed Alpha;		// alpha for transparencies
        };
        
        inline half4 LightingStandard2(SurfaceOutputStandard2 s, half3 viewDir, UnityGI gi)
        {
            s.Normal = normalize(s.Normal);			            
            //SSS implementation from:  https://colinbarrebrisebois.com/2011/03/07/gdc-2011-approximating-translucency-for-a-fast-cheap-and-convincing-subsurface-scattering-look/
            
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
            half3 specColor;
            s.Albedo = DiffuseAndSpecularFromMetallic (s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

            //shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
            //this is necessary to handle transparency in physically correct way - only diffuse component gets affected by alpha
            half outputAlpha;
            s.Albedo = PreMultiplyAlpha (s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);

            half4 c = UNITY_BRDF_PBS (s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
            c.rgb += UNITY_BRDF_GI (s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, s.Occlusion, gi);
            c.rgb += backColor;
            c.a = outputAlpha;
            return c;
        }
        
        inline void LightingStandard2_GI (SurfaceOutputStandard2 s, UnityGIInput data, inout UnityGI gi)
        {
            UNITY_GI(gi, s, data);
        }
		
		void surf (Input IN, inout SurfaceOutputStandard2 o)
		{
			fixed4 color = tex2D(_MainTex,(IN.uv_MainTex));
			fixed4 spec = tex2D(_MetallicGlossMap, (IN.uv_MainTex));
			fixed3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
			fixed4 ao = tex2D(_AOMap, (IN.uv_MainTex));
			fixed4 glow = tex2D(_Emissive, (IN.uv_MainTex));
            fixed4 thick = tex2D(_Thickness, (IN.uv_MainTex));
			
			o.Albedo = color.rgb * _Color.rgb;
			o.Normal = normal;
            o.Backlight.rgb = thick.rgb;
            o.Backlight.a = _SubSurfAmbient;
			o.Emission = glow.rgb * glow.aaa * _EmissiveColor.rgb *_EmissiveColor.aaa + stockEmit(IN.viewDir, normal, _RimColor, _RimFalloff, _TemperatureColor) * _Opacity;
			o.Metallic = spec.r;
			o.Smoothness = spec.a;
			o.Occlusion = ao.r;
			o.SubSurfParams = half4(_SubSurfScale, _SubSurfPower, _SubSurfDistort, _SubSurfAtten);
			o.Alpha = _Opacity;
		}
		ENDCG
	}
	Fallback "Bumped Specular"
}