Shader "Hidden/TU-Clouds"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color("_Color", Color) = (1,1,1)
		_PlanetPos("Planet Pos", Vector) = (0,0,0,0)
		_SunDir("Sun Dir", Vector) = (0, 0, 1)
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
			#include "/../Noise/randomNoise2.cginc"

			//frustum bounding vectors, used to determine world-space view direction from inside screen space shader
			float3 _Left;
			float3 _Right;
			float3 _Left2;
			float3 _Right2;

			//simulation setup params - sun and planet positions, sizes, and atmosphere stuffs
			float4 _SunPos;
			float3 _SunDir;
			float4 _PlanetPos;
			float _PlanetSize;
			float _AtmoSize;
			
			//scaling factor that handles difference between intended real size, and simulated scaled size
			float _ScaleAdjustFactor;

			//basic non-realistic atmosphere recoloring mechanism
			float4 _Color;

			//int based toggle for cloud cover
			int _Clouds;

			int _ViewSamples = 16;
			int _LightSamples = 8;

			sampler2D _MainTex;
			
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

			float remap01(float val, float min, float max, float decay) 
			{
				//delta between min and max, this forms our 0-1 range
				float delta = max - min;
				if (val <= min) { return 0; }
				if (val >= max) { return 0; }
				val -= min;
				val /= delta;
				if (val < 0.25) 
				{
					float ts = val / 0.25;
					ts = pow(ts, decay);
					val *= ts;
				}
				else if (val > 0.75) 
				{
					float ts = 1 - ((val - 0.75) / 0.25);
					ts = pow(ts, decay);
					val *= ts;
				}
				return val;
			}

			float cloudNoise(float3 P, float t, int oct) 
			{
				float noise = 0;
				float samp = 1;
				float mult = 1;
				float freq = 1;
				for (int i = 0; i < oct; i++) 
				{
					samp = snoise(float4(P * freq, t)) * mult;
					noise += samp;

					//decrease blending factor for next noise sample
					mult *= 0.5;
					//increase noise frequency
					freq *= 2;
				}
				return clamp(noise, 0, 1);
			}

			//P = world space sample position
			//h = height 
			float cloudSampling(float3 P, float h)
			{
				//cloud height deltasomething
				float chd = 1;
				//chd = remap01(h, 0, (_AtmoSize - _PlanetSize)*_ScaleAdjustFactor, 2);
				//return h;

				//offset by world-space planet pos, to get planet-space position
				P -= _PlanetPos;
				//normalize by atmosphere size to get a 0-1 value
				P /= _AtmoSize;
				//modulate y coordinate to instigate some horizontal banding
				P.y *= 4;
				return cloudNoise(P * 5, 1, 8) * chd;
			}

			bool cloudTest(float3 P) 
			{
				P -= _PlanetPos;
				P /= _AtmoSize;
				P.y *= 4;
				float check = cloudNoise(P * 5, 1.5, 4) - 0.25;
				return check > 0;
			}

			bool lightSampling(float3 P, float3 S)
			{
				float clouds = 0;
				float C1, C2;
				rayIntersect(P, S, _PlanetPos, _AtmoSize, C1, C2);
				// Optical depth for secondary ray
				// (used for sun light attenuation)
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
						return 0;
					}
					
					clouds += cloudSampling(Q, height)*100;// *(float)_LightSamples;
					
					time += lightSampleSize;
				}
				return clouds;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				
				float clouds = 0;
				float cloudsScat = 0;

				//view direction
				float3 V = normalize(i.viewDir);
				//ray direction
				float3 D = V;
				//camera world space position
				float3 cameraPos = _WorldSpaceCameraPos.xyz;
				//planet center world space position
				float3 pnt = _PlanetPos;// -cameraPos;
				//cameraPos = float3(0, 0, 0);
				//direction from point to sun
				float3 S = _SunDir;// normalize(_SunPos - _PlanetPos);

				//find start and end intersects of the ray with the atmosphere
				//these are offsets along the ray with 0 = starting point
				float tA;
				float tB;
				
				if (!fullAtmoIntersect(cameraPos, D, pnt, _AtmoSize, _PlanetSize, tA, tB))
				{
					return fixed4(0,0,0,0);
				}

				//bool inAtmo = length(cameraPos - _PlanetPos) < _AtmoSize;
												
				float time = tA;
				float viewSampleSize = (tB - tA) / (float)(_ViewSamples);
				for (int i = 0; i < _ViewSamples; i++)
				{
					// Point position
					// (sampling in the middle of the view sample segment)
					float3 P = cameraPos + D * (time + viewSampleSize *0.5);
					
					// Height of point
					float height = distance(pnt, P) - _PlanetSize;
					height *= _ScaleAdjustFactor;

					if (cloudTest(P))
					{
						clouds += cloudSampling(P, height) / (float)_ViewSamples;
						cloudsScat += lightSampling(P, S) / (float)_LightSamples;
					}

					time += viewSampleSize;
				}
				return fixed4((clouds-cloudsScat).rrr * _Color.rgb, 1);
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
