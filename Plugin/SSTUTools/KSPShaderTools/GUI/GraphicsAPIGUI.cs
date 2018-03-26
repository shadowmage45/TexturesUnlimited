using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPShaderTools
{
    public class GraphicsAPIGUI : MonoBehaviour
    {

        private Rect windowRect;
        private int instanceID;

        public void OnAwake()
        {

        }

        public void Start()
        {

        }

        public void Destroy()
        {

        }

        public void OnGUI()
        {

        }

        private void closeGUI()
        {
            TexturesUnlimitedLoader.INSTANCE.removeAPICheckGUI();
        }

    }
}
