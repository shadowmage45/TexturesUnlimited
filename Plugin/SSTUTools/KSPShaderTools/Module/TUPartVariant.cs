using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPShaderTools
{
    public class TUPartVariant : PartModule, IRecolorable
    {
        
        private EventData<Part, PartVariant>.OnEvent applied;
        private EventData<Part, PartVariant>.OnEvent editorApplied;
        private EventData<AvailablePart, PartVariant>.OnEvent editorDefaultApplied;

        //set by part-variant; only updated on variant change
        //config value should match the texture set for the default variant for the part
        [KSPField(isPersistant = true)]
        public string textureSet = string.Empty;

        //persistent color data
        [KSPField(isPersistant = true)]
        public string persistentData = string.Empty;

        private RecoloringData[] customColors;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            loadPersistentData(persistentData);
            init();
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            saveColors(customColors);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            init();
        }

        private void init()
        {
            if (customColors == null)
            {
                customColors = new RecoloringData[3];
            }
            if (applied == null)
            {
                //TODO only subscribe based on scene?
                //GameEvents.onVariantApplied.Add(applied = new EventData<Part, PartVariant>.OnEvent(variantApplied));
                GameEvents.onEditorVariantApplied.Add(editorApplied = new EventData<Part, PartVariant>.OnEvent(editorVariantApplied));
                GameEvents.onEditorDefaultVariantChanged.Add(editorDefaultApplied = new EventData<AvailablePart, PartVariant>.OnEvent(editorDefaultVariantApplied));
            }
        }

        public void OnDestroy()
        {
            //TODO only remove if not null
           // GameEvents.onVariantApplied.Remove(applied);
            GameEvents.onEditorVariantApplied.Remove(editorApplied);
            GameEvents.onEditorDefaultVariantChanged.Remove(editorDefaultApplied);
        }

        private void variantApplied(Part part, PartVariant variant)
        {
            MonoBehaviour.print("Variant applied: " + variant.Name);
            string setName = variant.GetExtraInfoValue("TU-TextureSet");
            if (!string.IsNullOrEmpty(setName))
            {
                applyConfig(part.transform.FindRecursive("model"), setName);
                textureSet = setName;
            }
        }

        private void editorVariantApplied(Part part, PartVariant variant)
        {
            MonoBehaviour.print("EditorVariant applied: " + variant.Name);
            string setName = variant.GetExtraInfoValue("TU-TextureSet");
            if (!string.IsNullOrEmpty(setName))
            {
                applyConfig(part.transform.FindRecursive("model"), setName);
                textureSet = setName;
            }
        }

        private void editorDefaultVariantApplied(AvailablePart part, PartVariant variant)
        {
            MonoBehaviour.print("EditorDefaultVariant applied: " + variant.Name);
            string setName = variant.GetExtraInfoValue("TU-TextureSet");
            if (!string.IsNullOrEmpty(setName))
            {
                applyConfig(part.partPrefab.transform.FindRecursive("model"), setName);
                applyConfig(part.iconPrefab.transform.FindRecursive("model"), setName, true);
                textureSet = setName;
            }
        }

        private void applyConfig(Transform root, string textureSetName, bool useIconShaders = false)
        {
            //TODO -- run-time dynamic swapping to icon shaders
            //  will require some additional support in the back-end code somewhere
            TextureSet set = TexturesUnlimitedLoader.getTextureSet(textureSetName);
            if (set == null)
            {
                MonoBehaviour.print("ERROR: Could not locate texture set for name: " + textureSetName);
                return;
            }
            customColors = set.maskColors;
            saveColors(customColors);
            set.enable(root, customColors);
        }

        public string[] getSectionNames()
        {
            return new string[] { "Stock Variant" };
        }

        public RecoloringData[] getSectionColors(string name)
        {
            return customColors;
        }

        public TextureSet getSectionTexture(string name)
        {
            return TexturesUnlimitedLoader.getTextureSet(textureSet);
        }

        public void setSectionColors(string name, RecoloringData[] colors)
        {
            customColors = colors;
            saveColors(customColors);
        }
        
        private void loadPersistentData(string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                string[] colorSplits = data.Split(';');
                int len = colorSplits.Length;
                customColors = new RecoloringData[len];
                for (int i = 0; i < len; i++)
                {
                    customColors[i] = new RecoloringData(colorSplits[i]);
                }
            }
            else if(customColors == null)
            {
                customColors = new RecoloringData[3];
            }
        }

        private void saveColors(RecoloringData[] colors)
        {
            if (colors == null || colors.Length == 0) { return; }
            int len = colors.Length;
            string data = string.Empty;
            for (int i = 0; i < len; i++)
            {
                if (i > 0) { data = data + ";"; }
                data = data + colors[i].getPersistentData();
            }
            persistentData = data;
        }

    }
}
