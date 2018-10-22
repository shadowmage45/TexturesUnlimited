Shader "TU/Transparent/Legacy"
{
	Properties 
	{
		//standard texture input slots
		_MainTex("_MainTex (RGB)", 2D) = "white" {}
		_SpecGlossMap("_SpecGlossMap (RGBA)", 2D) = "white" {}
		_BumpMap("_BumpMap (NRM)", 2D) = "bump" {}
		_AOMap("_AOMap (Grayscale)", 2D) = "white" {}
		_Emissive("_Emission (RGB Emissive Map)", 2D) = "black" {}
		_Thickness("_Thickness (RGB Subsurf Thickness) ", 2D) = "white" {}
		
		//recoloring texture input slots
		_MaskTex("_MaskTex (RGB Color Mask)", 2D) = "black" {}
		_SpecGlossNormMask("_SpecGlossNormMask", 2D) = "black" {}
		_SpecGlossInputMask("_SpecGlossInputMask", 2D) = "white" {}
		
		[Header(Normal Map Correction)]
		_NormalFlipX("Normal X Flip", Range(-1, 1)) = 1
		_NormalFlipY("Normal Y Flip", Range(-1, 1)) = 1
		
		//detail textures -- diff/met/nrm??
		
		//standard shader params
		_Color ("_Color", Color) = (1,1,1)
		_GlossColor ("_GlossColor", Color) = (1,1,1)
		_Smoothness ("_Smoothness", Range(0,1)) = 1
		
		//recoloring input color values
		_MaskColor1 ("Mask Color 1", Color) = (1,1,1,1)
		_MaskColor2 ("Mask Color 2", Color) = (1,1,1,1)
		_MaskColor3 ("Mask Color 3", Color) = (1,1,1,1)
		_MaskMetallic ("Mask Metals", Vector) = (0,0,0,0)
		
		_DiffuseNorm("Diffuse Normalization", Vector) = (1,1,1,0)
		_SpecularNorm("Specular Normalization", Vector) = (1,1,1,0)
		_SmoothnessNorm("Smoothness Normalization", Vector)=(1,1,1,0)
		
		//sub-surface scattering shader parameters		
		_SubSurfAmbient("SubSurf Ambient", Range(0, 1)) = 0
		_SubSurfScale("SubSurf Scale", Range(0, 10)) = 1
		_SubSurfPower("SubSurf Falloff Power", Range(0, 10)) = 1
		_SubSurfDistort("SubSurf Distortion", Range(0, 1)) = 0
		_SubSurfAtten("SubSurf Attenuation", Range(0, 1)) = 1
		
		//stock KSP compatibility properties -- used for emission/glow, part-highlighting, part-thermal overlay, and part 'burn' discoloring
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
		#pragma multi_compile __ TU_SUBSURF
		#pragma multi_compile TU_STD_SPEC TU_STOCK_SPEC
		#pragma multi_compile TU_RECOLOR_OFF TU_RECOLOR_STANDARD
		#pragma multi_compile __ TU_RECOLOR_NORM TU_RECOLOR_INPUT TU_RECOLOR_NORM_INPUT
		
		#define TU_LIGHT_SPECLEGACY 1
		#define TU_SURF_SPEC 1
		#define TU_TRANSPARENT 1
		
		#include "Lighting.cginc"
		#include "TU-Include-Functions.cginc"
		#include "TU-Include-Structs.cginc"
		#include "TU-Include-Lighting.cginc"
		#include "TU-Include-Surfaces.cginc"
				
		ENDCG
	}
	Fallback "Standard"
	CustomEditor "TUMetallicUI"
}