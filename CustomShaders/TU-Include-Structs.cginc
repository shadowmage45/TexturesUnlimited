//varyings/uniforms for all shaders
//blocked out by keyword/feature-set/manual preprocessor defines

sampler2D _MainTex;
#if TU_SURF_MET
	sampler2D _MetallicGlossMap;
#endif
#if TU_SURF_SPEC
	sampler2D _SpecGlossMap;
#endif	
sampler2D _BumpMap;
sampler2D _AOMap;
sampler2D _Emissive;
#if TU_SUBSURF
	sampler2D _Thickness;
#endif

sampler2D _MaskTex;
#if TU_RECOLOR_NORM || TU_RECOLOR_NORM_INPUT
	#if TU_SURF_MET
		sampler2D _MetalGlossNormMask;
	#endif
	#if TU_SURF_SPEC
		sampler2D _SpecGlossNormMask;
	#endif
#endif
#if TU_RECOLOR_INPUT || TU_RECOLOR_NORM_INPUT
	#if TU_SURF_MET
		sampler2D _MetalGlossInputMask;
	#endif
	#if TU_SURF_SPEC
		sampler2D _SpecGlossInputMask;
	#endif
#endif

//standard shader params for adjusting color/etc
float4 _Color;
#if TU_SURF_MET
	float _Metal;
#endif
#if TU_SURF_SPEC
	float3 _GlossColor;
#endif
float _Smoothness;		

//standard KSP shader property values
float _Opacity;
float4 _EmissiveColor;
float4 _TemperatureColor;
float4 _RimColor;
float _RimFalloff;

//recoloring property values
#if TU_RECOLOR_STANDARD
	float4 _MaskColor1;
	float4 _MaskColor2;
	float4 _MaskColor3;
	float4 _MaskMetallic;
	float4 _Channel1Norm;
	float4 _Channel2Norm;
	float4 _Channel3Norm;
#endif

#if TU_SUBSURF
	//subsurf property values
	float _SubSurfAmbient;
	float _SubSurfScale;
	float _SubSurfPower;
	float _SubSurfDistort;
	float _SubSurfAtten;
#endif

#if TU_ICON
	half _Multiplier;
	float _MinX;
	float _MaxX;
	float _MinY;
	float _MaxY;
#endif

//input struct shared by all shaders
struct Input
{
	float2 uv_MainTex;
	float3 viewDir;
	float3 worldPos;
	#if TU_ICON
		float4 screenPos;
	#endif
};

//surface output shared by all shaders
struct SurfaceOutputTU
{
	fixed3 Albedo;		// base (diffuse or specSampleular) color
	fixed3 Normal;		// tangent space normal, if written
	half3 Emission;		// emissive / glow color
	half Smoothness;	// 0=rough, 1=smooth
	#if TU_SURF_MET
		half Metallic;
	#endif
	#if TU_SURF_SPEC
		half3 SpecularColor;// 0=specular color
	#endif
	half Occlusion;		// occlusion (default 1)
	fixed Alpha;		// alpha for transparencies
	#if TU_SUBSURF
		half4 Backlight;	// backlight emissive glow color(RGB) and ambient light value (A)
	#endif
};