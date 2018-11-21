Shader "TU/Transparent/Metallic"
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
		_DetailMult("Recoloring Detail Multiplier", Vector)=(1,1,1,0)
		
		[Header(Subsurface Scattering Parameters)]
		_SubSurfAmbient("SubSurf Ambient", Range(0, 1)) = 0
		_SubSurfScale("SubSurf Scale", Range(0, 10)) = 1
		_SubSurfPower("SubSurf Falloff Power", Range(0, 10)) = 1
		_SubSurfDistort("SubSurf Distortion", Range(0, 1)) = 0
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
		Tags {"Queue"="Transparent" "RenderType"="Transparent"}
		ZWrite Off
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM

		//directives for 'surface shader' 'surface name = 'TU'' and 'don't discard alpha values'
		#pragma surface surf TU keepalpha
		#pragma target 3.0
		//#pragma skip_variants POINT POINT_COOKIE DIRECTIONAL_COOKIE //need to find out what variants are -actually- used...
		//#pragma multi_compile_fwdadd_fullshadows //stalls out Unity Editor while compiling shader....
		//subsurface scattering toggle
		#pragma multi_compile __ TU_SUBSURF
		//specular input source toggle
		#pragma multi_compile __ TU_STOCK_SPEC
		#pragma multi_compile __ TU_RECOLOR
		#pragma multi_compile __ TU_RECOLOR_NORM TU_RECOLOR_INPUT TU_RECOLOR_NORM_INPUT
		
		#define TU_SURF_MET 1
		#define TU_LIGHT_METAL 1
		#define TU_TRANSPARENT 1
		
		#include "HLSLSupport.cginc"
		#include "UnityCG.cginc"
		#include "Lighting.cginc"
		#include "AutoLight.cginc"
		#include "UnityPBSLighting.cginc"
		#include "TU-Include-Functions.cginc"
		#include "TU-Include-Structs.cginc"
		#include "TU-Include-Lighting.cginc"
		#include "TU-Include-Surfaces.cginc"
				
		ENDCG
	}
	Fallback "Standard"
	CustomEditor "TUMetallicUI"
}