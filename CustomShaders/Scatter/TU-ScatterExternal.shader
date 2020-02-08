// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Custom/TU-ScatterExternal" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_ScaleHeight("ScaleHeight", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = 0;
		}
		ENDCG

		//2nd pass
		Tags{ "RenderType" = "Transparent" "Queue"="Transparent" }
		LOD 200
		Cull Back
		Blend One One
		CGPROGRAM
		#pragma surface surf StandardScattering vertex:vert
		#include "UnityPBSLighting.cginc"
		#pragma target 3.0

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		sampler2D _MainTex;
		float _ScaleHeight;
		//float3 _PlanetCentre;

		struct Input 
		{
			float2 uv_MainTex;
			float3 worldPos;
			float3 centre;
		};

		void vert(inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = 0;
		}

		bool rayIntersect(float3 O, float3 D, float3 C, float R, out float AO, out float BO)
		{
			float3 L = C - O;
			float DT = dot(L, D);
			float R2 = R * R;
			float CT2 = dot(L, L) - DT * DT;
			if (CT2 > R2) { return false; }
			float AT = sqrt(R2 - CT2);
			float BT = AT;
			AO = DT - AT;
			BO = DT + BT;
			return true;
		}

		bool lightSampling (float3 P, float3 S, out float opticalDepthCA)
		{
			float _; // don't care about this one
			float C;
			rayIntersect(P, S, fixed3(0,0,0), 6.6, _, C);

			int _LightSamples = 40;
			float _RayScaleHeight = 1;

			// Samples on the segment PC
			float time = 0;
			float ds = distance(P, P + S * C) / (float)(_LightSamples);
			for (int i = 0; i < _LightSamples; i++)
			{
				float3 Q = P + S * (time + ds * 0.5);
				float height = distance(float3(0,0,0), Q) - float3(6,6,6);
				// Inside the planet
				if (height < 0) 
				{
					return false;
				}

				// Optical depth for the light ray
				opticalDepthCA += exp(-height / _RayScaleHeight) * ds;

				time += ds;
			}
			return true;
		}

		inline fixed4 LightingStandardScattering(SurfaceOutputStandard s, fixed3 viewDir, UnityGI gi) 
		{
			float3 L = gi.light.dir;
			float3 V = viewDir;
			float3 N = s.Normal;
			float3 S = L;
			float3 D = -V;

			float3 O = _WorldSpaceCameraPos;
			float3 C = float3(0, 0, 0);//world space planetOrigin;
			float R = 6.6f;

			float tA, tB;
			if (!rayIntersect(O, D, C, R, tA, tB)) 
			{
				return fixed4(0, 0, 0, 0);
			}
			
			float pA, pB;
			if (rayIntersect(O, D, C, R, pA, pB)) 
			{
				tB = pA;
			}

			int _ViewSamples = 40;

			float3 totalViewSamples = 0; 
			float opticalDepthPA = 0;
			float time = tA;
			float ds = (tB - tA) / (float)(_ViewSamples);
			for (int i = 0; i < _ViewSamples; i++)
			{
				float3 P = O + D * (time + ds * 0.5f);

				// Optical depth of current segment
				// ρ(h) * ds
				float height = distance(C, P) - 6;//planet radius
				float opticalDepthSegment = exp(-height / _ScaleHeight) * ds;

				// Accumulates the optical depths
				// D(PA)
				opticalDepthPA += opticalDepthSegment;

				time += ds;
				totalViewSamples += opticalDepthSegment;
			}

			// I = I_S * β(λ) * γ(θ) * totalViewSamples
			//float3 I = _SunIntensity * _ScatteringCoefficient * phase * totalViewSamples;
			return fixed4(totalViewSamples.r, totalViewSamples.g, totalViewSamples.b, 0);
			
		}

		void LightingStandardScattering_GI(SurfaceOutputStandard s, UnityGIInput data, inout UnityGI gi)
		{
			LightingStandard_GI(s, data, gi);
		}

		ENDCG
	}
	FallBack "Diffuse"
}
