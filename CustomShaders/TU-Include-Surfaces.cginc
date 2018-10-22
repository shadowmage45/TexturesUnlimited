//preprocessor macro triggered surface functions, to allow for sharing of code across multiple shader variants
#if TU_SURF_MET		
	void surf (Input IN, inout SurfaceOutputTU o)
	{
		#if TU_ICON
			//as the clip test needs to be performed regardless of the surface properties, run it first as an early exit
			//should save some texture sampling and processing of data that would just be discarded anyway.
			float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
			
			#ifdef SHADER_API_GLCORE				
				screenUV.y = 1 - screenUV.y;
			#endif			
			
			if(screenUV.x < _MinX || screenUV.y < _MinY || screenUV.x > _MaxX || screenUV.y > _MaxY)
			{
				clip(-1);
				return;
			}
		#endif
		//standard texture samplers used regardless of keywords...
		fixed4 color = tex2D(_MainTex,(IN.uv_MainTex));
		fixed4 specSample = tex2D(_MetallicGlossMap, (IN.uv_MainTex));
		
		//metal ALWAYS comes from MetallicGlossMap.r
		fixed metal = specSample.r;
		
		//if 'stock specular' mode is enabled, pull spec value from alpha channel of diffuse shader
		//else pull it from the alpha channel of the metallic gloss map
		#if TU_STD_SPEC
			fixed smooth = specSample.a;
		#endif
		#if TU_STOCK_SPEC
			fixed smooth = color.a;
		#endif
		
		//new TU recolor mode based on normalization maps
		#if TU_RECOLOR_STANDARD
		
			//RGBA value from the mask; RGB = recoloring channels, A = diffuse luminance normalization data
			fixed4 mask = tex2D(_MaskTex, (IN.uv_MainTex));
			//
			fixed diffuseNorm = getUserValue(mask, _DiffuseNorm.x, _DiffuseNorm.y, _DiffuseNorm.z);
			fixed metallicNorm = getUserValue(mask, _MetalNorm.x, _MetalNorm.y, _MetalNorm.z);
			fixed specularNorm = getUserValue(mask, _SmoothnessNorm.x, _SmoothnessNorm.y, _SmoothnessNorm.z);
			
			//same for specular and metallic if normalization for those channels is enabled
			#if TU_RECOLOR_NORM || TU_RECOLOR_NORM_INPUT
				fixed4 specMetNormData = tex2D(_MetalGlossNormMask, (IN.uv_MainTex));
				diffuseNorm += mask.a;
				metallicNorm += specMetNormData.r;
				specularNorm += specMetNormData.a;
			#endif
			
			fixed metalMaskFactor = 1;
			fixed specMaskFactor = 1;
			
			//sample/calculate mix factors for user-specified spec and metal values if input-masking setting is enabled
			#if TU_RECOLOR_INPUT || TU_RECOLOR_NORM_INPUT
				fixed4 specMaskValues = tex2D(_MetalGlossInputMask, IN.uv_MainTex);
				metalMaskFactor = specMaskValues.r;
				specMaskFactor = specMaskValues.a;
			#endif
			
			o.Albedo = recolorStandard(color.rgb, mask, diffuseNorm, _MaskColor1, _MaskColor2, _MaskColor3);
			o.Metallic = recolorStandard(metal, mask * metalMaskFactor, metallicNorm, _MaskMetallic.r, _MaskMetallic.g, _MaskMetallic.b);
			o.Smoothness = recolorStandard(smooth, mask * specMaskFactor, specularNorm, _MaskColor1.a, _MaskColor2.a, _MaskColor3.a);
			
		#endif			
		//no recoloring enabled -- use standard texture sampling -- use the values directly from the source textures
		#if TU_RECOLOR_OFF
			o.Albedo = color.rgb;
			o.Smoothness = smooth;
			o.Metallic = metal;
		#endif
		
		//If subsurf is enabled, this is a bit of pre-setup to pass params to lighting function
		#if TU_SUBSURF
			fixed4 thick = tex2D(_Thickness, (IN.uv_MainTex));
			o.Backlight.rgb = thick.rgb;
			//TODO -- can sample property in lighting function; no need to pass through alpha channel, save a register
			o.Backlight.a = _SubSurfAmbient;
		#endif
		
		//normal map always sampled and assigned directly to surface
		fixed3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
		normal.x *= _NormalFlipX;
		normal.y *= _NormalFlipY;
		o.Normal = normal;
		
		//ambient occlusion always sampled and assigned directly to surface
		fixed4 ao = tex2D(_AOMap, (IN.uv_MainTex));
		o.Occlusion = ao.g;
		
		//emission always sampled and assigned to surface along with stock part-highlighting functionality
		fixed4 glow = tex2D(_Emissive, (IN.uv_MainTex));
		o.Emission = glow.rgb * glow.aaa * _EmissiveColor.rgb *_EmissiveColor.aaa + stockEmit(IN.viewDir, normal, _RimColor, _RimFalloff, _TemperatureColor) * _Opacity;
		
		//controlled directly by shader property
		o.Alpha = _Opacity;
		#if TU_TRANSPARENT
			o.Alpha *= _Color.a * color.a;
		#endif
				
		//apply the standard shader param multipliers to the sampled/computed values.
		o.Albedo *= _Color.rgb;
		fixed4 fog = UnderwaterFog(IN.worldPos, o.Albedo);
		o.Albedo = fog.rgb;
		o.Emission *= fog.a;
		o.Metallic *= _Metal;
		o.Smoothness *= _Smoothness;
	}
#endif

#if TU_SURF_SPEC
	void surf (Input IN, inout SurfaceOutputTU o)
	{
		#if TU_ICON
			//as the clip test needs to be performed regardless of the surface properties, run it first as an early exit
			//should save some texture sampling and processing of data that would just be discarded anyway.
			float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
			
			#ifdef SHADER_API_GLCORE				
				screenUV.y = 1 - screenUV.y;
			#endif			
			
			if(screenUV.x < _MinX || screenUV.y < _MinY || screenUV.x > _MaxX || screenUV.y > _MaxY)
			{
				clip(-1);
				return;
			}
		#endif
		//standard texture samplers used regardless of keywords...
		fixed4 color = tex2D(_MainTex,(IN.uv_MainTex));
		fixed4 specSample = tex2D(_SpecGlossMap, (IN.uv_MainTex));
		fixed3 glossColor = specSample.rgb;
		
		#if TU_STD_SPEC
			fixed smooth = specSample.a;
		#endif
		#if TU_STOCK_SPEC
			fixed smooth = color.a;
		#endif
		
		//new TU recolor mode based on normalization maps
		#if TU_RECOLOR_STANDARD
		
			//RGBA value from the mask; RGB = recoloring channels, A = diffuse luminance normalization data
			fixed4 mask = tex2D(_MaskTex, (IN.uv_MainTex));
			fixed diffuseNorm = getUserValue(mask, _DiffuseNorm.x, _DiffuseNorm.y, _DiffuseNorm.z);
			fixed glossNorm = getUserValue(mask, _SpecularNorm.x, _SpecularNorm.y, _SpecularNorm.z);
			fixed smoothNorm = getUserValue(mask, _SmoothnessNorm.x, _SmoothnessNorm.y, _SmoothnessNorm.z);
			
			//same for specular and metallic if normalization for those channels is enabled
			#if TU_RECOLOR_NORM || TU_RECOLOR_NORM_INPUT
				fixed4 specGlossNormData = tex2D(_SpecGlossNormMask, (IN.uv_MainTex));
				diffuseNorm += mask.a;
				glossNorm += specGlossNormData.r;
				smoothNorm += specGlossNormData.a;					
			#endif
			
			fixed glossMaskFactor = 1;
			fixed smoothMaskFactor = 1;
			
			//sample/calculate mix factors for user-specified spec and metal values if input-masking setting is enabled
			#if TU_RECOLOR_INPUT || TU_RECOLOR_NORM_INPUT
				fixed4 specMaskValues = tex2D(_SpecGlossInputMask, IN.uv_MainTex);
				glossMaskFactor = specMaskValues.r;
				smoothMaskFactor = specMaskValues.a;
			#endif
			
			fixed3 custSpec;
			o.Albedo = recolorStandardSpecularToMetallic(color.rgb, glossColor.rgb, mask, _MaskMetallic, diffuseNorm, glossNorm, glossMaskFactor, _MaskColor1.rgb, _MaskColor2.rgb, _MaskColor3.rgb, custSpec);
			o.SpecularColor = custSpec;
			// o.Albedo = recolorStandard(color.rgb, mask, diffuseNorm, _MaskColor1.rgb, _MaskColor2.rgb, _MaskColor3.rgb);				
			// o.SpecularColor = recolorStandard(glossColor, mask * glossMaskFactor, glossNorm, _MaskSpec1.rgb, _MaskSpec2.rgb, _MaskSpec3.rgb);
			o.Smoothness = recolorStandard(smooth, mask * smoothMaskFactor, smoothNorm, _MaskColor1.a, _MaskColor2.a, _MaskColor3.a);
			
		#endif
		//no recoloring enabled -- use standard texture sampling -- use the values directly from the source textures
		#if TU_RECOLOR_OFF
			o.Albedo = color.rgb;
			o.Smoothness = smooth;
			o.SpecularColor = glossColor;
		#endif
		
		//If subsurf is enabled, this is a bit of pre-setup to pass params to lighting function
		#if TU_SUBSURF
			fixed4 thick = tex2D(_Thickness, (IN.uv_MainTex));
			o.Backlight.rgb = thick.rgb;
			//TODO -- can sample property in lighting function; no need to pass through alpha channel, save a register
			o.Backlight.a = _SubSurfAmbient;
		#endif
		
		//normal map always sampled and assigned directly to surface
		fixed3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
		normal.x *= _NormalFlipX;
		normal.y *= _NormalFlipY;
		o.Normal = normal;
		
		//ambient occlusion always sampled and assigned directly to surface
		fixed4 ao = tex2D(_AOMap, (IN.uv_MainTex));
		o.Occlusion = ao.g;
		
		//emission always sampled and assigned to surface along with stock part-highlighting functionality
		fixed4 glow = tex2D(_Emissive, (IN.uv_MainTex));
		o.Emission = glow.rgb * glow.aaa * _EmissiveColor.rgb *_EmissiveColor.aaa + stockEmit(IN.viewDir, normal, _RimColor, _RimFalloff, _TemperatureColor) * _Opacity;
		
		//controlled directly by shader property
		o.Alpha = _Opacity;
		#if TU_TRANSPARENT
			o.Alpha *= _Color.a * color.a;
		#endif
		
		//apply the standard shader param multipliers to the sampled/computed values.
		o.Albedo *= _Color.rgb;
		fixed4 fog = UnderwaterFog(IN.worldPos, o.Albedo);
		o.Albedo = fog.rgb;
		o.Emission *= fog.a;
		o.Smoothness *= _Smoothness;
		o.SpecularColor *= _GlossColor;
	}
#endif