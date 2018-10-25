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

        [KSPField(isPersistant = true)]
        public string modelShaderSet = string.Empty;

        //persistent color data
        [KSPField(isPersistant = true)]
        public string persistentData = string.Empty;

        //if setup to interact with stock fairing module
        [KSPField(isPersistant = true)]
        public bool stockFairing = false;

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
                GameEvents.onVariantApplied.Add(applied = new EventData<Part, PartVariant>.OnEvent(variantApplied));
                GameEvents.onEditorVariantApplied.Add(editorApplied = new EventData<Part, PartVariant>.OnEvent(editorVariantApplied));
                GameEvents.onEditorDefaultVariantChanged.Add(editorDefaultApplied = new EventData<AvailablePart, PartVariant>.OnEvent(editorDefaultVariantApplied));
            }
            //application of the initial/default texture set should be handled by onVariantApplied being called when the base variant is applied?
        }

        public void OnDestroy()
        {
            //TODO only remove if not null
            GameEvents.onVariantApplied.Remove(applied);
            GameEvents.onEditorVariantApplied.Remove(editorApplied);
            GameEvents.onEditorDefaultVariantChanged.Remove(editorDefaultApplied);
        }

        private void variantApplied(Part part, PartVariant variant)
        {
            MonoBehaviour.print("Variant applied: " + variant.Name);
            TextureSet set = getSet(variant);
            if (set != null)
            {
                applyConfig(part.transform.FindRecursive("model"), set, true);
            }
        }

        private void editorVariantApplied(Part part, PartVariant variant)
        {
            MonoBehaviour.print("EditorVariant applied: " + variant.Name);
            TextureSet set = getSet(variant);
            if (set != null)
            {
                applyConfig(part.transform.FindRecursive("model"), set, true);
            }
        }

        private void editorDefaultVariantApplied(AvailablePart part, PartVariant variant)
        {
            MonoBehaviour.print("EditorDefaultVariant applied: " + variant.Name);
            TextureSet set = getSet(variant);
            if (set != null)
            {
                applyConfig(part.partPrefab.transform.FindRecursive("model"), set, true);
                applyConfig(part.iconPrefab.transform.FindRecursive("model"), set, true, true);
            }
        }

        private TextureSet getSet(PartVariant variant)
        {
            string setName = variant.GetExtraInfoValue("TU-TextureSet");
            TextureSet set = null;
            if (!string.IsNullOrEmpty(setName))
            {
                set = TexturesUnlimitedLoader.getTextureSet(setName);
                textureSet = setName;
                modelShaderSet = string.Empty;
                return set;
            }
            setName = variant.GetExtraInfoValue("TU-ModelShader");
            if (!string.IsNullOrEmpty(setName))
            {
                set = TexturesUnlimitedLoader.getModelShaderTextureSet(setName);
                modelShaderSet = setName;
                textureSet = string.Empty;
                return set;
            }
            return null;
        }

        private TextureSet getSet()
        {
            TextureSet set = null;
            if (!string.IsNullOrEmpty(textureSet) && (set = TexturesUnlimitedLoader.getTextureSet(textureSet))!=null)
            {
                modelShaderSet = string.Empty;
                return set;
            }
            if (!string.IsNullOrEmpty(modelShaderSet) && (set = TexturesUnlimitedLoader.getModelShaderTextureSet(modelShaderSet)) !=null)
            {                
                textureSet = string.Empty;
                return set;
            }
            return null;
        }

        private void applyConfig(Transform root, TextureSet set, bool useSetColors, bool useIconShaders = false)
        {            
            RecoloringData[] colors = useSetColors? set.maskColors : customColors;
            if (useSetColors)
            {
                customColors = set.maskColors;
                saveColors(customColors);
            }
            //apply the texture set to the base model (and trusses?)
            set.enable(root, colors, useIconShaders);
            if (stockFairing)
            {
                TextureSetMaterialData tsmd = set.textureData[0];                
                //adjust the already existing fairing materials and fairing panels
                ModuleProceduralFairing mpf = part.GetComponent<ModuleProceduralFairing>();
                if (mpf != null)
                {
                    Material mat;
                    if (mpf.FairingMaterial != null && mpf.FairingConeMaterial != null)
                    {
                        mat = mpf.FairingMaterial;
                        tsmd.apply(mat, useIconShaders);
                        tsmd.applyRecoloring(mat, colors);
                        mat = mpf.FairingConeMaterial;
                        tsmd.apply(mat, useIconShaders);
                        tsmd.applyRecoloring(mat, colors);
                    }
                    if (mpf.Panels != null && mpf.Panels.Count > 0)//cones are included in regular panels
                    {
                        int len = mpf.Panels.Count;
                        for (int i = 0; i < len; i++)
                        {
                            mat = mpf.Panels[i].mat;
                            tsmd.apply(mat, useIconShaders);
                            tsmd.applyRecoloring(mat, colors);
                            mat = mpf.Panels[i].go.GetComponent<Renderer>().material;
                            tsmd.apply(mat, useIconShaders);
                            tsmd.applyRecoloring(mat, colors);
                        }
                    }
                }                
            }
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
            applyConfig(part.transform.FindRecursive("model"), getSet(), false, false);            
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
