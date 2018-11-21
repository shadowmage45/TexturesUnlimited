//functions to be shared across all shaders
//MORE KSP shader properties...for fog.. they have to go here so that they are declared prior to the function
//silly C-style ordered linking...
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

inline fixed3 recolorStandard(fixed3 diffuseSample, fixed3 maskSample, fixed norm, fixed detailMult, fixed3 userColor1, fixed3 userColor2, fixed3 userColor3)
{
	fixed mixFactor = getMaskMix(maskSample);
	fixed userMix = 1 - mixFactor;
	
	//the color to use from the recoloring channels
	fixed3 userSelectedColor = getUserColor(maskSample, userColor1, userColor2, userColor3);
	
	//luminance of the original texture -- used for details in masked portions
	fixed luminance = Luminance(diffuseSample);
	
	//output factor of the original texture, used in unmasked or partially masked pixels
	fixed3 baseOutput = mixFactor * diffuseSample;
	
	//extracts a +/- 0 detail value
	//will be NAN if normalization value is zero (unmasked pixels)
	fixed detail = ((luminance - norm) / norm) * userMix;
	
	//user selected coloring, plus details as applied in a renormalized fashion
	fixed3 userPlusDetail = (userSelectedColor * detail * detailMult) + userSelectedColor;
	
	//combined output value
	//using saturate on the recoloring value, else it might have NANs from the division operation
	return saturate(userPlusDetail) + baseOutput;
}

inline fixed recolorStandard(fixed sample1, fixed3 maskSample, fixed norm, fixed detailMult, fixed user1, fixed user2, fixed user3)
{
	fixed mixFactor = getMaskMix(maskSample);
	fixed userSelectedValue = getUserValue(maskSample, user1, user2, user3);
	fixed baseOutput = sample1 * mixFactor;
	// dt = (1 - 0) * ( 1 )
	// upd = (1 / 0) * 1 * 1 + 1
	fixed detail = (sample1 - norm) * (1 - mixFactor) * userSelectedValue * detailMult;
	fixed userPlusDetail = detail + userSelectedValue;
	return saturate(userPlusDetail) + baseOutput;
}

/**
	Specular Recoloring - Convert from Specular to Metallic Workflow
	For recolored pixels
		Add diffuse + gloss
		Extract	gloss color from user-selected value * metallic slider input
		Output separate gloss color and diffuse colors
**/
inline fixed3 recolorStandardSpecularToMetallic(fixed3 diffuseSample, fixed3 glossSample, fixed3 maskSample, fixed3 maskMetallic, fixed norm, fixed glossNorm, fixed detailMult, fixed specInput, fixed3 userColor1, fixed3 userColor2, fixed3 userColor3, out fixed3 glossColor)
{
	fixed mixFactor = getMaskMix(maskSample);
	fixed userMix = 1 - mixFactor;
	fixed specMixFactor = mixFactor * specInput;
	
	//determines how much of the user selected value is diverted to specular coloring
	fixed metalMask = getUserValue(maskSample, maskMetallic.r, maskMetallic.g, maskMetallic.b);
	
	//the color to use from the recoloring channels
	fixed3 userSelectedColor = getUserColor(maskSample, userColor1, userColor2, userColor3);
	
	//luminance of the original textures -- used for details in masked portions
	fixed luminance = Luminance(diffuseSample + glossSample);
	
	//output factor of the original texture, used in unmasked or partially masked pixels
	fixed3 baseOutput = diffuseSample * mixFactor;
	fixed3 baseSpecOutput = glossSample * specMixFactor;
	
	//extracts a +/- 0 detail value
	//will be NAN if normalization value is zero (unmasked pixels)
	fixed detail = ((luminance - norm) / norm) * userMix;
	
	//user selected coloring, plus details as applied in a renormalized fashion
	fixed3 userPlusDetail = (userSelectedColor * detail * detailMult) + userSelectedColor;
		
	//this is the gloss color, derived from metallic input + diffuse color
	fixed3 userGlossColor = max(userPlusDetail * metalMask, fixed3(0.2,0.2,0.2));
	
	//the remainder is used for the diffuse output
	userPlusDetail = (1 - metalMask) * userPlusDetail;	
	
	//output glossColor
	glossColor = saturate(userGlossColor) + baseSpecOutput;
	return saturate(userPlusDetail) + baseOutput;
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

inline float3 subsurf(float SubSurfScale, float SubSurfPower, float SubSurfDistort, float SubSurfAtten, float SubSurfAmbient, float3 color, float3 Thickness, float3 normal, float3 viewDir, float3 lightColor, float3 lightDir)
{
	//SSS implementation from:  https://colinbarrebrisebois.com/2011/03/07/gdc-2011-approximating-translucency-for-a-fast-cheap-and-convincing-subsurface-scattering-look/	
	float fLTScale = SubSurfScale;//main output scalar
	float iLTPower = SubSurfPower;//exponent used in power
	float fLTDistortion = SubSurfDistort;;//how much the surface normal distorts the outgoing light
	float fLightAttenuation = SubSurfAtten;//how much light attenuates while traveling through the surface (gets multiplied by distance)
	
	float fLTAmbient = SubSurfAmbient;//ambient from texture/material
	float3 fLTThickness = Thickness;//sampled from texture
	
	//float3 vLTLight = lightDir * (1-abs(fLTDistortion)) + normal * fLTDistortion;
	float3 vLTLight = lightDir + normal * fLTDistortion;
	float fLTDot = pow(saturate(dot(viewDir, -vLTLight)), iLTPower) * fLTScale;
	float3 fLT = fLightAttenuation * (fLTDot + fLTAmbient) * fLTThickness;
	return color * lightColor * fLT;
}