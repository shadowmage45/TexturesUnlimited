Shader "Hidden/TU-Scattering"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color("_Color", Color) = (1,1,1)
		_Scale("Scale", Range(0,2)) = 1
		_PlanetPos("Planet Pos", Vector) = (0,0,0,0)
		_SunPos("Sun Pos", Vector) = (0, 0, 15000, 0)		
		_PlanetSize("Planet Size", Float) = 6
		_AtmoSize("Atmo Size", Float) = 6.6
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

			float3 _Left;
			float3 _Right;
			float3 _Left2;
			float3 _Right2;

			float4x4 _frustumCorners;

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

			float4 _SunPos;
			float4 _PlanetPos;
			float _PlanetSize;
			float _AtmoSize;
			float4 _Color; 
			float _ScaleHeight;
			float _SunPower;
			
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
			bool rayIntersect(float3 O, float3 D, float3 C, float R, float d2, out float AO, out float BO)
			{
				//https://www.scratchapixel.com/lessons/3d-basic-rendering/minimal-ray-tracer-rendering-simple-shapes/ray-sphere-intersection
				//L is a line pointing from ray origin (O) to sphere center (C)
				float3 L = C - O;
				//Tca is dot product of L and D; projection of sphere center onto ray line as a length from ray origin
				float Tca = dot(L, D);
				//if Tca is negative, L/D point in opposite directions, so any intersection would be behind the camera
				if (Tca < 0) { return false; }
				
				//R2 = radius squared
				float R2 = R * R;
				//if distance squared is greater than radius squared, there are zero intersects
				if (d2 > R2) { return false; }

				float Thc = sqrt(R2 - d2);
				AO = Tca - Thc;
				BO = Tca + Thc;
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

				float d2 = sqDistanceFromLine(O, D, C);

				//https://www.scratchapixel.com/lessons/3d-basic-rendering/minimal-ray-tracer-rendering-simple-shapes/ray-sphere-intersection
				//L is a line pointing from ray origin (O) to sphere center (C)
				float3 L = C - O;
				//Tca is dot product of L and D; projection of sphere center onto ray line as a length from ray origin
				float Tca = dot(L, D);
				//if Tca is negative, L/D point in opposite directions, so any intersection would be behind the camera
				if (Tca < 0) { return false; }

				//if distance squared is greater than radius squared, there are zero intersects
				if (!inAtmo && d2 > R2) { return false; }
				
				float Thc = sqrt(R2 - d2);
				AO = Tca - Thc;
				BO = Tca + Thc;
				if (inAtmo) { AO = 0; }

				//check if ray hits the planet
				float P2 = PR * PR;
				if (d2 < P2) 
				{
					Thc = sqrt(P2 - d2);
					BO = Tca - Thc;
				}
				
				return true;
			}

			bool lightSample(float3 P, float3 D, out float3 accum) 
			{
				float height = distance(_PlanetPos, P) - _PlanetSize;
				if (height < 0) 
				{
					return false;
				}
				float sampleC = exp(-height / _ScaleHeight);
				accum = sampleC*_SunPower;
				return true;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float3 viewDir = normalize(i.viewDir);
				float3 cameraPos = _WorldSpaceCameraPos.xyz;
				float3 direction = viewDir;
				float3 pnt = _PlanetPos;
				int samples = 40;

				//find start and end intersects of the ray with the atmosphere
				float rayAtmoStart;
				float rayAtmoEnd;
				
				if (!fullAtmoIntersect(cameraPos, direction, pnt, _AtmoSize, _PlanetSize, rayAtmoStart, rayAtmoEnd)) 
				{
					return 0;
				}

				float3 accumA = 0;
				float3 accumB = 0;
				float sampleLength = (rayAtmoEnd - rayAtmoStart) / (float)samples;
				float3 pos = cameraPos + direction * rayAtmoStart + direction * sampleLength * 0.5;
				float3 S;

				for (int i = 0; i < samples; i++)
				{
					float height = distance(pnt, pos) - _PlanetSize;

					//direct accumulation along the travel path of the ray
					accumA += exp(-height / _ScaleHeight) * sampleLength *_SunPower;

					if (lightSample(pos, cameraPos - _SunPos, S)) 
					{
						accumB += S * sampleLength;
					}
					pos += direction * sampleLength;
				}
				
				return fixed4(accumA, 1);
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
