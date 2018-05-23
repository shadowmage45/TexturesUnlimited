using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KSPShaderTools
{

    /// <summary>
    /// Container class for all data that defines a single 'texture set'.
    /// A texture set is a collection of material definitions and the meshes of a model that they are applied to.
    /// Can include multiple materials, multiple target meshes, and can specify the texture for every slot for the chosen shader.
    /// </summary>
    public class TextureSet
    {
        
        /// <summary>
        /// the registered name of this texture set -- MUST be unique (for global sets), or name collisions will occur.
        /// </summary>
        public readonly String name;

        /// <summary>
        /// the display-title of this texture set, can be non-unique (but for UI purposes should be unique within a given part)
        /// </summary>
        public readonly string title;
                
        /// <summary>
        /// the list of mesh->material assignments; each material data contains a list of meshes/excluded-meshes, along with the shaders and textures to apply to each mesh
        /// </summary>
        public readonly TextureSetMaterialData[] textureData;

        /// <summary>
        /// default mask colors for this texture set
        /// </summary>
        public readonly RecoloringData[] maskColors;

        /// <summary>
        /// Does this texture set support recoloring?
        /// </summary>
        public readonly bool supportsRecoloring = false;

        /// <summary>
        /// What recoloring channels are available for this set? Bitmask.
        /// 1 = main, 2 = secondary, 4 = detail
        /// 3 = main, secondary
        /// 5 = main, detail
        /// 6 = secondary, detail
        /// 7 = main, secondary, detail
        /// </summary>
        public readonly int recolorableChannelMask = 7;

        /// <summary>
        /// What recoloring features are available for this texture set?  Bitmask
        /// 1 = color, 2 = specular, 4 = metallic, 8 = hardness
        /// </summary>
        public readonly int featureMask = 7;

        public TextureSet(ConfigNode node)
        {
            name = node.GetStringValue("name");
            title = node.GetStringValue("title", name);
            ConfigNode[] texNodes = node.GetNodes("MATERIAL");
            int len = texNodes.Length;
            if (len == 0)
            {
                MonoBehaviour.print("Did not find any MATERIAL nodes in texture set:"+name+", searching for legacy styled TEXTURE nodes.");
                MonoBehaviour.print("Please update the config for the texture-set to fix this error.");
                texNodes = node.GetNodes("TEXTURE");
                len = texNodes.Length;
            }
            textureData = new TextureSetMaterialData[len];
            for (int i = 0; i < len; i++)
            {
                textureData[i] = new TextureSetMaterialData(texNodes[i]);
            }
            supportsRecoloring = node.GetBoolValue("recolorable", false);
            recolorableChannelMask = node.GetIntValue("channelMask", 1 | 2 | 4);
            featureMask = node.GetIntValue("featureMask", 1 | 2 | 4);
            if (node.HasNode("COLORS"))
            {
                ConfigNode colorsNode = node.GetNode("COLORS");
                RecoloringData c1 = new RecoloringData(colorsNode.GetStringValue("mainColor"));
                RecoloringData c2 = new RecoloringData(colorsNode.GetStringValue("secondColor"));
                RecoloringData c3 = new RecoloringData(colorsNode.GetStringValue("detailColor"));
                maskColors = new RecoloringData[] { c1, c2, c3 };
            }
            else
            {
                maskColors = new RecoloringData[3];
                Color white = Color.white;
                maskColors[0] = new RecoloringData(white, 0, 0);
                maskColors[1] = new RecoloringData(white, 0, 0);
                maskColors[2] = new RecoloringData(white, 0, 0);
            }
        }

        /// <summary>
        /// Enable this texture set.  Creates a new material for every TextureSetMaterialData, initializes with the config specified properties, including the input custom color data.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="userColors"></param>
        public void enable(Transform root, RecoloringData[] userColors)
        {
            TextureSetMaterialData mtd;
            int len = textureData.Length;
            for (int i = 0; i < len; i++)
            {
                mtd = textureData[i];
                mtd.enable(root);
                mtd.applyRecoloring(root, userColors);
            }
        }

        /// <summary>
        /// Apply the shader properties for the input recoloring data.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="userColors"></param>
        public void applyRecoloring(Transform root, RecoloringData[] userColors)
        {
            TextureSetMaterialData mtd;
            int len = textureData.Length;
            for (int i = 0; i < len; i++)
            {
                mtd = textureData[i];
                mtd.applyRecoloring(root, userColors);
            }
        }
        
        /// <summary>
        /// Public utility method to retrive all of the transforms that a TextureMaterialData would update.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="meshes"></param>
        /// <param name="excludeMeshes"></param>
        /// <returns></returns>
        public static Transform[] findApplicableTransforms(Transform root, string[] meshes, string[] excludeMeshes)
        {
            List<Transform> transforms = new List<Transform>();
            //black-list, do everything not specified in excludeMeshes array
            if (excludeMeshes != null && excludeMeshes.Length > 0)
            {
                Renderer[] rends = root.GetComponentsInChildren<Renderer>();
                int len = rends.Length;
                for (int i = 0; i < len; i++)
                {
                    if (!excludeMeshes.Contains(rends[i].name))
                    {
                        transforms.AddUnique(rends[i].transform);
                    }
                }
            }
            else if (meshes == null || meshes.Length <= 0)//no validation, do them all
            {
                Renderer[] rends = root.GetComponentsInChildren<Renderer>(true);
                int len = rends.Length;
                for (int i = 0; i < len; i++)
                {
                    transforms.AddUnique(rends[i].transform);
                }
            }
            else//white-list, only do what is specified by meshes array
            {
                int len = meshes.Length;
                Transform[] trs;
                Transform tr;
                Renderer r;
                for (int i = 0; i < len; i++)
                {
                    trs = root.FindChildren(meshes[i]);
                    int len2 = trs.Length;
                    for (int k = 0; k < len2; k++)
                    {
                        tr = trs[k];
                        if (tr == null)
                        {
                            continue;
                        }
                        r = tr.GetComponent<Renderer>();
                        if (r == null)
                        {
                            continue;
                        }
                        transforms.AddUnique(tr);
                    }
                }
            }
            return transforms.ToArray();
        }

        /// <summary>
        /// Public utility method to parse texture set instances from config nodes.
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public static TextureSet[] parse(ConfigNode[] nodes)
        {
            int len = nodes.Length;
            TextureSet[] sets = new TextureSet[len];
            for (int i = 0; i < len; i++)
            {
                sets[i] = new TextureSet(nodes[i]);
            }
            return sets;
        }

        /// <summary>
        /// Public utility method to retrieve the registered names for the textures sets from the input collection of nodes.
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public static string[] getTextureSetNames(ConfigNode[] nodes)
        {
            List<string> names = new List<string>();
            string name;
            TextureSet set;
            int len = nodes.Length;
            for (int i = 0; i < len; i++)
            {
                name = nodes[i].GetStringValue("name");
                set = TexturesUnlimitedLoader.getTextureSet(name);
                if (set != null) { names.Add(set.name); }
            }
            return names.ToArray();
        }

        /// <summary>
        /// Public utility method to retrieve just the display titles of the texture sets from the input collection of nodes.
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public static string[] getTextureSetTitles(ConfigNode[] nodes)
        {
            List<string> names = new List<string>();
            string name;
            TextureSet set;
            int len = nodes.Length;
            for (int i = 0; i < len; i++)
            {
                name = nodes[i].GetStringValue("name");
                set = TexturesUnlimitedLoader.getTextureSet(name);
                if (set != null) { names.Add(set.title); }
            }
            return names.ToArray();
        }

        /// <summary>
        /// Applies the input properties to all transforms for a single TextureSetMaterialData.
        /// The input properties can include textures, standard properties, and/or recoloring data.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="meshes"></param>
        /// <param name="excludeMeshes"></param>
        /// <param name="props"></param>
        internal static void updateMaterialProperties(Transform root, string[] meshes, string[] excludeMeshes, ShaderProperty[] props)
        {
            Transform[] trs = findApplicableTransforms(root, meshes, excludeMeshes);
            int len = trs.Length;
            Renderer render;
            for (int i = 0; i < len; i++)
            {
                render = trs[i].GetComponent<Renderer>();
                if (render != null)
                {
                    updateMaterialProperties(render.sharedMaterial, props);
                }
            }
        }

        /// <summary>
        /// Apply the input properties to the single input material.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="props"></param>
        internal static void updateMaterialProperties(Material m, ShaderProperty[] props)
        {
            if (m == null || props == null || props.Length == 0) { return; }
            int len = props.Length;
            for (int i = 0; i < len; i++)
            {
                props[i].apply(m);
            }
        }

        /// <summary>
        /// Return an array containing the properties for only the recoloring data for this texture set.
        /// </summary>
        /// <param name="userColors"></param>
        /// <returns></returns>
        internal static ShaderProperty[] getRecolorProperties(RecoloringData[] userColors, ShaderProperty[] matProps)
        {
            List<ShaderProperty> ps = new List<ShaderProperty>();
            string name;
            if (userColors != null)
            {
                int len = userColors.Length;
                for (int i = 0; i < len; i++)
                {
                    name = "_MaskColor" + (i + 1);
                    if (!Array.Exists(matProps, m => m.name == name))//only add custom coloring if it was not overriden in the MATERIAL block (else keep material block props)
                    {
                        ps.Add(new ShaderPropertyColor(name, userColors[i].getShaderColor()));
                    }
                    else
                    {
                        MonoBehaviour.print("Skipping updating of custom color: " + name + " due to matching existing texture prop");
                    }
                }
                if (!Array.Exists(matProps, m => m.name == "_MaskMetallic"))//only add custom metallic value if it was not overriden in the MATERIAL block (else keep material block props)
                {
                    Color metallicInput = new Color();
                    if (len > 0) { metallicInput.r = userColors[0].metallic; }
                    if (len > 1) { metallicInput.g = userColors[1].metallic; }
                    if (len > 2) { metallicInput.b = userColors[2].metallic; }
                    ps.Add(new ShaderPropertyColor("_MaskMetallic", metallicInput));
                }
                else
                {
                    MonoBehaviour.print("Skipping updating of custom metallic due to matching existing texture prop");
                }
            }
            return ps.ToArray();
        }

        internal static void fillEmptyStockTextureSlots(Material material)
        {
            fillEmptyTextureSlot(material, "_MainTex", "0,0,0,255");
            fillEmptyTextureSlot(material, "_BumpMap", "127, 127, 127, 127");
            fillEmptyTextureSlot(material, "_Emissive", "0,0,0,0");
        }

        internal static void fillEmptyTextureSlot(Material mat, string slot, string textureColor)
        {
            if (mat.HasProperty(slot) && mat.GetTexture(slot) == null)
            {
                if (TexturesUnlimitedLoader.logReplacements)
                {
                    MonoBehaviour.print("Replacing empty textureslot: " + slot + " with color: " + textureColor);
                }
                mat.SetTexture(slot, TexturesUnlimitedLoader.getTextureColor(textureColor));
            }
        }

    }

    /// <summary>
    /// Encapsulates the run-time data for a texture set that is applicable to a single material -- single or multiple meshes that share the same shader, textures, and shader properties.<para/>
    /// A texture set may have multiples of these, and must contain at least one.<para/>
    /// They are created through MATERIAL config nodes in any place the TextureSet definitions are supported.  Each MATERIAL node creates one TextureSetMaterialData.
    /// </summary>
    public class TextureSetMaterialData
    {

        public readonly String shader;
        public readonly String[] meshNames;
        public readonly String[] excludedMeshes;
        public readonly ShaderProperty[] shaderProperties;
        public readonly String[] inheritedTex;
        public readonly String[] inheritedFloat;
        public readonly String[] inheritedColor;
        public readonly string mode;//ghetto enum - 'update' or 'create' are the only valid values

        public TextureSetMaterialData(ConfigNode node)
        {
            shader = node.GetStringValue("shader");
            meshNames = node.GetStringValues("mesh");
            excludedMeshes = node.GetStringValues("excludeMesh");
            shaderProperties = ShaderProperty.parse(node);
            inheritedTex = node.GetStringValues("inheritTexture");
            inheritedFloat = node.GetStringValues("inheritFloat");
            inheritedColor = node.GetStringValues("inheritColor");
            mode = node.GetStringValue("mode", "update");
        }

        /// <summary>
        /// Applies this texture set to the input root transform, using the specified inclusions/exclusions from config.
        /// Does not update any recoloring data.  Must also call 'applyRecoloring' in order to update recoloring properties.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="userColors"></param>
        public void enable(Transform root)
        {
            bool updateMode = string.Equals(mode, "update", StringComparison.CurrentCultureIgnoreCase);
            Transform[] trs = TextureSet.findApplicableTransforms(root, meshNames, excludedMeshes);
            int len = trs.Length;
            Renderer render;
            if (updateMode)
            {
                Material material;
                for (int i = 0; i < len; i++)
                {
                    render = trs[i].GetComponent<Renderer>();
                    if (render != null)
                    {
                        material = render.material;
                        apply(material);
                        render.material = material;
                    }
                }
            }
            else//create mode - creates new materials for renders to use, applying only those properties specified in the texture set, with optional inheritance of properties from the old material
            {
                Material newMaterial = createMaterial();//create a new material for this TSMD
                Material origMaterial = null;
                for (int i = 0; i < len; i++)
                {
                    render = trs[i].GetComponent<Renderer>();
                    if (render != null)
                    {
                        //only inherit properties a single time, from the first render/transform/material found
                        if (origMaterial == null)
                        {
                            origMaterial = render.sharedMaterial;
                            inheritProperties(newMaterial, origMaterial);
                        }
                        render.sharedMaterial = newMaterial;
                    }
                }
            }
        }

        /// <summary>
        /// Sets the shader and properties to the input material
        /// </summary>
        /// <param name="material"></param>
        public void apply(Material material)
        {
            material.shader = TexturesUnlimitedLoader.getShader(shader);
            TextureSet.updateMaterialProperties(material, shaderProperties);
            TextureSet.fillEmptyStockTextureSlots(material);
            material.renderQueue = TexturesUnlimitedLoader.isTransparentMaterial(material) ? TexturesUnlimitedLoader.transparentTextureRenderQueue : TexturesUnlimitedLoader.diffuseTextureRenderQueue;
        }

        /// <summary>
        /// Update the current recoloring data for this texture set.  Does not adjust any other material properties.
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="userColors"></param>
        public void applyRecoloring(Transform root, RecoloringData[] userColors)
        {
            TextureSet.updateMaterialProperties(root, meshNames, excludedMeshes, TextureSet.getRecolorProperties(userColors, shaderProperties));
        }

        public void applyRecoloring(Material mat, RecoloringData[] userColors)
        {
            TextureSet.updateMaterialProperties(mat, TextureSet.getRecolorProperties(userColors, shaderProperties));
        }

        /// <summary>
        /// Return a new material instances instatiated with the shader and properties for this material data.
        /// Does not include applying any recoloring data -- that needs to be handled externally.
        /// </summary>
        /// <returns></returns>
        public Material createMaterial()
        {
            if (string.IsNullOrEmpty(this.shader))
            {
                //TODO -- include texture set name somehow...
                throw new NullReferenceException("ERROR: No shader specified for texture set.");
            }
            Shader shader = TexturesUnlimitedLoader.getShader(this.shader);
            Material material = new Material(shader);
            TextureSet.updateMaterialProperties(material, shaderProperties);
            material.renderQueue = TexturesUnlimitedLoader.isTransparentMaterial(material) ? TexturesUnlimitedLoader.transparentTextureRenderQueue : TexturesUnlimitedLoader.diffuseTextureRenderQueue;
            return material;
        }

        private void inheritProperties(Material newMat, Material origMat)
        {
            if (newMat == null)
            {
                MonoBehaviour.print("ERROR: New material was null when trying to inherit properties.");
                return;
            }
            if (origMat == null)
            {
                MonoBehaviour.print("ERROR: Original material was null when trying to inherit properties.");
                return;
            }
            int len = inheritedTex.Length;
            string propName;
            for (int i = 0; i < len; i++)
            {
                propName = inheritedTex[i];
                Texture tex = origMat.GetTexture(propName);
                if (tex != null)
                {
                    newMat.SetTexture(propName, tex);
                }
                else
                {
                    MonoBehaviour.print("ERROR: Could not inherit texture: " + propName + " from material.  Texture was not found");
                }
            }
            len = inheritedFloat.Length;
            for (int i = 0; i < len; i++)
            {
                propName = inheritedFloat[i];
                float val = origMat.GetFloat(propName);
                newMat.SetFloat(propName, val);
            }
            len = inheritedColor.Length;
            for (int i = 0; i < len; i++)
            {
                propName = inheritedColor[i];
                Color c = origMat.GetColor(propName);
                newMat.SetColor(propName, c);
            }
        }

    }

}
