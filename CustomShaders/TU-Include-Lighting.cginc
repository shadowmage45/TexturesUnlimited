//custom lighting function to enable colored specular, SubSurf, and icon light multiply functionality
//three versions -- one for pbr/metallic, pbr/specular, and legacy/specular
#if TU_LIGHT_METAL		
	//replacement for Unity bridge method to call GI with custom structs
	inline void LightingTU_GI (SurfaceOutputTU s, UnityGIInput data, inout UnityGI gi)
	{
		UNITY_GI(gi, s, data);
	}	

	//custom lighting function to enable SubSurf functionality
	inline half4 LightingTU(SurfaceOutputTU s, half3 viewDir, UnityGI gi)
	{
		s.Normal = normalize(s.Normal);
		
		//Unity 'Standard Metallic' lighting function, unabridged
		half oneMinusReflectivity;
		half3 specSampleColor;
		s.Albedo = DiffuseAndSpecularFromMetallic(s.Albedo, s.Metallic, /*out*/ specSampleColor, /*out*/ oneMinusReflectivity);
		half outputAlpha;
		s.Albedo = PreMultiplyAlpha (s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);
		half4 c = UNITY_BRDF_PBS (s.Albedo, specSampleColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
		c.rgb += UNITY_BRDF_GI (s.Albedo, specSampleColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, s.Occlusion, gi);
		c.a = outputAlpha;
		
		//subsurface scattering contribution
		#if TU_SUBSURF
			c.rgb += subsurf(_SubSurfScale, _SubSurfPower, _SubSurfDistort, _SubSurfAtten, s.Backlight.a, s.Albedo, s.Backlight.rgb, s.Normal, viewDir, gi.light.color, gi.light.dir);
		#endif
		
		return c;
	}
#endif

#if TU_LIGHT_SPEC
	//replacement for Unity bridge method to call GI with custom structs
	inline void LightingTU_GI (SurfaceOutputTU s, UnityGIInput data, inout UnityGI gi)
	{
		UNITY_GI(gi, s, data);
	}	

	//custom lighting function to enable SubSurf functionality
	inline half4 LightingTU(SurfaceOutputTU s, half3 viewDir, UnityGI gi)
	{
		s.Normal = normalize(s.Normal);
		
		#if TU_SUBSURF
			//SSS implementation from:  https://colinbarrebrisebois.com/2011/03/07/gdc-2011-approximating-translucency-for-a-fast-cheap-and-convincing-subsurface-scattering-look/	
			
			half fLTScale = _SubSurfScale;//main output scalar
			half iLTPower = _SubSurfPower;//exponent used in power
			half fLTDistortion = _SubSurfDistort;;//how much the surface normal distorts the outgoing light
			half fLightAttenuation = _SubSurfAtten;//how much light attenuates while traveling through the surface (gets multiplied by distance)  
						
			half fLTAmbient = s.Backlight.a;//ambient from texture/material
			half3 fLTThickness = s.Backlight.rgb;//sampled from texture
			
			float3 H = normalize(gi.light.dir + s.Normal * fLTDistortion);
			float vdh = pow(saturate(dot(viewDir, -H)), iLTPower) * fLTScale;
			float3 I = fLightAttenuation * (vdh + fLTAmbient) * fLTThickness;
			half3 backColor = I * gi.light.color;
		#endif
		
		//Unity 'Standard' lighting function, unabridged
		// energy conservation
		half oneMinusReflectivity;
		s.Albedo = EnergyConservationBetweenDiffuseAndSpecular (s.Albedo, s.SpecularColor, /*out*/ oneMinusReflectivity);
		// shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
		// this is necessary to handle transparency in physically correct way - only diffuse component gets affected by alpha
		half outputAlpha;
		s.Albedo = PreMultiplyAlpha (s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);
		half4 c = UNITY_BRDF_PBS (s.Albedo, s.SpecularColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
		c.rgb += UNITY_BRDF_GI (s.Albedo, s.SpecularColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, s.Occlusion, gi);
		c.a = outputAlpha;
		
		#if TU_SUBSURF
			c.rgb += subsurf(_SubSurfScale, _SubSurfPower, _SubSurfDistort, _SubSurfAtten, s.Backlight.a, s.Albedo, s.Backlight.rgb, s.Normal, viewDir, gi.light.color, gi.light.dir);
		#endif
		
		return c;
	}
#endif

#if TU_LIGHT_SPECLEGACY	
	inline half4 LightingTU(SurfaceOutputTU s, half3 lightDir, half3 viewDir, half atten)
	{
		#if TU_BUMPMAP
			s.Normal = normalize(s.Normal);
		#endif
		
		#if TU_SUBSURF
			//SSS implementation from:  https://colinbarrebrisebois.com/2011/03/07/gdc-2011-approximating-translucency-for-a-fast-cheap-and-convincing-subsurface-scattering-look/	
			
			half fLTScale = _SubSurfScale;//main output scalar
			half iLTPower = _SubSurfPower;//exponent used in power
			half fLTDistortion = _SubSurfDistort;;//how much the surface normal distorts the outgoing light
			half fLightAttenuation = _SubSurfAtten;//how much light attenuates while traveling through the surface (gets multiplied by distance)  
			
			//half fLTScale = s.SubSurfParams.r;//main output scalar
			//half iLTPower = s.SubSurfParams.g;//exponent used in power
			//half fLTDistortion = s.SubSurfParams.b;//how much the surface normal distorts the outgoing light
			//half fLightAttenuation = s.SubSurfParams.a;//how much light attenuates while traveling through the surface (gets multiplied by distance)
			
			half fLTAmbient = s.Backlight.a;//ambient from texture/material
			half3 fLTThickness = s.Backlight.rgb;//sampled from texture
			
			float3 H = normalize(lightDir + s.Normal * fLTDistortion);
			float vdh = pow(saturate(dot(viewDir, -H)), iLTPower) * fLTScale;
			float3 I = fLightAttenuation * (vdh + fLTAmbient) * fLTThickness;
			half3 backColor = I * _LightColor0.rgb;
		#endif

		
		s.Smoothness = max(0.01, s.Smoothness);
		//standard blinn-phong lighting model
		//diffuse light intensity, from surface normal and light direction
		half diff = max (0, dot (s.Normal, lightDir));
		//specular light calculations
		half3 h = normalize (lightDir + viewDir);
		float nh = max (0, dot (s.Normal, h));
		float spec = pow (nh, s.Smoothness * 128);
		half3 specCol = spec * s.SpecularColor;
		
		//output fragment color; Unity adds Emission to it through some other method
		half4 c;
		#if TU_ICON
			//diff *= _Multiplier;
		#endif
		c.rgb = ((s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * specCol)) * atten;
		c.a = s.Alpha;
		
		#if TU_SUBSURF
			c.rgb += subsurf(_SubSurfScale, _SubSurfPower, _SubSurfDistort, _SubSurfAtten, s.Backlight.a, s.Albedo, s.Backlight.rgb, s.Normal, viewDir, _LightColor0.rgb, lightDir);
		#endif			
		
		return c;
	}
#endif