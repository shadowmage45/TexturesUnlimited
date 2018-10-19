//functions to be shared across all shaders
		
inline half3 stockEmit (float3 viewDir, float3 normal, half4 rimColor, half rimFalloff, half4 tempColor)
{
	half rim = 1.0 - saturate(dot (normalize(viewDir), normal));
	return rimColor.rgb * pow(rim, rimFalloff) * rimColor.a + tempColor.rgb * tempColor.a;
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

inline fixed3 recolorStandardSpecularToMetallic(fixed3 diffuseSample, fixed3 glossSample, fixed3 maskSample, fixed3 maskMetallic, fixed norm, fixed glossNorm, fixed specInput, fixed3 userColor1, fixed3 userColor2, fixed3 userColor3, out fixed3 glossColor)
{	
	fixed mixFactor = getMaskMix(maskSample);
	fixed specMixFactor = mixFactor * specInput;
	
	//the color to use from the recoloring channels
	fixed3 userSelectedColor = getUserColor(maskSample, userColor1, userColor2, userColor3);
	
	//determines how much of the user selected value is diverted to specular coloring
	fixed metalMask = getUserValue(maskSample, maskMetallic.r, maskMetallic.g, maskMetallic.b);
	
	fixed3 userGlossColor = max(userSelectedColor * metalMask, fixed3(0.2,0.2,0.2));
	userSelectedColor = (1 - metalMask) * userSelectedColor;	
	
	fixed specLum = Luminance(glossSample);
	fixed3 detailSpec = ((specLum - glossNorm) * (1 - specMixFactor)).rrr;
	//output
	glossColor.rgb = saturate(userGlossColor + glossSample * specMixFactor + detailSpec).rgb;
	
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