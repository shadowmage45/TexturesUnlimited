using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TexturesUnlimitedFX
{

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class TexturesUnlimitedFXLoader : MonoBehaviour
    {

        public static TexturesUnlimitedFXLoader INSTANCE;

        private ConfigurationGUI configGUI;
        private static ApplicationLauncherButton debugAppButton;

        public void Start()
        {
            MonoBehaviour.print("TUFXLoader - Start()");
            INSTANCE = this;
            DontDestroyOnLoad(this);
            GameEvents.onLevelWasLoaded.Add(new EventData<GameScenes>.OnEvent(onSceneChange));
        }
        
        public void ModuleManagerPostLoad()
        {
            MonoBehaviour.print("TUFXLoader - MMPostLoad()");
            //grab references to shaders, register events?
        }

        private void onSceneChange(GameScenes scene)
        {
            MonoBehaviour.print("TUFXLoader - onSceneChange()");            
            if (scene == GameScenes.FLIGHT)
            {
                Texture2D tex;
                if (debugAppButton == null)//static reference; track if the button was EVER created, as KSP keeps them even if the addon is destroyed
                {
                    //create a new button
                    tex = GameDatabase.Instance.GetTexture("Squad/PartList/SimpleIcons/RDIcon_fuelSystems-highPerformance", false);
                    debugAppButton = ApplicationLauncher.Instance.AddModApplication(debugGuiEnable, debugGuiDisable, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB, tex);
                }
                else if (debugAppButton != null)
                {
                    //reseat callback refs to the ones from THIS instance of the KSPAddon (old refs were stale, pointing to methods for a deleted class instance)
                    debugAppButton.onTrue = debugGuiEnable;
                    debugAppButton.onFalse = debugGuiDisable;
                }
                EffectManager effect = Camera.main.gameObject.AddOrGetComponent<EffectManager>();
                fixMaterials();
            }
        }

        private void fixMaterials()
        {
            Material[] mats = Resources.FindObjectsOfTypeAll<Material>();
            int len = mats.Length;
            for (int i = 0; i < len; i++)
            {
                Material mat = mats[i];
                if (mat.shader != null && mat.shader.name.StartsWith("KSP/Scenery"))
                {
                    mat.SetOverrideTag("RenderType", "Opaque");
                    if (mat.shader.name == "KSP/Scenery/Specular" ||
                        mat.shader.name == "KSP/Scenery/Diffuse" ||
                        mat.shader.name == "KSP/Scenery/Bumped" ||
                        mat.shader.name == "KSP/Scenery/Bumped Specular" ||
                        mat.shader.name == "KSP/Scenery/Emissive/Diffuse" ||
                        mat.shader.name == "KSP/Scenery/Emissive/Specular" ||
                        mat.shader.name == "KSP/Scenery/Emissive/Bumped Specular")
                    {
                        mat.shader = KSPShaderTools.TexturesUnlimitedLoader.getShader("TU/Metallic");
                        mat.EnableKeyword("TU_STOCK_SPEC");
                        mat.SetFloat("_Metal", 0);
                        mat.SetFloat("_Smoothness", mat.GetFloat("_Shininess"));
                        MonoBehaviour.print("\n"+KSPShaderTools.Debug.getMaterialPropertiesDebug(mat));
                    }
                }
            }
        }

        public static void onHDRToggled()
        {
            MonoBehaviour.print("Toggling HDR");
            Camera[] cams = GameObject.FindObjectsOfType<Camera>();
            int len = cams.Length;
            for (int i = 0; i < len; i++)
            {
                cams[i].allowHDR = EffectManager.hdrEnabled;
            }
        }

        private void debugGuiEnable()
        {
            if (configGUI == null)
            {
                configGUI = this.gameObject.AddOrGetComponent<ConfigurationGUI>();
            }
        }

        public void debugGuiDisable()
        {
            if (configGUI != null)
            {
                GameObject.Destroy(configGUI);
            }
        }

    }

}
