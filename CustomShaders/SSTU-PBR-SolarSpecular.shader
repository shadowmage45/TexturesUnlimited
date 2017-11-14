Shader "SSTU/PBR/SolarSpecular"
{
	Properties 
	{
		_MainTex("_MainTex (RGB)", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_MetallicGlossMap("_MetallicGlossMap (RGB)", 2D) = "white" {}
		_BumpMap("_BumpMap (NRM)", 2D) = "bump" {}
		_AOMap("_AOMap (Grayscale)", 2D) = "white" {}
		_Emissive("Emission", 2D) = "black" {}
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

		#pragma surface surf Standard3 keepalpha
		#pragma target 3.0
        #include "HLSLSupport.cginc"
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"
        #include "UnityPBSLighting.cginc"
        #include "SSTUShaders.cginc"
				
		sampler2D _MainTex;
		sampler2D _Emissive;
		sampler2D _MetallicGlossMap;
		sampler2D _BumpMap;		
		sampler2D _AOMap;

		float _Opacity;
		float4 _Color;
		float4 _EmissiveColor;
		float4 _TemperatureColor;
		float4 _RimColor;
		float _RimFalloff;
		
		struct Input
		{
			float2 uv_MainTex;
			float3 viewDir;
		};

		struct SurfaceOutputStandard3
        {
            fixed3 Albedo;		// base (diffuse or specular) color
            fixed3 Specular;	// specular color
            fixed3 Normal;		// tangent space normal, if written
            half3 Emission;
            half3 Backlight;
            half Smoothness;	// 0=rough, 1=smooth
            half Occlusion;		// occlusion (default 1)
            fixed Alpha;		// alpha for transparencies
        };
        
        inline half4 LightingStandard3 (SurfaceOutputStandard3 s, half3 viewDir, UnityGI gi)
        {
            s.Normal = normalize(s.Normal);
            
            half backlight = backlitSolar(viewDir, s.Normal, gi.light.dir, 0);
            half3 backColor = (s.Backlight.rgb) * backlight.xxx * gi.light.color;

            // energy conservation
            half oneMinusReflectivity;
            s.Albedo = EnergyConservationBetweenDiffuseAndSpecular (s.Albedo, s.Specular, /*out*/ oneMinusReflectivity);

            // shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
            // this is necessary to handle transparency in physically correct way - only diffuse component gets affected by alpha
            half outputAlpha;
            s.Albedo = PreMultiplyAlpha (s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);

            half4 c = UNITY_BRDF_PBS (s.Albedo, s.Specular, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
            c.rgb += UNITY_BRDF_GI (s.Albedo, s.Specular, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, s.Occlusion, gi);
            c.rgb += backColor;
            c.a = outputAlpha;
            return c;
        }
        
        inline void LightingStandard3_GI (SurfaceOutputStandard3 s, UnityGIInput data, inout UnityGI gi)
        {
            UNITY_GI(gi, s, data);
        }
		
		void surf (Input IN, inout SurfaceOutputStandard3 o)
		{
			fixed4 color = tex2D(_MainTex,(IN.uv_MainTex));
			fixed4 spec = tex2D(_MetallicGlossMap, (IN.uv_MainTex));
			fixed3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
			fixed4 ao = tex2D(_AOMap, (IN.uv_MainTex));
			fixed4 glow = tex2D(_Emissive, (IN.uv_MainTex));
			
			o.Albedo = color.rgb * _Color.rgb;
			o.Normal = normal;
            o.Backlight = glow.rgb;
            o.Specular = spec.rgb;
            //o.Emission = glow.rgb;
			//o.Emission = fixed3(1,0,0);//glow.rgb * glow.aaa * _EmissiveColor.rgb *_EmissiveColor.aaa + stockEmit(IN.viewDir, normal, _RimColor, _RimFalloff, _TemperatureColor) * _Opacity;
			//o.Metallic = spec.r;
			o.Smoothness = 0.8;
			o.Occlusion = ao.r;
			o.Alpha = _Opacity;
		}
		ENDCG
	}
	Fallback "Bumped Specular"
}