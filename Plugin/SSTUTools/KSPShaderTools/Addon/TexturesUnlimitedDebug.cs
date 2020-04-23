using KSP.UI.Screens;
using KSPShaderTools.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPShaderTools.Addon
{

    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class TexturesUnlimitedDebug : MonoBehaviour
    {

        private DebugGUI gui;
        private bool debug = false;
        private static ApplicationLauncherButton debugAppButton;

        public void Awake()
        {
            debug = TUGameSettings.Debug;
            Texture2D tex;
            if (debugAppButton == null && debug)//static reference; track if the button was EVER created, as KSP keeps them even if the addon is destroyed
            {
                //TODO create an icon for TU debug App-Launcher button
                //create a new button
                tex = GameDatabase.Instance.GetTexture("Squad/PartList/SimpleIcons/RDIcon_fuelSystems-highPerformance", false);
                debugAppButton = ApplicationLauncher.Instance.AddModApplication(debugGuiEnable, debugGuiDisable, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB, tex);
            }
            else if (debugAppButton != null && debug)
            {
                //reseat callback refs to the ones from THIS instance of the KSPAddon (old refs were stale, pointing to methods for a deleted class instance)
                debugAppButton.onTrue = debugGuiEnable;
                debugAppButton.onFalse = debugGuiDisable;
            }
            else//button not null, but not debug mode; needs to be removed
            {
                if (debugAppButton != null)
                {
                    ApplicationLauncher.Instance.RemoveModApplication(debugAppButton);
                }
            }
        }

        private void debugGuiEnable()
        {
            gui = gameObject.AddComponent<DebugGUI>();
        }

        public void debugGuiDisable()
        {
            GameObject.Destroy(gui);
            gui = null;
        }

    }

}
