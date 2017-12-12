using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;

namespace KSPShaderTools
{
    public class SSTURecolorGUI : PartModule, IPartGeometryUpdated, IPartTextureUpdated
    {

        private static GameObject guiObject;
        private static CraftRecolorGUI gui;

        private WaitForSeconds coroutineYield;//don't create a new one every frame..
        private bool coroutineRunning = false;//just tracks if co-routing was created

        [KSPEvent(guiName ="Open Recoloring GUI", guiActive = false, guiActiveEditor = true)]
        public void recolorGUIEvent()
        {
            bool open = true;
            if (guiObject != null)
            {
                //apparently delegates can/do use reference/memory location ==, which is exactl what is needed in this situation
                if (gui.guiCloseAction == recolorClose)
                {
                    open = false;
                }
                //kill existing GUI before opening new one
                gui.guiCloseAction();
                GameObject.Destroy(guiObject);
                guiObject = null;
            }
            if (open)
            {
                guiObject = new GameObject("SSTURecolorGUI");
                gui = guiObject.AddComponent<CraftRecolorGUI>();
                gui.openGUIPart(part);
                gui.guiCloseAction = recolorClose;
            }
        }

        //IPartGeometryUpdated callback method
        public void geometryUpdated(Part part)
        {
            if (part == this.part && gui!=null)
            {
                gui.refreshGui(part);
            }
        }

        //IPartTextureUpdated callback method
        public void textureUpdated(Part part)
        {
            if (part == this.part)
            {
                updateButtonVisibility();
                if (gui != null)
                {
                    gui.refreshGui(part);
                }
            }
        }

        public void recolorClose()
        {
            if (guiObject != null)
            {
                gui.closeGui();
                gui = null;
                GameObject.Destroy(guiObject);
            }
        }

        public override string GetInfo()
        {
            return "This part has configurable colors.";
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            GameEvents.onEditorShipModified.Add(new EventData<ShipConstruct>.OnEvent(editorVesselModified));
            updateButtonVisibility();
        }

        public void Start()
        {
            //called from both OnStart and Start -- as external modules might not yet be initialized during OnStart() (but also might have), need to validate after both
            updateButtonVisibility();
        }

        public void OnDestroy()
        {
            GameEvents.onEditorShipModified.Remove(new EventData<ShipConstruct>.OnEvent(editorVesselModified));
        }

        public void editorVesselModified(ShipConstruct ship)
        {
            updateButtonVisibility();
            if (gui != null)
            {
                gui.refreshGui(part);
            }
        }

        private void updateButtonVisibility()
        {
            if (!coroutineRunning)
            {
                coroutineRunning = true;
                if (coroutineYield == null)
                {
                    coroutineYield = new WaitForSeconds(0);
                }
                StartCoroutine(updateButtonVisibility2());
            }
        }

        private IEnumerator updateButtonVisibility2()
        {
            yield return coroutineYield;
            coroutineRunning = false;
            IRecolorable[] ircs = part.GetComponents<IRecolorable>();
            int len = ircs.Length;
            IRecolorable irc;
            TextureSet ts;
            string[] sections;
            bool enabled = false;
            for (int i = 0; i < len && !enabled; i++)
            {
                irc = ircs[i];
                sections = irc.getSectionNames();
                int len2 = sections.Length;
                for (int k = 0; k < len2 && !enabled; k++)
                {
                    ts = irc.getSectionTexture(sections[k]);
                    if (ts == null)
                    {
                        //apparently null texture sets aren't really an error in some of the SSTU modules (TODO -- fix SSTU modules to at least return a default - empty texture set (no textures/meshes))
                        //MonoBehaviour.print("ERROR: Texture set was null for recolorable section: " + sections[k] + " in module: " + irc.GetType() + " in part:" + part);
                    }
                    if (ts == null) { continue; }
                    //both for-loops will break the first time enabled==true
                    enabled = ts.supportsRecoloring;
                }
            }
            Events[nameof(recolorGUIEvent)].guiActiveEditor = enabled;
        }
    }
}
