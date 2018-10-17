struct IconSurfaceOutput 
{
	half3 Albedo;
	half3 Normal;
	half3 Emission;
	half Specular;
	half3 GlossColor;
	fixed Alpha;
	half Multiplier;
};

struct ColoredSpecularSurfaceOutput 
{
	half3 Albedo;
	half3 Normal;
	half3 Emission;
	half Specular;
	half3 GlossColor;
	fixed Alpha;
};
	
struct SolarSurfaceOutput 
{
	half3 Albedo;
	half3 Normal;
	half3 Emission;
	half Specular;
	half3 GlossColor;
	half Alpha;
	half3 BackLight;
	half BackClamp;
};	

inline half4 LightingIcon (IconSurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
{
    //fixed normal, as Unity de-normalizes it somewhere between the lighting functions
	fixed3 fN = normalize(s.Normal);
	//diffuse light intensity, from surface normal and light direction
	half diff = max (0, dot (fN, lightDir));
	//specular light calculations
	half3 h = normalize (lightDir + viewDir);
	float nh = max (0, dot (fN, h));
	float spec = pow (nh, s.Specular * 128);
	half3 specCol = spec * s.GlossColor;
	
	//output fragment color; Unity adds Emission to it through some other method
	half4 c;
	c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * specCol) * atten * s.Multiplier;
	c.a = s.Alpha;
	return c;
}
		
inline half4 LightingColoredSpecular (ColoredSpecularSurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
{
    //fixed normal, as Unity de-normalizes it somewhere between the lighting functions
	fixed3 fN = normalize(s.Normal);
	//diffuse light intensity, from surface normal and light direction
	half diff = max (0, dot (fN, lightDir));
	//specular light calculations
	half3 h = normalize (lightDir + viewDir);
	float nh = max (0, dot (fN, h));
	float spec = pow (nh, s.Specular * 128);
	half3 specCol = spec * s.GlossColor;
	
	//output fragment color; Unity adds Emission to it through some other method
	half4 c;
	c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * specCol) * atten;
	c.a = s.Alpha;
	return c;
}

inline half4 LightingColoredSolar (SolarSurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
{
    //fixed normal, as Unity de-normalizes it somewhere between the lighting functions
	fixed3 fN = normalize(s.Normal);
	//diffuse light intensity, from surface normal and light direction
	half diff = max (0, dot (fN, lightDir));
	//specular light calculations
	half3 h = normalize (lightDir + viewDir);
	float nh = max (0, dot (fN, h));
	float spec = pow (nh, s.Specular * 128);
	half3 specCol = spec * s.GlossColor;
	
	//output fragment color; Unity adds Emission to it through some other method
	half4 c;
	
	half norm = s.BackClamp==0? 1 : 1 / (1 - s.BackClamp);
	half backLight = max(0, -dot(s.Normal, lightDir));
	backLight *= (max(0, -dot(s.Normal, -viewDir))+0.25)*0.8;
	half3 backColor = backLight * s.GlossColor * _LightColor0.rgb * s.BackLight;
	c.rgb = ((s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * specCol) + backColor) * atten;
	c.a = s.Alpha;
	return c;
}
		
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

inline fixed getMaskMix(fixed3 mask)
{
	return saturate(1 - (mask.r + mask.g + mask.b));
}

inline fixed3 getUserColor(fixed3 mask, fixed3 color1, fixed3 color2, fixed3 color3)
{
	return mask.rrr * color1.rgb + mask.ggg * color2.rgb + mask.bbb * color3.rgb;
}

inline fixed3 getUserValue(fixed3 mask, fixed color1, fixed color2, fixed color3)
{
	return mask.r * color1 + mask.g * color2 + mask.b * color3;
}
		
inline fixed mix1(fixed a, fixed b, fixed t)
{
	return a * (1 - t) + b * t;
}

inline fixed3 mix3(fixed3 a, fixed3 b, fixed t)
{
	return a * (1 - t) + b * t;
}

inline fixed3 recolorStandard(fixed3 diffuseSample, fixed3 maskSample, fixed norm, fixed3 userColor1, fixed3 userColor2, fixed3 userColor3)
{	
	fixed mixFactor = getMaskMix(maskSample);
	//the color to use from the recoloring channels
	fixed3 userSelectedColor = getUserColor(maskSample, userColor1, userColor2, userColor3);
	//luminance of the original texture -- used for details in masked portions
	fixed luminance = Luminance(diffuseSample);
	fixed3 detailColor = ((luminance - norm) * (1 - mixFactor)).rrr;
	return saturate(userSelectedColor + diffuseSample * mixFactor + detailColor);
}

inline fixed recolorStandard(fixed sample1, fixed3 maskSample, fixed norm, fixed user1, fixed user2, fixed user3)
{
	fixed mixFactor = getMaskMix(maskSample);
	fixed userSelectedValue = getUserValue(maskSample, user1, user2, user3);
	fixed detail = (sample1 - norm) * (1 - mixFactor);
	return saturate(userSelectedValue + detail + sample1 * mixFactor);
}

inline fixed3 recolorTinting(fixed3 diffuseSample, fixed3 maskSample, fixed3 userColor1, fixed3 userColor2, fixed3 userColor3)
{
	fixed mixFactor = getMaskMix(maskSample);
	fixed3 userSelectedColor = getUserColor(maskSample, userColor1, userColor2, userColor3);
	fixed3 detailColor = diffuseSample * (1 - mixFactor);
	return saturate(userSelectedColor * detailColor + diffuseSample * mixFactor);
}

inline fixed recolorTinting(fixed sample1, fixed3 maskSample, fixed user1, fixed user2, fixed user3)
{
	fixed mixFactor = getMaskMix(maskSample);
	fixed userSelectedValue = getUserValue(maskSample, user1, user2, user3);
	fixed detail = sample1 * (1 - mixFactor);
	return saturate(userSelectedValue + detail + sample1 * mixFactor);
}