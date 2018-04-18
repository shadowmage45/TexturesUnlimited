using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPShaderTools
{
    public class KSPFairingShader : PartModule
    {
        public string shader = "SSTU/PBR/Metallic";
                
        //really, could probably just move this back to the base class, possibly with a config bool for toggling enable of the secondary updates
        public void Start()
        {
            ModuleProceduralFairing mpf = part.GetComponent<ModuleProceduralFairing>();
            Shader shader = TexturesUnlimitedLoader.getShader(this.shader);
            if (mpf != null && shader != null && mpf.FairingMaterial != null)
            {
                mpf.FairingMaterial.shader = shader;
                if (mpf.FairingConeMaterial != null) { mpf.FairingConeMaterial.shader = shader; }
                MonoBehaviour.print("Adjusted MPF materials!");
                if (mpf.Panels != null && mpf.Panels.Count > 0)//cones are included in regular panels
                {
                    int len = mpf.Panels.Count;
                    for (int i = 0; i < len; i++)
                    {
                        mpf.Panels[i].mat.shader = shader;
                        mpf.Panels[i].go.GetComponent<Renderer>().material.shader = shader;
                    }
                }
            }
        }
    }

}
