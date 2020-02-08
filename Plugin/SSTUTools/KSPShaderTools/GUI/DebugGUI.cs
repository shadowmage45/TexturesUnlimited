using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPShaderTools
{

    public class DebugGUI : MonoBehaviour
    {

        private static Rect windowRect = new Rect(Screen.width - 900, 40, 800, 600);
        private int windowID = 0;

        public void Awake()
        {
            windowID = GetInstanceID();
        }

        public void OnGUI()
        {
            try
            {
                windowRect = GUI.Window(windowID, windowRect, updateWindow, "Textures Unlimited Development Mode");
            }
            catch (Exception e)
            {
                MonoBehaviour.print("Caught exception while rendering TUDebugGUI");
                MonoBehaviour.print(e.Message);
                MonoBehaviour.print(System.Environment.StackTrace);
            }
        }

        private void updateWindow(int id)
        {
            GUILayout.BeginVertical();
            if (addButtonRow("Dump ReflectionData")) { Utils.dumpReflectionData(); }
            if (addButtonRow("Export UV and Model Data")) { TexturesUnlimitedLoader.dumpUVMaps(true); }
            GUILayout.EndVertical();
        }

        private bool addButtonRowToggle(string text, bool value)
        {
            GUILayout.BeginHorizontal();
            GUILayoutOption width = GUILayout.Width(100);
            GUILayout.Label(text, width);
            GUILayout.Label(value.ToString(), width);
            if (GUILayout.Button("Toggle", width))
            {
                value = !value;
            }
            GUILayout.EndHorizontal();
            return value;
        }

        private bool addButtonRow(string text)
        {
            GUILayout.BeginHorizontal();
            GUILayoutOption width = GUILayout.Width(100);
            GUILayout.Label(text, width);
            bool value = GUILayout.Button("Toggle", width);
            GUILayout.EndHorizontal();
            return value;
        }

        private bool addButtonRow(string labelText, string buttonText)
        {
            GUILayout.BeginHorizontal();
            GUILayoutOption width = GUILayout.Width(100);
            GUILayout.Label(labelText, width);
            bool value = GUILayout.Button(buttonText, width);
            GUILayout.EndHorizontal();
            return value;
        }

    }

}
