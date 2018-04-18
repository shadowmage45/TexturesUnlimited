using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPShaderTools
{
    public class KSPFairingShader : PartModule
    {

        //[KSPField]
        //public string shader = "SSTU/PBR/Metallic";

        [KSPField]
        public string textureSet;

        [KSPField]
        public int materialIndex = 0;
                
        //really, could probably just move this back to the base class, possibly with a config bool for toggling enable of the secondary updates
        public void Start()
        {
            ModuleProceduralFairing mpf = part.GetComponent<ModuleProceduralFairing>();
            TextureSet ts = TexturesUnlimitedLoader.getTextureSet(textureSet);
            if (ts != null)
            {
                ts.enable(part.transform.FindRecursive("model"), ts.maskColors);
                TextureSetMaterialData tsmd = ts.textureData[materialIndex];
                if (mpf != null)
                {
                    if (mpf.FairingMaterial != null && mpf.FairingConeMaterial != null)
                    {
                        tsmd.apply(mpf.FairingMaterial);
                        tsmd.apply(mpf.FairingConeMaterial);
                    }
                    if (mpf.Panels != null && mpf.Panels.Count > 0)//cones are included in regular panels
                    {
                        int len = mpf.Panels.Count;
                        for (int i = 0; i < len; i++)
                        {
                            tsmd.apply(mpf.Panels[i].mat);
                            tsmd.apply(mpf.Panels[i].go.GetComponent<Renderer>().material);
                        }
                    }
                }
            }
            //prev shader-only code...
            //Shader shader = TexturesUnlimitedLoader.getShader(this.shader);
            //if (mpf != null && shader != null && mpf.FairingMaterial != null)
            //{
            //    mpf.FairingMaterial.shader = shader;
            //    if (mpf.FairingConeMaterial != null) { mpf.FairingConeMaterial.shader = shader; }
            //    MonoBehaviour.print("Adjusted MPF materials!");
            //    if (mpf.Panels != null && mpf.Panels.Count > 0)//cones are included in regular panels
            //    {
            //        int len = mpf.Panels.Count;
            //        for (int i = 0; i < len; i++)
            //        {
            //            mpf.Panels[i].mat.shader = shader;
            //            mpf.Panels[i].go.GetComponent<Renderer>().material.shader = shader;
            //        }
            //    }
            //}
        }
    }

}
