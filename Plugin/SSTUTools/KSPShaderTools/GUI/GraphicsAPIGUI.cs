using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPShaderTools
{
    public class GraphicsAPIGUI : MonoBehaviour
    {

        private Rect windowRect = new Rect(Screen.width/2 - 240, Screen.height/2 - 160, 480, 320);
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
            GUILayout.Label("The Graphics API Detected: "+ SystemInfo.graphicsDeviceType);
            GUILayout.Label("Is unsupported by Textures Unlimited, and some graphics may not render correctly.");
            GUILayout.Label("For best results, use the -force-glcore command line option to start KSP using the OpenGL Core graphics API");
            GUILayout.FlexibleSpace();//push button to bottom of window
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
