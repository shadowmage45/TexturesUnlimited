using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPShaderTools
{
    public class KSPFairingTextureSwitch : KSPTextureSwitch
    {
        //really, could probably just move this back to the base class, possibly with a config bool for toggling enable of the secondary updates
        public void Start()
        {
            textureSets.enableCurrentSet(getModelTransforms());
        }
    }
}
