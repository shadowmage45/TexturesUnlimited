// Volumetric Atmospheric Scattering
// by Alan Zucconi
// http://www.alanzucconi.com/?p=7374
Shader "Alan Zucconi/Scattering/Atmospheric Scattering (Simple)"
{
	Properties
	{
		// ----------------------------------------------
		// --- Standard Shader ---
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_GlossyMap ("Glossy Map", 2D) = "white" {}
		_BumpMap ("Bump Map", 2D) = "bump" {}
		// ---------------------------------------------
		
		// ----------------------------------------------
		// --- Scattering ---
		[Space]
		_AtmosphereModifier ("Atmosphere Modifier", Float) = 1
		_ScatteringModifier ("Scattering Modifier", Float) = 1
		_AtmosphereColor ("Atmosphere Color", Color) = (1,1,1,1)

		[Space]
		_SphereRadius("Sphere Radius (units)", Range(0.1,25)) = 6.371
		
		// Total radius + _PlanetRadius + _AtmosphereHeight
		_PlanetRadius ("Planet Radius (metres)", Float) = 6371000 // 6731Km
		_AtmosphereHeight ("Atmosphere Height (metres)", Float) = 60000 // 60Km
		// ----------------------------------------------

		// ----------------------------------------------
		// --- Rayleigh Scattering ---
		[Space]
		_RayScatteringCoefficient("βᵣ, Rayleight Scattering Coefficient (RGB, metres^-1)", Vector) = (0.000005804542996261093, 0.000013562911419845635, 0.00003026590629238531, 0)
		_RayScaleHeight("H₀, Rayleigh Scale Height (metres)", Float) = 8000 // 8Km
		// ----------------------------------------------

		// ----------------------------------------------
		// --- Mie Scattering ---
		[Space]
		_MieScatteringCoefficient("βₘ, Mie Scattering Coefficient (metres^-1)", Float) = 0.0021
		_MieAnisotropy ("g, Mie preferred scattering direction", Range(-1,1)) = 0.758
		_MieScaleHeight("H₀, Mie Scale Height (metres)", Float) = 1200 // 1.2Km
		
	
		[Space]
		//_SunCentre ("Sun Centre", Vector) = (0,0,0,0)
		_SunIntensity("Sun intensity", Range(0,100)) = 22


		[Space]
		_ViewSamples("View ray samples (out-scattering)", Range(0,256)) = 16
		_LightSamples("Light ray samples (in-scattering)", Range(0,256)) = 8
	}
	SubShader {

		// ----------------------------------------------------
		// --- STANDARD PASS ----------------------------------
		// ----------------------------------------------------
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _GlossyMap;

		struct Input {
			float2 uv_MainTex;
		};

		half _Glossiness;
		fixed4 _Color;

		// Alpha blending macro
		#define blend(c1, c2, a)	lerp((c1), (c2), (a))
		// color = alpha * src + (1 - alpha) * dest
		// color = alpha * (src - dest) + dest

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;

			// Metallic and smoothness come from slider variables
			o.Metallic = 0;
			o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_MainTex));
			o.Smoothness = tex2D (_GlossyMap, IN.uv_MainTex) * _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
		// ----------------------------------------------------





		// ----------------------------------------------------
		// --- SCATTERING PASS --------------------------------
		// ----------------------------------------------------
		Tags { "RenderType"="Transparent"
		"Queue"="Transparent"}
		LOD 200
		//ZWrite On
		//ZTest LEqual
		Cull Back

		Blend One One
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf StandardScattering vertex:vert // alpha:blend 

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
			float3 centre;
		};

		//half _Glossiness;
		//half _Metallic;
		fixed4 _Color;

		#define PI 3.14159265358979

		// Planet
		float _SphereRadius; // units
		float _PlanetRadius; // metres
		float _AtmosphereHeight; // metres
		float atmosphereRadius; // metres

		// Modifiers
		float _AtmosphereModifier;
		float _ScatteringModifier;
		fixed4 _AtmosphereColor;

		// Converts from space 
		float UnitsToMetres; // _PlanetRadius / _SphereRadius = UnitToSpace

		// Units
		float3 worldCentre;
		float3 worldPos;

		// Metres
		float3 spaceCentre;
		float3 spacePos;
		

		// Rayleigh Scattering
		float3 _RayScatteringCoefficient;
		float _RayScaleHeight;

		// Mie Scattering
		float _MieScatteringCoefficient;
		float _MieScaleHeight;
		float _MieAnisotropy;

		// Rendering
		float _SunIntensity;
		int _ViewSamples;
		int _LightSamples;

		void vert (inout appdata_full v, out Input o) {
			// Scale:
			//_Scale = _ScaleModifier * (_PlanetRadiusUnits / _PlanetRadius);
			//_Scale = (_PlanetRadiusUnits / _PlanetRadius);
			/*
			_AtmosphereRadiusUnits = (_PlanetRadius + _AtmosphereHeight) * _Scale;
			
			_RayScaleHeightUnits = _RayScaleHeight * _Scale;
			_MieScaleHeightUnits = _MieScaleHeight * _Scale;

			// Planet size is scaled down
			// To correct, we need to massively amplify the scattering
			// If planet is 1000 smaller, the scattering must be 1000 larger
			_RayScatteringCoefficientUnits = _RayScatteringCoefficient / _Scale;
			_MieScatteringCoefficientUnits = _MieScatteringCoefficient / _Scale;
			*/
			UNITY_INITIALIZE_OUTPUT(Input,o);
			const float MetresToUnits = _SphereRadius / _PlanetRadius;
			//v.vertex.xyz += v.normal * (_AtmosphereHeight / scale); // Atmosphere height in units
			v.vertex.xyz += v.normal * ( ( _AtmosphereHeight * _AtmosphereModifier) * MetresToUnits); // Atmosphere height in units
			o.centre = mul(unity_ObjectToWorld, half4(0,0,0,1));
		}

		bool rayInstersect
		(
			// Ray
			float3 O, // Origin
			float3 D, // Direction

			// Sphere
			float3 C, // Centre
			float R,	// Radius
			out float A, // First intersection time
			out float B  // Second intersections time
		)
		{
			float3 L = C - O;
			float DT = dot (L, D);
			// Ray pointing in the opposite direction
			/*
			if (DT < 0)
			{
				A = 0;
				B = 0;
				return false;
			}
			*/
			float R2 = R * R;

			//float CT = sqrt(dot(L,L) - DT*DT);
			float CT2 = dot(L,L) - DT*DT;
			
			// Intersection point outside the circle
			//if (CT > R)
			if (CT2 > R2)
			{
				//A = 0;
				//B = 0;
				return false;
			}

			//float AT = sqrt(R*R - CT*CT);
			float AT = sqrt(R2 - CT2);
			float TB = AT;
			//float BT = sqrt(R*R - CT*CT);

			A = DT - AT;
			B = DT + TB;
			return true;
		}

		// P: point inside atmosphere
		// S: sun direction
		// returns false if the point is inside the ground
		bool lightSampling
		(	float3 P, float3 S,
			out float opticalDepthRay, out float opticalDepthMie
		)
		{
				float C1; // don't care about this one
				float C2;
				rayInstersect(P, S, spaceCentre, atmosphereRadius, C1, C2);

				// Optical depth for secondary ray
				// (used for sun light attenuation)
				opticalDepthRay = 0;
				opticalDepthMie = 0;

				

				// Samples on the segment PC
				float time = 0;
				float lightSampleSize = distance(P, C2) / (float)(_LightSamples);
				for (int i = 0; i < _LightSamples; i ++)
				{
					// Sample point on the segment PC
					//float3 Q = P + S * (jStepSize * j);
					//float3 Q = P + S * (jTime + 0.5 * jStepSize);

					float3 Q = P + S * (time + lightSampleSize*0.5);
					float height = distance(spaceCentre, Q) - _PlanetRadius;
					// Inside the planet
					if (height < 0)
						return false;
						//break;

					// Optical depth for the secondary ray
					opticalDepthRay += exp(-height / _RayScaleHeight) * lightSampleSize;
					opticalDepthMie += exp(-height / _MieScaleHeight) * lightSampleSize;
					
					time += lightSampleSize;
				}

				return true;
		}

		#include "UnityPBSLighting.cginc"
		inline fixed4 LightingStandardScattering(SurfaceOutputStandard s, fixed3 viewDir, UnityGI gi)
		{
			//fixed4 dd = 1;	
			//dd.rgb = _RayScatteringCoefficientUnits;
			//return dd;

			// Original colour
			//fixed4 pbr = LightingStandard(s, viewDir, gi);
			
			// ------
			float3 L = gi.light.dir;
			float3 V = viewDir;
			float3 N = s.Normal;

			float3 S = L;	// Direction of light from the sun
			float3 D = -V;  // Direction of view ray piercing the atmosphere

			float tA;	// Atmosphere entry point (worldPos + V * tA)
			float tB;	// Atmosphere exit point  (worldPos + V * tB)
			//if (!rayInstersect(worldPos, D, centre, _AtmosphereRadius, tA, tB))
			if (!rayInstersect(spacePos, D, spaceCentre, atmosphereRadius, tA, tB))
				return fixed4(0,0,0,0);

			// Is the ray passing through the planet core?
			float pA, pB;
			if (rayInstersect(spacePos, D, spaceCentre, _PlanetRadius, pA, pB))
			{
				// Yes! Then we adjust the end point
				// so that it coincide with the first planet hit
				tB = pA;
			}

			// Total optical depth
			float opticalDepthRay = 0; // Rayleigh
			float opticalDepthMie = 0; // Mie

			// Total Scattering accumulated
			float3 totalRayScattering = float3(0,0,0); // RGB
			float totalMieScattering = 0; // A single channel

			float time = tA;
			float viewSampleSize = (tB-tA) / (float)(_ViewSamples);
			for (int i = 0; i < _ViewSamples; i ++)
			{
				// Point position
				// (sampling in the middle of the view sample segment)
				float3 P = spacePos + D * (time + viewSampleSize*0.5);

				// Height of point
				float height = distance(spaceCentre, P) - _PlanetRadius;
				
				// This point is inside the Planet
				//if (height <= 0)
				//	break;
				// The above check is removed
				// because tB is ajusted so that it never enters into the planet

				// Calculate the optical depth for the current segment
				float viewOpticalDepthRay = exp(-height / _RayScaleHeight) * viewSampleSize;
				float viewOpticalDepthMie = exp(-height / _MieScaleHeight) * viewSampleSize;

				// Accumulates the optical depths
				opticalDepthRay += viewOpticalDepthRay;
				opticalDepthMie += viewOpticalDepthMie;

				// We are sampling the amount of light received at point P,
				// from the segment AB
				// This light comes from the sun.
				// However, light from the sun itself goes into the atmosphere,
				// so is subjected to attenuation.
				// The dependes on how long it has travelled through the atmosphere.
				// C is the point at which the sun enters the atmosphere.
				// So the segment PC is the distance light from the sun travels
				// into the atmosphere before reaching P.
				// At that point, we take the light that remains and we see how much
				// is reflected back into the direction of the camera.

				// Optical depth for secondary ray (light sample)
				// (used for sun light attenuation)
				float lightOpticalDepthRay = 0;
				float lightOpticalDepthMie = 0;
				
				bool overground = lightSampling(P, S, lightOpticalDepthRay, lightOpticalDepthMie);
				if (overground)
				{
					// Calculates the attenuation of sun light
					// after travelling through the segment PC
					// This quantity is called T(PC)T(PA) in the tutorial
					float3 attenuation = exp
					(
						- (
							_RayScatteringCoefficient * (opticalDepthRay + lightOpticalDepthRay) +
							_MieScatteringCoefficient * (opticalDepthMie + lightOpticalDepthMie)
						)					
					);

					// Scattering accumulation
					totalRayScattering += viewOpticalDepthRay * attenuation;
					totalMieScattering += viewOpticalDepthMie * attenuation;
				}
				time += viewSampleSize;
			}

			float cosTheta = dot(V, S);
			float cos2Theta = cosTheta * cosTheta;
			float g = _MieAnisotropy;
			float g2 = g * g;
			float rayPhase = 3.0 / (16.0 * PI) * (1.0 + cos2Theta);
			float miePhase = (3.0 / (8.0 * PI)) * ((1.0 - g2) * (1.+cos2Theta)) / (pow(1.0 + g2 - 2.0 * g * cosTheta, 1.5) * (2.0 + g2));
			//float miePhase = ((1-g2) / (4*PI)) / pow(1+g * cosTheta,2);

			float3 scattering = _SunIntensity * 
			(
				(rayPhase * _RayScatteringCoefficient) * totalRayScattering +
				(miePhase * _MieScatteringCoefficient) * totalMieScattering
			);

			fixed4 c = _AtmosphereColor;
			c.rgb *= scattering * c.a;

			c.rgb = min(c.rgb, 1);
			return c;

		}
		



		void LightingStandardScattering_GI(SurfaceOutputStandard s, UnityGIInput data, inout UnityGI gi)
		{
			LightingStandard_GI(s, data, gi);		
		}
		// --------------------

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			//o.Metallic = _Metallic;
			//o.Smoothness = _Glossiness;
			o.Alpha = c.a;

			// In units
			worldCentre = IN.centre;
			worldPos = IN.worldPos;
			
			// Scale
			UnitsToMetres = _PlanetRadius / _SphereRadius;

			// In metres
			spaceCentre = float3(0,0,0);
			spacePos = (worldPos - worldCentre) * UnitsToMetres;

			// Atmosphere scaler
			atmosphereRadius = _PlanetRadius + (_AtmosphereHeight * _AtmosphereModifier);
			_RayScaleHeight *= _AtmosphereModifier;
			_MieScaleHeight *= _AtmosphereModifier;

			_RayScatteringCoefficient *= _ScatteringModifier;
			_MieScatteringCoefficient *= _ScatteringModifier;
		}
		ENDCG
	}
	FallBack "Diffuse"
}