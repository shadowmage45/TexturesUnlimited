using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TexturesUnlimitedFX
{

    public class EffectManager : MonoBehaviour
    {

        private Material finishMat;

        //general settings
        public static bool hdrEnabled = false;

        //bloom settings
        public static bool bloomEnabled = true;
        public static float bloomIntensity = 4f;        
        public static float bloomRadius = 4f;
        public static float softKnee = 0.5f;        
        public static float linearThreshold = 0.15f;
        public static bool antiFlicker = false;
        public static long bloomEffectTime;
        public static double bloomAverageTime;

        public BloomEffect bloom;

        public EffectManager()
        {
            bloom = new BloomEffect();
            finishMat = new Material(KSPShaderTools.TexturesUnlimitedLoader.getShader("TUFX/EffectsCombine"));
        }

        public void OnRenderImage(RenderTexture source, RenderTexture dest)
        {
            if (bloomEnabled)
            {
                bloom.RenderEffect(finishMat, source);
                Graphics.Blit(source, dest, finishMat);
                bloomAverageTime -= bloomAverageTime / 60;
                bloomAverageTime += bloomEffectTime;
            }
            else
            {
                Graphics.Blit(source, dest);
            }
        }

    }

}
