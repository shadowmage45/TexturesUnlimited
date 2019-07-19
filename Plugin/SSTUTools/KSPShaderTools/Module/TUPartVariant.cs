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
        //config value should match the texture set for the default variant for the part  (TODO -- is this optional or mandatory?)
        [KSPField(isPersistant = true)]
        public string textureSet = string.Empty;

        //alternative to texture-set; used to load 'model-shader' configurations as texture-sets at runtime
        [KSPField(isPersistant = true)]
        public string modelShaderSet = string.Empty;

        [KSPField(isPersistant = true)]
        public string variantName = string.Empty;

        //persistent color data
        [KSPField(isPersistant = true)]
        public string persistentData = string.Empty;

        //if setup to interact with stock fairing module
        [KSPField]
        public bool stockFairing = false;
        
        private RecoloringData[] customColors;

        private bool initialized = false;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            init();
            MonoBehaviour.print("TUPartVariant OnLoad");
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            saveColors(customColors);
            node.SetValue(nameof(persistentData), persistentData, true);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            init();
            MonoBehaviour.print("TUPartVariant OnStart");
        }

        public void Start()
        {
            //if fairing..apply texture-set?
            if (stockFairing)
            {

            }
        }

        public override string GetInfo()
        {
            MonoBehaviour.print("TUPartVariant GetInfo()");
            return base.GetInfo();
        }

        private void init()
        {
            if (initialized) { return; }
            initialized = true;
            loadPersistentData(persistentData);
            if (applied == null)
            {
                //TODO only subscribe based on scene?
                GameEvents.onVariantApplied.Add(applied = new EventData<Part, PartVariant>.OnEvent(variantApplied));
                GameEvents.onEditorVariantApplied.Add(editorApplied = new EventData<Part, PartVariant>.OnEvent(editorVariantApplied));
                GameEvents.onEditorDefaultVariantChanged.Add(editorDefaultApplied = new EventData<AvailablePart, PartVariant>.OnEvent(editorDefaultVariantApplied));
            }

            //application of the initial/default texture set should be handled by onVariantApplied being called when the base variant is applied?
            //.... but apparently is not
            //so find the last used or default set and apply it
            TextureSet set = null;

            //check 'last used set' names
            if (string.IsNullOrEmpty(textureSet) && string.IsNullOrEmpty(modelShaderSet))
            {
                //if both are empty, module is uninitialized; query part for base variant, find extra-info, and find texture set from there
                set = getSet(part.baseVariant);
            }
            else
            {
                //else module previously had a texture set used, restore the texture-set instance from that value
                set = getSet();
            }
            if (set != null)
            {
                //TODO -- will icon shaders ever be needed here?
                applyConfig(part.transform.FindRecursive("model"), set, false, false);
            }
            else
            {
                MonoBehaviour.print("ERROR: TUPartVariant could not locate default or stored texture set data");
            }
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
            if (part != this.part) { return; }
            bool resetColors = variant.Name!=variantName;
            variantName = variant.Name;            
            MonoBehaviour.print("Variant applied: " + variant.Name);
            TextureSet set = getSet(variant);
            if (set != null)
            {
                applyConfig(part.transform.FindRecursive("model"), set, resetColors);
            }
            else
            {
                MonoBehaviour.print("ERROR: Set was null for variant: " + variant.Name);
            }
        }

        private void editorVariantApplied(Part part, PartVariant variant)
        {
            if (part != this.part) { return; }
            bool resetColors = variant.Name != variantName;
            variantName = variant.Name;
            MonoBehaviour.print("EditorVariant applied: " + variant.Name);
            TextureSet set = getSet(variant);
            if (set != null)
            {
                applyConfig(part.transform.FindRecursive("model"), set, resetColors);
            }
            else
            {
                MonoBehaviour.print("ERROR: Set was null for variant: " + variant.Name);
            }
        }

        private void editorDefaultVariantApplied(AvailablePart part, PartVariant variant)
        {
            //TODO -- how to tell if it was -this- part? -- or is this to apply to the icon when the icon-switch is toggled?
            //MonoBehaviour.print("EditorDefaultVariant applied: " + variant.Name);
            //TextureSet set = getSet(variant);
            //if (set != null)
            //{
            //    applyConfig(part.partPrefab.transform.FindRecursive("model"), set, true);
            //    applyConfig(part.iconPrefab.transform.FindRecursive("model"), set, true, true);
            //    variant.Materials.Clear();//don't let variants manage materials, at all
            //}
            //else
            //{
            //    textureSet = string.Empty;
            //    modelShaderSet = string.Empty;
            //    MonoBehaviour.print("ERROR: Set was null for variant: " + variant.Name);
            //}
        }

        private TextureSet getSet(PartVariant variant)
        {
            string setName = variant.GetExtraInfoValue("textureSet");
            MonoBehaviour.print("Found texture set name of: " + setName);
            TextureSet set = null;
            if (!string.IsNullOrEmpty(setName))
            {
                set = TexturesUnlimitedLoader.getTextureSet(setName);
                textureSet = setName;
                modelShaderSet = string.Empty;
                return set;
            }
            setName = variant.GetExtraInfoValue("modelShader");
            if (!string.IsNullOrEmpty(setName))
            {
                set = TexturesUnlimitedLoader.getModelShaderTextureSet(setName);
                modelShaderSet = setName;
                textureSet = string.Empty;
                return set;
            }
            //if nothing found, clear out references
            if (TexturesUnlimitedLoader.logErrors || TexturesUnlimitedLoader.logAll)
            {
                MonoBehaviour.print("Could not load texture set for part variant: " + variant?.Name + " for part: " + part.name);
            }
            modelShaderSet = textureSet = string.Empty;
            return null;
        }

        private TextureSet getSet()
        {
            TextureSet set = null;
            if (!string.IsNullOrEmpty(textureSet) && (set = TexturesUnlimitedLoader.getTextureSet(textureSet)) != null)
            {
                modelShaderSet = string.Empty;
                return set;
            }
            else if (!string.IsNullOrEmpty(modelShaderSet) && (set = TexturesUnlimitedLoader.getModelShaderTextureSet(modelShaderSet)) != null)
            {
                textureSet = string.Empty;
                return set;
            }
            else if ((set=getSet(part.baseVariant))!=null)
            {
                return set;
            }
            //if nothing found, clear out references
            modelShaderSet = textureSet = string.Empty;
            return null;
        }

        private void applyConfig(Transform root, TextureSet set, bool useSetColors, bool useIconShaders = false)
        {
            if (set == null) { return; }
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
            return string.IsNullOrEmpty(textureSet)? TexturesUnlimitedLoader.getModelShaderTextureSet(modelShaderSet) : TexturesUnlimitedLoader.getTextureSet(textureSet);
        }

        public void setSectionColors(string name, RecoloringData[] colors)
        {
            MonoBehaviour.print("Set section colors: " + name);
            this.actionWithSymmetry(m =>
            {
                m.customColors = colors;
                m.saveColors(m.customColors);
                m.applyConfig(m.part.transform.FindRecursive("model"), m.getSet(), false, false);
            });
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
            MonoBehaviour.print("Saving custom color data: " + persistentData);
        }
        
    }
}
