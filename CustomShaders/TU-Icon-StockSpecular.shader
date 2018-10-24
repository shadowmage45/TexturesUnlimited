Shader "TU/Icon/StockSpecular"
{
	Properties 
	{
		//standard texture input slots
		_MainTex("_MainTex (RGB)", 2D) = "white" {}
		_SpecMap("_SpecGlossMap (RGBA)", 2D) = "white" {}
		_BumpMap("_BumpMap (NRM)", 2D) = "bump" {}
						
		//standard shader params
		_SpecTint ("Specular Tint", Range (0, 0.1)) = 0.05
		_Color ("_Color", Color) = (1,1,1)
		_GlossColor ("_SpecColor", Color) = (1,1,1)
		_Shininess ("_Smoothness", Range(0,1)) = 1
				
		//stock KSP compatibility properties -- used for emission/glow, part-highlighting, part-thermal overlay, and part 'burn' discoloring
		_Opacity("Part Opacity", Range(0,1) ) = 1
		_RimFalloff("_RimFalloff", Range(0.01,5) ) = 0.1
		_RimColor("_RimColor", Color) = (0,0,0,0)
		_TemperatureColor("Temperature Color", Color) = (0,0,0,0)
		_BurnColor ("Burn Color", Color) = (1,1,1,1)
		
		_MinX ("MinX", Range(0.000000,1.000000)) = 0.500000
		_MaxX ("MaxX", Range(0.000000,1.000000)) = 0.800000
		_MinY ("MinY", Range(0.000000,1.000000)) = 0.500000
		_MaxY ("MaxY", Range(0.000000,1.000000)) = 0.800000
		_Multiplier("Multiplier", Float) = 2
	}
	
	SubShader
	{
		Tags {"RenderType"="Opaque"}
		ZWrite On
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM

		//directives for 'surface shader' 'surface name = 'TU'' and 'don't discard alpha values'
		#pragma surface surf StandardSpecular keepalpha
		#pragma target 3.0
		
		half _Shininess;
		half _SpecTint;

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _SpecMap;

		float _Opacity;
		float _RimFalloff;
		float4 _RimColor;
		float4 _TemperatureColor;
		float4 _BurnColor;
			
		half _Multiplier;
		float _MinX;
		float _MaxX;
		float _MinY;
		float _MaxY;
		
		float4 _LocalCameraPos;
		float4 _LocalCameraDir;
		float4 _UnderwaterFogColor;
		float _UnderwaterMinAlphaFogDistance;
		float _UnderwaterMaxAlbedoFog;
		float _UnderwaterMaxAlphaFog;
		float _UnderwaterAlbedoDistanceScalar;
		float _UnderwaterAlphaDistanceScalar;
		float _UnderwaterFogFactor;
		
		//stock fog function
		fixed4 UnderwaterFog(fixed3 worldPos, fixed3 color)
		{
			fixed3 toPixel = worldPos - _LocalCameraPos.xyz;
			fixed toPixelLength = length(toPixel);
			
			fixed underwaterDetection = _UnderwaterFogFactor * _LocalCameraDir.w; ///< sign(1 - sign(_LocalCameraPos.w));
			fixed albedoLerpValue = underwaterDetection * (_UnderwaterMaxAlbedoFog * saturate(toPixelLength * _UnderwaterAlbedoDistanceScalar));
			fixed alphaFactor = 1 - underwaterDetection * (_UnderwaterMaxAlphaFog * saturate((toPixelLength - _UnderwaterMinAlphaFogDistance) * _UnderwaterAlphaDistanceScalar));

			return fixed4(lerp(color, _UnderwaterFogColor.rgb, albedoLerpValue), alphaFactor);
		}
		
		struct Input
		{
			float2 uv_MainTex;
			float2 uv_BumpMap;
			float2 uv_Emissive;
			float2 uv_SpecMap;
			float3 viewDir;
			float3 worldPos;
			float4 screenPos;
			float4 color : COLOR;
		};

		void surf (Input IN, inout SurfaceOutputStandardSpecular o)
		{
			float2 screenUV = IN.screenPos.xy / IN.screenPos.w;			
			#ifdef SHADER_API_GLCORE				
				screenUV.y = 1 - screenUV.y;
			#endif			
			if(screenUV.x < _MinX || screenUV.y < _MinY || screenUV.x > _MaxX || screenUV.y > _MaxY)
			{
				clip(-1);
				return;
			}
			
			float4 color = tex2D(_MainTex,(IN.uv_MainTex)) * _BurnColor * IN.color;
			float3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
			float3 specularMap = tex2D(_SpecMap,(IN.uv_SpecMap)).rgb;

			half rim = 1.0 - saturate(dot (normalize(IN.viewDir), normal));

			float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
			emission += _TemperatureColor.rgb * _TemperatureColor.a;

			float4 fog = UnderwaterFog(IN.worldPos, color);

			o.Albedo = fog.rgb;
			o.Emission = (emission+ specularMap)*_SpecTint;
		    //o.Gloss = color.a;
			o.Smoothness = _Shininess;
			o.Specular = specularMap;
			o.Normal = normal;
			o.Emission *= _Opacity * fog.a;
			o.Alpha = _Opacity * fog.a;
		}
		
		ENDCG
	}
	Fallback "Standard"
	CustomEditor "TUMetallicUI"
}