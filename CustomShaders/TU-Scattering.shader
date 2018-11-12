Shader "Hidden/TU-Scattering"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color("_Color", Color) = (1,1,1)
		_PlanetPos("Planet Pos", Vector) = (0,0,0,0)
		_SunPos("Sun Pos", Vector) = (0, 0, 15000, 0)		
		_PlanetSize("Planet Size", Float) = 6
		_AtmoSize("Atmo Size", Float) = 6.06
		_SunIntensity("Sun Intensity", Float) = 20
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		//pass 0 - extract pass
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			//frustum bounding vectors, used to determine world-space view direction from inside screen space shader
			float3 _Left;
			float3 _Right;
			float3 _Left2;
			float3 _Right2;

			//simulation setup params - sun and planet positions, sizes, and atmosphere stuffs
			float4 _SunPos;
			float4 _PlanetPos;
			float _PlanetSize;
			float _AtmoSize;
			
			//scaling factor that handles difference between intended real size, and simulated scaled size
			float _ScaleAdjustFactor;

			//basic non-realistic atmosphere recoloring mechanism
			float4 _Color;

			//multiplier to sun output intensity -- units are in ??????
			float _SunIntensity;

			int _ViewSamples = 16;
			int _LightSamples = 8;

			//actual scattering parameters that control scattering simulation
			float _RayScaleHeight;
			float3 _RayScatteringCoefficient;
			float _MieScaleHeight;			
			float _MieScatteringCoefficient;
			float _MieAnisotropy;
			
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
				float3 viewDir : TEXCOORD1;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;

				float3 left = lerp(_Left2, _Left, v.uv.y);
				float3 right = lerp(_Right2, _Right, v.uv.y);
				o.viewDir = normalize(lerp(left, right, v.uv.x));

				return o;
			}
			
			sampler2D _MainTex;
			sampler2D _CameraDepthTexture;
			sampler2D _LastCameraDepthTexture;
			
			float distanceFromLine(float3 origin, float3 direction, float3 pnt)
			{
				return length(cross(origin - pnt, direction));
			}

			float sqDistanceFromLine(float3 origin, float3 direction, float3 pnt)
			{
				float3 d = cross(origin - pnt, direction);
				return d.x*d.x + d.y*d.y + d.z*d.z;
			}

			//O = Origin
			//D = Direction
			//C = Sphere Center
			//R = Sphere Radius
			//AO = Entry Intersection
			//AB = Exit Intersection
			bool rayIntersect(float3 O, float3 D, float3 C, float R, out float A, out float B)
			{
				//https://www.scratchapixel.com/lessons/3d-basic-rendering/minimal-ray-tracer-rendering-simple-shapes/ray-sphere-intersection
				//L is a line pointing from ray origin (O) to sphere center (C)
				float3 L = C - O;
				//Tca is dot product of L and D; projection of sphere center onto ray line as a length from ray origin
				float DT = dot(L, D);
				//if DT is negative, L/D point in opposite directions, so any intersection would be behind the camera
				if (DT < 0) { return false; }
				
				//R2 = radius squared
				float R2 = R * R;
				float CT2 = dot(L, L) - DT * DT;
				//if distance squared is greater than radius squared, there are zero intersects
				if (CT2 > R2) { return false; }

				float AT = sqrt(R2 - CT2);
				float TB = AT;
				A = DT - AT;
				B = DT + TB;
				return true;
			}

			//compute full atmosphere ray -- entry and exit point of the ray with the atmosphere; return false if no intersect
			// WORKS - but has issues when starting from within the atmo; need to find how to set the start/end points appropriately in those cases
			bool fullAtmoIntersect(float3 O, float3 D, float3 C, float AR, float PR, out float AO, out float BO) 
			{
				//quick test to see if camera is -inside- of atmosphere
				float3 cd = C - O;
				float cDistSq = cd.x*cd.x+cd.y*cd.y+cd.z*cd.z;
				//R2 = atmo radius squared
				float R2 = AR * AR;
				bool inAtmo = cDistSq < R2;
				bool inPlanet = cDistSq < PR * PR;
				if (inPlanet) { return false; }
				if (inAtmo)
				{
					//line segment from origin
					float3 L = C - O;
					float tca = dot(L, D);
					float d2 = dot(L, L) - tca * tca;
					float thc = sqrt(R2 - d2);
					AO = 0;// tca - thc;
					BO = tca + thc;

					float PA, PB;
					//check if ray intersects planet
					if (rayIntersect(O, D, C, PR, PA, PB) && tca > 0) 
					{
						BO = PA;
					}
				}
				else 
				{
					//return false;
					float d2 = sqDistanceFromLine(O, D, C);

					//https://www.scratchapixel.com/lessons/3d-basic-rendering/minimal-ray-tracer-rendering-simple-shapes/ray-sphere-intersection
					//L is a line pointing from ray origin (O) to sphere center (C)
					float3 L = C - O;
					//Tca is dot product of L and D; projection of sphere center onto ray line as a length from ray origin
					float Tca = dot(L, D);
					//if Tca is negative, L/D point in opposite directions, so any intersection would be behind the camera
					if (Tca < 0) { return false; }

					//if distance squared is greater than radius squared, there are zero intersects
					if (d2 > R2) { return false; }

					float Thc = sqrt(R2 - d2);
					AO = Tca - Thc;
					BO = Tca + Thc;

					//check if ray hits the planet
					float P2 = PR * PR;
					if (d2 < P2)
					{
						Thc = sqrt(P2 - d2);
						BO = Tca - Thc;
					}
				}
				
				return true;
			}

			bool lightSampling(float3 P, float3 S, out float opticalDepthRay, out float opticalDepthMie)
			{
				float C1, C2;
				rayIntersect(P, S, _PlanetPos, _AtmoSize, C1, C2);
				// Optical depth for secondary ray
				// (used for sun light attenuation)
				opticalDepthRay = 0;
				opticalDepthMie = 0;
				float time = 0;
				float lightSampleSize = distance(P, C2) / (float)(_LightSamples);

				for (int i = 0; i < _LightSamples; i++)
				{
					// Sample point on the segment PC
					float3 Q = P + S * (time + lightSampleSize * 0.5);
					float height = distance(_PlanetPos, Q) - _PlanetSize;
					height *= _ScaleAdjustFactor;
					// Inside the planet
					if (height < 0) 
					{
						//break;
						return false;
					}

					// Optical depth for the secondary ray
					opticalDepthRay += exp(-height / _RayScaleHeight) * lightSampleSize * _ScaleAdjustFactor;
					opticalDepthMie += exp(-height / _MieScaleHeight) * lightSampleSize * _ScaleAdjustFactor;

					time += lightSampleSize;
				}
				return true;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				//float3 _RayScatteringCoefficient = float3(0.000005804542996261093, 0.000013562911419845635, 0.00003026590629238531);
				//float _MieScatteringCoefficient = 0.0021;
				//float _MieAnisotropy = 0.758;
				//float _SunIntensity = 20;


				//view direction
				float3 V = normalize(i.viewDir);
				//ray direction
				float3 D = V;
				//camera world space position
				float3 cameraPos = _WorldSpaceCameraPos.xyz;
				//planet center world space position
				float3 pnt = _PlanetPos;
				//direction from point to sun
				float3 S = normalize(_SunPos - _PlanetPos);

				//find start and end intersects of the ray with the atmosphere
				//these are offsets along the ray with 0 = starting point
				float tA;
				float tB;
				
				if (!fullAtmoIntersect(cameraPos, D, pnt, _AtmoSize, _PlanetSize, tA, tB))
				{
					return fixed4(0,0,0,0);
				}

				//bool inAtmo = length(cameraPos - _PlanetPos) < _AtmoSize;



				// Total optical depth
				float opticalDepthRay = 0; // Rayleigh
				float opticalDepthMie = 0; // Mie

										   // Total Scattering accumulated
				float3 totalRayScattering = float3(0, 0, 0); // RGB
				float totalMieScattering = 0; // A single channel
				
				float time = tA;
				float viewSampleSize = (tB - tA) / (float)(_ViewSamples);
				for (int i = 0; i < _ViewSamples; i++)
				{
					// Point position
					// (sampling in the middle of the view sample segment)
					float3 P = cameraPos + D * (time + viewSampleSize * 0.5);

					// Height of point
					float height = distance(pnt, P) - _PlanetSize;
					height *= _ScaleAdjustFactor;

					// This point is inside the Planet
					//if (height <= 0)
					//	break;
					// The above check is removed
					// because tB is ajusted so that it never enters into the planet

					// Calculate the optical depth for the current segment
					float viewOpticalDepthRay = exp(-height / _RayScaleHeight) * viewSampleSize *_ScaleAdjustFactor;
					float viewOpticalDepthMie = exp(-height / _MieScaleHeight) * viewSampleSize *_ScaleAdjustFactor;

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
						float3 attenuation = exp (-(_RayScatteringCoefficient * (opticalDepthRay + lightOpticalDepthRay) + _MieScatteringCoefficient * (opticalDepthMie + lightOpticalDepthMie)));
						// Scattering accumulation
						//totalRayScattering += viewOpticalDepthRay * attenuation;
						//totalMieScattering += viewOpticalDepthMie * attenuation;

						float3 rayAtten = exp(-(_RayScatteringCoefficient * (opticalDepthRay + lightOpticalDepthRay)));
						float mieAtten = exp(-(_MieScatteringCoefficient * (opticalDepthMie + lightOpticalDepthMie)));
						totalRayScattering += viewOpticalDepthRay * rayAtten;
						totalMieScattering += viewOpticalDepthMie * mieAtten;
						
					}
					time += viewSampleSize;
				}

				//phase functions are in here somewhere...
				float PI = 3.1415926543785;
				float cosTheta = dot(V, S);
				float cos2Theta = cosTheta * cosTheta;
				float g = _MieAnisotropy;
				float g2 = g * g;
				float rayPhase = 3.0 / (16.0 * PI) * (1.0 + cos2Theta);
				float miePhase = (3.0 / (8.0 * PI)) * ((1.0 - g2) * (1. + cos2Theta)) / (pow(1.0 + g2 - 2.0 * g * cosTheta, 1.5) * (2.0 + g2));
				//float miePhase = ((1-g2) / (8*PI)) / pow(1+g * cosTheta,2);

				float3 rayScatter = (rayPhase * _RayScatteringCoefficient) * totalRayScattering;
				float ms = (miePhase * _MieScatteringCoefficient) * totalMieScattering;
				float3 mieScatter = float3(ms,ms,ms);
				//totalMieScattering = max(0, totalMieScattering);
				//miePhase = max(0, miePhase);
				float3 scattering = _SunIntensity * ( rayScatter + mieScatter );
				return fixed4(scattering*_Color.rgb, 1);
			}
			ENDCG
		}

		//pass 1 - combine pass
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			sampler2D _SecTex;

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				fixed4 fog = tex2D(_SecTex, i.uv);
				return col + fog;
			}
			ENDCG
		}

	}
}
