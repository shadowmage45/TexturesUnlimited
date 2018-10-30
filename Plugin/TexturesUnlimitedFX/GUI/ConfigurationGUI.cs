using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TexturesUnlimitedFX
{

    public class ConfigurationGUI : MonoBehaviour
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
                windowRect = GUI.Window(windowID, windowRect, updateWindow, "SSTUReflectionDebug");
            }
            catch (Exception e)
            {
                MonoBehaviour.print("Caught exception while rendering SSTUReflectionDebug GUI");
                MonoBehaviour.print(e.Message);
                MonoBehaviour.print(System.Environment.StackTrace);
            }
        }

        private void updateWindow(int id)
        {
            bool hdr = addButtonRowToggle("HDR", EffectManager.hdrEnabled);
            if (hdr != EffectManager.hdrEnabled)
            {
                EffectManager.hdrEnabled = hdr;
                TexturesUnlimitedFXLoader.onHDRToggled();
            }
            EffectManager.bloomEnabled = addButtonRowToggle("BloomEnabled", EffectManager.bloomEnabled);
            EffectManager.bloomIntensity = addSliderRow("BloomIntensity", EffectManager.bloomIntensity, 0, 10);
            EffectManager.bloomRadius = addSliderRow("BloomRad", EffectManager.bloomRadius, 0, 8);
            EffectManager.linearThreshold = addSliderRow("Threshold", EffectManager.linearThreshold, 0, 2);
            EffectManager.softKnee = addSliderRow("SoftKnee", EffectManager.softKnee, 0, 1);
            EffectManager.antiFlicker = addButtonRowToggle("Anti Flicker", EffectManager.antiFlicker);
            addLabelRow("Bloom time(ms): " + EffectManager.bloomEffectTime);
            addLabelRow("Bloom avg(ms): " + (EffectManager.bloomAverageTime / 60d));
            GUI.DragWindow();
        }

        private void addLabelRow(string text)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(text);
            GUILayout.EndHorizontal();
        }

        private float addSliderRow(string text, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayoutOption width = GUILayout.Width(100);
            GUILayout.Label(text, width);
            GUILayout.Label(value.ToString(), width);
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(300));
            GUILayout.EndHorizontal();
            return value;
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

    }

}
