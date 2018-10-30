using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TexturesUnlimitedFX
{
    public class BloomEffect
    {

        static class Uniforms
        {
            internal static readonly int _AutoExposure = Shader.PropertyToID("_AutoExposure");
            internal static readonly int _Threshold = Shader.PropertyToID("_Threshold");
            internal static readonly int _Curve = Shader.PropertyToID("_Curve");
            internal static readonly int _PrefilterOffs = Shader.PropertyToID("_PrefilterOffs");
            internal static readonly int _SampleScale = Shader.PropertyToID("_SampleScale");
            internal static readonly int _BaseTex = Shader.PropertyToID("_BaseTex");
            internal static readonly int _BloomTex = Shader.PropertyToID("_BloomTex");
            internal static readonly int _Bloom_Settings = Shader.PropertyToID("_Bloom_Settings");
            internal static readonly int _Bloom_DirtTex = Shader.PropertyToID("_Bloom_DirtTex");
            internal static readonly int _Bloom_DirtIntensity = Shader.PropertyToID("_Bloom_DirtIntensity");
        }

        const int k_MaxPyramidBlurLevel = 16;
        readonly RenderTexture[] m_BlurBuffer1 = new RenderTexture[k_MaxPyramidBlurLevel];
        readonly RenderTexture[] m_BlurBuffer2 = new RenderTexture[k_MaxPyramidBlurLevel];

        private Material bloomMat;
        RenderTexture bloomTex=null;
        Stopwatch stopwatch;

        public BloomEffect()
        {
            bloomMat = new Material(KSPShaderTools.TexturesUnlimitedLoader.getShader("Hidden/Post FX/Bloom"));
            stopwatch = new Stopwatch();
        }

        //based on -- https://catlikecoding.com/unity/tutorials/advanced-rendering/bloom/
        //or... just copied from unity bloom effect setup
        public void RenderEffect(Material finishMat, RenderTexture source)
        {
            //Adapted unity post-process bloom shader code
            stopwatch.Start();
            bloomMat.shaderKeywords = null;
            if (bloomTex != null)
            {
                RenderTexture.ReleaseTemporary(bloomTex);
                bloomTex = null;
            }

            // Do bloom on a half-res buffer, full-res doesn't bring much and kills performances on
            // fillrate limited platforms
            var tw = source.width / 2;
            var th = source.height / 2;

            // Blur buffer format
            // TODO: Extend the use of RGBM to the whole chain for mobile platforms
            var useRGBM = Application.isMobilePlatform;
            var rtFormat = useRGBM
                ? RenderTextureFormat.Default
                : RenderTextureFormat.DefaultHDR;

            // Determine the iteration count
            float logh = Mathf.Log(th, 2f) + EffectManager.bloomRadius - 8f;
            int logh_i = (int)logh;
            int iterations = Mathf.Clamp(logh_i, 1, k_MaxPyramidBlurLevel);

            // Uupdate the shader properties
            float lthresh = EffectManager.linearThreshold;
            bloomMat.SetFloat(Uniforms._Threshold, lthresh);

            float knee = lthresh * EffectManager.softKnee + 1e-5f;
            var curve = new Vector3(lthresh - knee, knee * 2f, 0.25f / knee);
            bloomMat.SetVector(Uniforms._Curve, curve);

            bloomMat.SetFloat(Uniforms._PrefilterOffs, EffectManager.antiFlicker ? -0.5f : 0f);

            float sampleScale = 0.5f + logh - logh_i;
            bloomMat.SetFloat(Uniforms._SampleScale, sampleScale);

            // TODO: Probably can disable antiFlicker if TAA is enabled - need to do some testing
            if (EffectManager.antiFlicker)
                bloomMat.EnableKeyword("ANTI_FLICKER");

            // Prefilter pass
            RenderTexture prefiltered = RenderTexture.GetTemporary(tw, th, 0, rtFormat);
            Graphics.Blit(source, prefiltered, bloomMat, 0);

            // Construct a mip pyramid
            RenderTexture last = prefiltered;

            for (int level = 0; level < iterations; level++)
            {
                m_BlurBuffer1[level] = RenderTexture.GetTemporary(
                        last.width / 2, last.height / 2, 0, rtFormat
                        );

                int pass = (level == 0) ? 1 : 2;
                Graphics.Blit(last, m_BlurBuffer1[level], bloomMat, pass);

                last = m_BlurBuffer1[level];
            }

            // Upsample and combine loop
            for (int level = iterations - 2; level >= 0; level--)
            {
                RenderTexture baseTex = m_BlurBuffer1[level];
                bloomMat.SetTexture(Uniforms._BaseTex, baseTex);

                m_BlurBuffer2[level] = RenderTexture.GetTemporary(
                        baseTex.width, baseTex.height, 0, rtFormat
                        );

                Graphics.Blit(last, m_BlurBuffer2[level], bloomMat, 3);
                last = m_BlurBuffer2[level];
            }

            bloomTex = last;

            // Release the temporary buffers
            for (int i = 0; i < k_MaxPyramidBlurLevel; i++)
            {
                if (m_BlurBuffer1[i] != null)
                    RenderTexture.ReleaseTemporary(m_BlurBuffer1[i]);

                if (m_BlurBuffer2[i] != null && m_BlurBuffer2[i] != bloomTex)
                    RenderTexture.ReleaseTemporary(m_BlurBuffer2[i]);

                m_BlurBuffer1[i] = null;
                m_BlurBuffer2[i] = null;
            }

            RenderTexture.ReleaseTemporary(prefiltered);
            //end unity post process code
            //custom blit setup for 'combine' shader (replacement for uber shader)
            finishMat.SetFloat("_BloomIntensity", EffectManager.bloomIntensity);
            finishMat.SetFloat("_BloomSampleSize", sampleScale);
            finishMat.SetTexture("_MainTex", source);
            finishMat.SetTexture("_BloomTex", bloomTex);
            stopwatch.Stop();            
            EffectManager.bloomEffectTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();
        }

    }
}
