inline half3 stockEmit (float3 viewDir, float3 normal, half4 rimColor, half rimFalloff, half4 tempColor)
{
	half rim = 1.0 - saturate(dot (normalize(viewDir), normal));
	return rimColor.rgb * pow(rim, rimFalloff) * rimColor.a + tempColor.rgb * tempColor.a;
}

inline half backlitSolar(float3 viewDir, float3 normal, float3 lightDir, float dirBias)
{
    half backLight = max(0, -dot(normal, lightDir)) * max(0, -dot(normal, -viewDir));
    return backLight;
}