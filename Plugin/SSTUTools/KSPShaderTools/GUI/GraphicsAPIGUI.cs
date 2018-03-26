using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPShaderTools
{
    public class GraphicsAPIGUI : MonoBehaviour
    {

        private Rect windowRect = new Rect(Screen.width - 500, 40, 480, 320);
        private int instanceID;

        private bool open = false;

        public void Awake()
        {
            instanceID = GetInstanceID();
        }

        public void Start()
        {
            //noop
        }

        public void Destroy()
        {
            //noop
        }

        public void OnGUI()
        {
            if (!open) { return; }
            windowRect = GUI.Window(instanceID, windowRect, drawWindow, "Textures Unlimited - GFX API Check");
        }

        private void drawWindow(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Graphics API Detected: "+ SystemInfo.graphicsDeviceType);
            GUILayout.Label("is unsupported by Textures Unlimited, and may not render correctly.");
            GUILayout.Label("For best results, use the -force-glcore command line option to start KSP using the OpenGL Core graphics API");
            if (GUILayout.Button("Acknowledge & Close"))
            {
                closeGUI();
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        internal void openGUI()
        {
            open = true;
        }

        private void closeGUI()
        {
            open = false;
            TexturesUnlimitedLoader.INSTANCE.removeAPICheckGUI();
        }

    }
}
