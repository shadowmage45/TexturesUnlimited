Shader "TU/MetallicDetail"
{
	Properties 
	{
        [Header(Standard Texture Maps)]
		_MainTex("_MainTex (RGB)", 2D) = "white" {}
		_MetallicGlossMap("_MetallicGlossMap (R Metal/A Gloss)", 2D) = "white" {}
		_BumpMap("_BumpMap (NRM)", 2D) = "bump" {}
		_AOMap("_AOMap (R Grayscale)", 2D) = "white" {}
		_Emissive("_Emission (RGB Emissive Map)", 2D) = "black" {}
		_Thickness("_Thickness (RGB Subsurf Thickness) ", 2D) = "white" {}				
        [Header(Recoloring Texture Maps)]
		_MaskTex("_MaskTex (RGB Color Mask)", 2D) = "red" {}
		_MetalGlossNormMask("_MetalGlossNormMask (R/A Normalization)", 2D) = "black" {}
		_MetalGlossInputMask("_MetalGlossInputMask (R/A Input Masking)", 2D) = "white" {}
		
		[Header(Normal Map Correction)]
		_NormalFlipX("Normal X Flip", Range(-1, 1)) = 1
		_NormalFlipY("Normal Y Flip", Range(-1, 1)) = 1
		
		//detail textures -- diff/met/nrm??
		[Header(Detail Maps)]
		_DetailArray("Tex", 2DArray) = "" {}
		_MaskDetail ("Mask Detail Selection", Vector) = (0,0,0,0)
		
        [Header(Color Multiplier Parameters)]
		_Color ("_Color", Color) = (1,1,1)
		_Metal ("_Metal", Range(0,1)) = 1
		_Smoothness ("_Smoothness", Range(0,1)) = 1
		
        [Header(Recoloring Input Parameters)]
		_MaskColor1 ("Mask Color 1", Color) = (1,1,1,1)
		_MaskColor2 ("Mask Color 2", Color) = (1,1,1,1)
		_MaskColor3 ("Mask Color 3", Color) = (1,1,1,1)
		_MaskMetallic ("Mask Metals", Vector) = (0,0,0,0)
		
        [Header(Recoloring Normalization Parameters)]
		_DiffuseNorm("Diffuse Normalization", Vector) = (1,1,1,0)
		_MetalNorm("Metallic Normalization", Vector) = (1,1,1,0)
		_SmoothnessNorm("Smoothness Normalization", Vector)=(1,1,1,0)
		
		[Header(Subsurface Scattering Parameters)]
		_SubSurfAmbient("SubSurf Ambient", Range(0, 1)) = 0
		_SubSurfScale("SubSurf Scale", Range(0, 10)) = 1
		_SubSurfPower("SubSurf Falloff Power", Range(0, 10)) = 1
		_SubSurfDistort("SubSurf Distortion", Range(-1, 1)) = 0
		_SubSurfAtten("SubSurf Attenuation", Range(0, 1)) = 1
		
		[Header(Stock KSP Parameters)]
		_EmissiveColor("EmissionColor", Color) = (0,0,0)
		_Opacity("Part Opacity", Range(0,1) ) = 1
		_RimFalloff("_RimFalloff", Range(0.01,5) ) = 0.1
		_RimColor("_RimColor", Color) = (0,0,0,0)
		_TemperatureColor("Temperature Color", Color) = (0,0,0,0)
		_BurnColor ("Burn Color", Color) = (1,1,1,1)
		_UnderwaterFogFactor ("Underwater Fog Factor", Range(0,1)) = 0
	}
	
	SubShader
	{
		Tags {"RenderType"="Opaque"}
		ZWrite On
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM

		//directives for 'surface shader' 'surface name = 'TU'' and 'don't discard alpha values'
		#pragma surface surf TU keepalpha
		#pragma target 3.5
		//#pragma require 2darray
		//#pragma skip_variants POINT POINT_COOKIE DIRECTIONAL_COOKIE //need to find out what variants are -actually- used...
		//#pragma multi_compile_fwdadd_fullshadows //stalls out Unity Editor while compiling shader....
		//subsurface scattering toggle
		#pragma multi_compile __ TU_SUBSURF
		//specular input source toggle
		#pragma multi_compile TU_STD_SPEC TU_STOCK_SPEC
		#pragma multi_compile TU_RECOLOR_OFF TU_RECOLOR_STANDARD
		#pragma multi_compile __ TU_RECOLOR_NORM TU_RECOLOR_INPUT TU_RECOLOR_NORM_INPUT
		
		#define TU_SURF_MET 1
		#define TU_LIGHT_METAL 1
		
		#include "HLSLSupport.cginc"
		#include "UnityCG.cginc"
		#include "Lighting.cginc"
		#include "AutoLight.cginc"
		#include "UnityPBSLighting.cginc"
		#include "TU-Include-Functions.cginc"
		#include "TU-Include-Structs.cginc"
		#include "TU-Include-Lighting.cginc"
		
        UNITY_DECLARE_TEX2DARRAY(_DetailArray);
		
		void surf (Input IN, inout SurfaceOutputTU o)
		{
			#if TU_ICON
				//as the clip test needs to be performed regardless of the surface properties, run it first as an early exit
				//should save some texture sampling and processing of data that would just be discarded anyway.
				float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
				
				#ifdef SHADER_API_GLCORE				
					screenUV.y = 1 - screenUV.y;
				#endif			
				
				if(screenUV.x < _MinX || screenUV.y < _MinY || screenUV.x > _MaxX || screenUV.y > _MaxY)
				{
					clip(-1);
					return;
				}
			#endif
			//standard texture samplers used regardless of keywords...
			fixed4 color = tex2D(_MainTex,(IN.uv_MainTex));
			fixed4 specSample = tex2D(_MetallicGlossMap, (IN.uv_MainTex));
			
			float3 normalDetail = float3(0,0,0);
			
			//metal ALWAYS comes from MetallicGlossMap.r
			fixed metal = specSample.r;
			
			//if 'stock specular' mode is enabled, pull spec value from alpha channel of diffuse shader
			//else pull it from the alpha channel of the metallic gloss map
			#if TU_STD_SPEC
				fixed smooth = specSample.a;
			#endif
			#if TU_STOCK_SPEC
				fixed smooth = color.a;
			#endif
			
			//new TU recolor mode based on normalization maps
			#if TU_RECOLOR_STANDARD
			
				//RGBA value from the mask; RGB = recoloring channels, A = diffuse luminance normalization data
				fixed4 mask = tex2D(_MaskTex, (IN.uv_MainTex));
				//
				fixed diffuseNorm = getUserValue(mask, _DiffuseNorm.x, _DiffuseNorm.y, _DiffuseNorm.z);
				fixed metallicNorm = getUserValue(mask, _MetalNorm.x, _MetalNorm.y, _MetalNorm.z);
				fixed specularNorm = getUserValue(mask, _SmoothnessNorm.x, _SmoothnessNorm.y, _SmoothnessNorm.z);
				
				//same for specular and metallic if normalization for those channels is enabled
				#if TU_RECOLOR_NORM || TU_RECOLOR_NORM_INPUT
					fixed4 specMetNormData = tex2D(_MetalGlossNormMask, (IN.uv_MainTex));
					diffuseNorm += mask.a;
					metallicNorm += specMetNormData.r;
					specularNorm += specMetNormData.a;
				#endif
				
				fixed metalMaskFactor = 1;
				fixed specMaskFactor = 1;
				
				//sample/calculate mix factors for user-specified spec and metal values if input-masking setting is enabled
				#if TU_RECOLOR_INPUT || TU_RECOLOR_NORM_INPUT
					fixed4 specMaskValues = tex2D(_MetalGlossInputMask, IN.uv_MainTex);
					metalMaskFactor = specMaskValues.r;
					specMaskFactor = specMaskValues.a;
				#endif
				
				fixed detailMapIndex = getUserValue(mask, 0, 1, 2);
				float3 uv = float3(0,0,0);
				uv.xy = IN.uv_MainTex;
				uv.z = detailMapIndex;
				normalDetail = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_DetailArray, uv));
				
				o.Albedo = recolorStandard(color.rgb, mask, diffuseNorm, _MaskColor1, _MaskColor2, _MaskColor3);
				o.Metallic = recolorStandard(metal, mask * metalMaskFactor, metallicNorm, _MaskMetallic.r, _MaskMetallic.g, _MaskMetallic.b);
				o.Smoothness = recolorStandard(smooth, mask * specMaskFactor, specularNorm, _MaskColor1.a, _MaskColor2.a, _MaskColor3.a);
				
			#endif			
			//no recoloring enabled -- use standard texture sampling -- use the values directly from the source textures
			#if TU_RECOLOR_OFF
				o.Albedo = color.rgb;
				o.Smoothness = smooth;
				o.Metallic = metal;
			#endif
			
			//If subsurf is enabled, this is a bit of pre-setup to pass params to lighting function
			#if TU_SUBSURF
				fixed4 thick = tex2D(_Thickness, (IN.uv_MainTex));
				o.Backlight.rgb = thick.rgb;
				//TODO -- can sample property in lighting function; no need to pass through alpha channel, save a register
				o.Backlight.a = _SubSurfAmbient;
			#endif
			
			//normal map always sampled and assigned directly to surface
			float3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
			normal.x *= _NormalFlipX;
			normal.y *= _NormalFlipY;
			o.Normal = normalize(normal + normalDetail);
			
			//ambient occlusion always sampled and assigned directly to surface
			fixed4 ao = tex2D(_AOMap, (IN.uv_MainTex));
			o.Occlusion = ao.g;
			
			//emission always sampled and assigned to surface along with stock part-highlighting functionality
			fixed4 glow = tex2D(_Emissive, (IN.uv_MainTex));
			o.Emission = glow.rgb * glow.aaa * _EmissiveColor.rgb *_EmissiveColor.aaa + stockEmit(IN.viewDir, normal, _RimColor, _RimFalloff, _TemperatureColor) * _Opacity;
			
			//controlled directly by shader property
			o.Alpha = _Opacity;
			#if TU_TRANSPARENT
				o.Alpha *= _Color.a * color.a;
			#endif
					
			//apply the standard shader param multipliers to the sampled/computed values.
			o.Albedo *= _Color.rgb;
			fixed4 fog = UnderwaterFog(IN.worldPos, o.Albedo);
			o.Albedo = fog.rgb;
			o.Emission *= fog.a;
			o.Metallic *= _Metal;
			o.Smoothness *= _Smoothness;
			
			
			//sample from and mix in details from detail textures from array
			//IDX0 = Main
			//IDX1 = Second
			//IDX2 = Third
			
			
		}
				
		ENDCG
	}
	Fallback "Standard"
	CustomEditor "TUMetallicUI"
}