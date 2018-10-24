Shader "TU/Skybox" {
Properties {
    [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
	[NoScaleOffset] _Tex ("Cubemap (HDR)", Cube) = "grey" {}
}

SubShader {
	Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
	Cull Off ZWrite Off

	Pass {
		
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 2.0

		#include "UnityCG.cginc"

		samplerCUBE _Tex;
        half4 _Tex_HDR;
        half _Exposure;
		
		struct appdata_t {
			float4 vertex : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct v2f {
			float4 vertex : SV_POSITION;
			float3 texcoord : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
		};

		v2f vert (appdata_t v)
		{
			v2f o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.texcoord = v.vertex.xyz;
			return o;
		}

		fixed4 frag (v2f i) : SV_Target
		{
			half4 tex = texCUBE (_Tex, i.texcoord);
            half3 c = DecodeHDR (tex, _Tex_HDR);
            c *= _Exposure;
			return half4(c.rgb, 1);
		}
		ENDCG 
	}
} 	


Fallback Off

}
