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
		_BacklightBoost("Back Lighting Boost", Range(0, 5) ) = 1
		_BasePolarization("View-Light influence", Range(0,5) ) = 1
		_IncomingPolarization("Light-Surf influence", Range(0,5) ) = 1
		_OutgoingPolarization("Surf-view influence", Range(0,5) ) = 1
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
		
		float _BasePolarization;
		float _IncomingPolarization;
		float _OutgoingPolarization;
		float _BacklightBoost;
		
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
            half3 Backlight;	// backlight emissive glow color
			half3 Polarization;	// polarization properties to use in backlight calculations
            half Metallic;		// 0=non-metal, 1=metal
            half Smoothness;	// 0=rough, 1=smooth
            half Occlusion;		// occlusion (default 1)
            fixed Alpha;		// alpha for transparencies
        };
        
        inline half4 LightingStandard2(SurfaceOutputStandard2 s, half3 viewDir, UnityGI gi)
        {
            s.Normal = normalize(s.Normal);
			
			half basePol = s.Polarization.x;
			half incPol = s.Polarization.y;
			half outPol = s.Polarization.z;
			
			half viewDotLight = max(0, -dot(viewDir, gi.light.dir));//view vs light...does...stuff...
			half viewDotNorm = max(0, -dot(s.Normal, -viewDir));//outgoing polarization
			half lightDotNorm = max(0, -dot(s.Normal, gi.light.dir));//incoming polarization
			
			half cont1 = min(1, pow(viewDotLight, basePol));
			half cont2 = min(1, pow(viewDotNorm, outPol));
			half cont3 = min(1, pow(lightDotNorm, incPol));
			half backLight = cont1 * cont2 * cont3;
            
            // half backLight = max(0, -dot(s.Normal, gi.light.dir));
            // backLight *= (max(0, -dot(s.Normal, -viewDir)));
            half3 backColor = (s.Backlight.rgb) * backLight.xxx * gi.light.color;
            
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
        		
        inline half3 stockEmit (float3 viewDir, float3 normal, half4 rimColor, half rimFalloff, half4 tempColor)
        {
            half rim = 1.0 - saturate(dot (normalize(viewDir), normal));
            return rimColor.rgb * pow(rim, rimFalloff) * rimColor.a + tempColor.rgb * tempColor.a;
        }
		
		void surf (Input IN, inout SurfaceOutputStandard2 o)
		{
			fixed4 color = tex2D(_MainTex,(IN.uv_MainTex));
			fixed4 spec = tex2D(_MetallicGlossMap, (IN.uv_MainTex));
			fixed3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
			fixed4 ao = tex2D(_AOMap, (IN.uv_MainTex));
			fixed4 glow = tex2D(_Emissive, (IN.uv_MainTex));
			
			o.Albedo = color.rgb * _Color.rgb;
			o.Normal = normal;
            o.Backlight = glow.rgb * _BacklightBoost.xxx;
			o.Emission = _EmissiveColor.rgb *_EmissiveColor.aaa + stockEmit(IN.viewDir, normal, _RimColor, _RimFalloff, _TemperatureColor) * _Opacity;
			o.Metallic = spec.r;
			o.Smoothness = spec.a;
			o.Occlusion = ao.r;
			o.Polarization = fixed3(_BasePolarization, _IncomingPolarization, _OutgoingPolarization);
			o.Alpha = _Opacity;
		}
		ENDCG
	}
	Fallback "Bumped Specular"
}