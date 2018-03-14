using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPShaderTools
{

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class TexturesUnlimitedLoader : MonoBehaviour
    {

        /*  Custom Shader Loading for KSP
         *  Includes loading of platform-specific bundles, or 'universal' bundles.  
         *  Bundles to be loaded are determined by config files (KSP_SHADER_BUNDLE)
         *  Each bundle can have multiple shaders in it.
         *  
         *  Shader / Icon shaders are determined by another config node (KSP_SHADER_DATA)
         *  with a key for shader = <shaderName> and iconShader = <iconShaderName>
         *  
         *  Shaders are applied to models in the database through a third config node (KSP_MODEL_SHADER)
         *  --these specify which database-model-URL to apply a specific texture set to (KSP_TEXTURE_SET)
         *  
         *  Texture sets (KSP_TEXTURE_SET) can be referenced in the texture-switch module for run-time texture switching capability.
         *  
         *  
         *  //eve shader loading data -- need to examine what graphics APIs the SSTU shaders are set to build for -- should be able to build 'universal' bundles
         *  https://github.com/WazWaz/EnvironmentalVisualEnhancements/blob/master/Assets/Editor/BuildABs.cs
         */

        #region REGION - Maps of shaders, texture sets, procedural textures

        /// <summary>
        /// List of loaded shaders and corresponding icon shader.  Loaded from KSP_SHADER_DATA config nodes.
        /// </summary>
        public static Dictionary<string, IconShaderData> loadedShaders = new Dictionary<string, IconShaderData>();

        /// <summary>
        /// List of loaded global texture sets.  Loaded from KSP_TEXTURE_SET config nodes.
        /// </summary>
        public static Dictionary<string, TextureSet> loadedTextureSets = new Dictionary<string, TextureSet>();

        /// <summary>
        /// List of procedurally created 'solid color' textures to use for filling in empty texture slots in materials.
        /// </summary>
        public static Dictionary<string, Texture2D> textureColors = new Dictionary<string, Texture2D>();

        /// <summary>
        /// List of shaders with transparency, and the keywords that enable it.  Used to properly set the render-queue for materials.
        /// </summary>
        public static Dictionary<string, TransparentShaderData> transparentShaderData = new Dictionary<string, TransparentShaderData>();

        #endregion ENDREGION - Maps of shaders, texture sets, procedural textures

        #region REGION - Config Values loaded from disk

        public static bool logReplacements = false;
        public static bool logErrors = false;

        public static int recolorGUIWidth = 400;
        public static int recolorGUISectionHeight = 540;
        public static int recolorGUITotalHeight = 100;

        #endregion ENDREGION - Config Values loaded from disk

        public static TexturesUnlimitedLoader INSTANCE;

        private static List<Action> postLoadCallbacks = new List<Action>();

        private static EventVoid.OnEvent partListLoadedEvent;

        public void Start()
        {
            INSTANCE = this;
            DontDestroyOnLoad(this);
            if (partListLoadedEvent == null)
            {
                partListLoadedEvent = new EventVoid.OnEvent(onPartListLoaded);
                GameEvents.OnPartLoaderLoaded.Add(partListLoadedEvent);
            }
        }

        public void OnDestroy()
        {
            GameEvents.OnPartLoaderLoaded.Remove(partListLoadedEvent);
        }

        public void ModuleManagerPostLoad()
        {
            load();
        }

        private static void load()
        {
            MonoBehaviour.print("TexturesUnlimited - Initializing shader and texture set data.");
            ConfigNode config = GameDatabase.Instance.GetConfigNodes("TEXTURES_UNLIMITED")[0];
            logReplacements = config.GetBoolValue("logReplacements", logReplacements);
            logErrors = config.GetBoolValue("logErrors", logErrors);
            recolorGUIWidth = config.GetIntValue("recolorGUIWidth");
            recolorGUITotalHeight = config.GetIntValue("recolorGUITotalHeight");
            recolorGUISectionHeight = config.GetIntValue("recolorGUISectionHeight");
            Dictionary<string, Shader> dict = new Dictionary<string, Shader>();
            loadBundles(dict);
            buildShaderSets(dict);
            PresetColor.loadColors();
            loadTextureSets();
            applyToModelDatabase();
            MonoBehaviour.print("TexturesUnlimited - Calling PostLoad handlers");
            foreach (Action act in postLoadCallbacks) { act.Invoke(); }
            dumpUVMaps();
        }

        private void onPartListLoaded()
        {
            MonoBehaviour.print("TexturesUnlimited - Updating Part Icon shaders.");
            applyToPartIcons();
        }

        private static void loadBundles(Dictionary<string, Shader> dict)
        {
            ConfigNode[] shaderNodes = GameDatabase.Instance.GetConfigNodes("KSP_SHADER_BUNDLE");
            int len = shaderNodes.Length;
            for (int i = 0; i < len; i++)
            {
                loadBundle(shaderNodes[i], dict);
            }
        }

        private static void loadBundle(ConfigNode node, Dictionary<String, Shader> shaderDict)
        {
            string assetBundleName = "";
            if (node.HasValue("universal")) { assetBundleName = node.GetStringValue("universal"); }
            else if (Application.platform == RuntimePlatform.WindowsPlayer) { assetBundleName = node.GetStringValue("windows"); }
            else if (Application.platform == RuntimePlatform.LinuxPlayer) { assetBundleName = node.GetStringValue("linux"); }
            else if (Application.platform == RuntimePlatform.OSXPlayer) { assetBundleName = node.GetStringValue("osx"); }
            assetBundleName = KSPUtil.ApplicationRootPath + "GameData/" + assetBundleName;

            MonoBehaviour.print("TexturesUnlimited - Loading Shader Pack: " + node.GetStringValue("name") + " :: " + assetBundleName);

            // KSP-PartTools built AssetBunldes are in the Web format, 
            // and must be loaded using a WWW reference; you cannot use the
            // AssetBundle.CreateFromFile/LoadFromFile methods unless you 
            // manually compiled your bundles for stand-alone use
            WWW www = CreateWWW(assetBundleName);

            if (!string.IsNullOrEmpty(www.error))
            {
                MonoBehaviour.print("TexturesUnlimited - Error while loading shader AssetBundle: " + www.error);
                return;
            }
            else if (www.assetBundle == null)
            {
                MonoBehaviour.print("TexturesUnlimited - Could not load AssetBundle from WWW - " + www);
                return;
            }

            AssetBundle bundle = www.assetBundle;

            string[] assetNames = bundle.GetAllAssetNames();
            int len = assetNames.Length;
            Shader shader;
            for (int i = 0; i < len; i++)
            {
                if (assetNames[i].EndsWith(".shader"))
                {
                    shader = bundle.LoadAsset<Shader>(assetNames[i]);
                    MonoBehaviour.print("TexturesUnlimited - Loaded Shader: " + shader.name + " :: " + assetNames[i]+" from pack: "+ node.GetStringValue("name"));
                    if (shader == null || string.IsNullOrEmpty(shader.name))
                    {
                        MonoBehaviour.print("ERROR: Shader did not load properly for asset name: " + assetNames[i]);
                    }
                    else if (shaderDict.ContainsKey(shader.name))
                    {
                        MonoBehaviour.print("ERROR: Duplicate shader detected: " + shader.name);
                    }
                    else
                    {
                        MonoBehaviour.print("Adding shader to shader map: " + shader.name);
                        shaderDict.Add(shader.name, shader);
                    }
                    GameDatabase.Instance.databaseShaders.AddUnique(shader);
                }
            }
            //this unloads the compressed assets inside the bundle, but leaves any instantiated shaders in-place
            bundle.Unload(false);
        }

        public static void addPostLoadCallback(Action func)
        {
            postLoadCallbacks.AddUnique(func);
        }

        public static void removePostLoadCallback(Action func)
        {
            postLoadCallbacks.Remove(func);
        }

        private static void buildShaderSets(Dictionary<string, Shader> dict)
        {
            ConfigNode[] shaderNodes = GameDatabase.Instance.GetConfigNodes("KSP_SHADER_DATA");
            ConfigNode node;
            int len = shaderNodes.Length;
            string sName, iName;
            for (int i = 0; i < len; i++)
            {
                node = shaderNodes[i];
                sName = node.GetStringValue("shader", "KSP/Diffuse");
                iName = node.GetStringValue("iconShader", "KSP/ScreenSpaceMask");
                MonoBehaviour.print("Attempting to load shader icon replacement data for: " + sName + " :: " + iName);
                Shader shader = dict[sName];
                Shader iconShader = dict[iName];
                IconShaderData data = new IconShaderData(shader, iconShader);
                loadedShaders.Add(shader.name, data);
            }
        }

        private static void loadTransparencyData()
        {

        }

        /// <summary>
        /// Asset bundle loader helper method.  Creates a Unity WWW URL reference for the input file-path
        /// </summary>
        /// <param name="bundlePath"></param>
        /// <returns></returns>
        private static WWW CreateWWW(string bundlePath)
        {
            try
            {
                string name = Application.platform == RuntimePlatform.WindowsPlayer ? "file:///" + bundlePath : "file://" + bundlePath;
                return new WWW(Uri.EscapeUriString(name));
            }
            catch (Exception e)
            {
                MonoBehaviour.print("Error while creating AssetBundle request: " + e);
                return null;
            }
        }

        private static void loadTextureSets()
        {
            ConfigNode[] setNodes = GameDatabase.Instance.GetConfigNodes("KSP_TEXTURE_SET");
            TextureSet[] sets = TextureSet.parse(setNodes);
            int len = sets.Length;
            for (int i = 0; i < len; i++)
            {
                loadedTextureSets.Add(sets[i].name, sets[i]);
            }
        }

        /// <summary>
        /// Applies any 'KSP_MODEL_SHADER' definitions to models in the GameDatabase.loadedModels list.
        /// </summary>
        private static void applyToModelDatabase()
        {
            ConfigNode[] modelShaderNodes = GameDatabase.Instance.GetConfigNodes("KSP_MODEL_SHADER");
            TextureSet set = null;
            ConfigNode textureNode;
            string setName;
            int len = modelShaderNodes.Length;
            string[] modelNames;
            GameObject model;
            for (int i = 0; i < len; i++)
            {
                textureNode = modelShaderNodes[i];
                if (textureNode.HasNode("MATERIAL"))
                {
                    set = new TextureSet(textureNode);
                    setName = set.name;
                }
                else if (textureNode.HasValue("textureSet"))
                {
                    setName = textureNode.GetStringValue("textureSet");
                    set = getTextureSet(setName);
                }
                modelNames = textureNode.GetStringValues("model");
                int len2 = modelNames.Length;
                for (int k = 0; k < len2; k++)
                {
                    model = GameDatabase.Instance.GetModelPrefab(modelNames[k]);
                    if (model != null)
                    {
                        if (logReplacements)
                        {
                            MonoBehaviour.print("TexturesUnlimited -- Replacing textures on database model: " + modelNames[k]);
                        }                        
                        set.enable(model.transform, set.maskColors);
                    }
                }
            }
        }

        /// <summary>
        /// Update the part-icons for any parts using shaders found in the part-icon-updating shader map.  Adjusts models specifically based on what shader they are currently using.
        /// </summary>
        private static void applyToPartIcons()
        {
            //brute-force method for fixing part icon shaders
            //  iterate through entire loaded parts list
            //      iterate through every transform with a renderer component
            //          if renderer uses a shader in the shader-data-list
            //              replace shader on icon with the 'icon shader' corresponding to the current shader
            Shader iconShader;
            foreach (AvailablePart p in PartLoader.LoadedPartsList)
            {
                bool outputName = false;//only log the adjustment a single time
                Transform pt = p.partPrefab.gameObject.transform;
                Renderer[] ptrs = pt.GetComponentsInChildren<Renderer>();
                foreach (Renderer ptr in ptrs)
                {
                    string ptsn = ptr.sharedMaterial.shader.name;
                    if (loadedShaders.ContainsKey(ptsn))//is a shader that we care about
                    {
                        iconShader = loadedShaders[ptsn].iconShader;
                        if (!outputName)
                        {
                            MonoBehaviour.print("KSPShaderLoader - Adjusting icon shaders for part: " + p.name + " for original shader:" + ptsn + " replacement: " + iconShader.name);
                            outputName = true;
                        }
                        Transform[] ictrs = p.iconPrefab.gameObject.transform.FindChildren(ptr.name);//find transforms from icon with same name
                        foreach (Transform ictr in ictrs)
                        {
                            Renderer itr = ictr.GetComponent<Renderer>();
                            if (itr != null)
                            {
                                itr.sharedMaterial.shader = iconShader;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Utility method to dump UV maps from every model currently in the model database.
        /// TODO -- has issues/errors on some models/meshes/renderers (might be a skinned-mesh-renderer problem...)
        /// </summary>
        public static void dumpUVMaps()
        {
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("UV_EXPORT");
            if (nodes.Length > 0)
            {
                UVMapExporter exporter = new UVMapExporter();
                ConfigNode node = nodes[0];
                bool export = node.GetBoolValue("exportUVs", false);
                if (!export) { return; }
                string path = node.GetStringValue("exportPath", "exportedUVs");
                exporter.width = node.GetIntValue("width", 1024);
                exporter.height = node.GetIntValue("height", 1024);
                exporter.stroke = node.GetIntValue("thickness", 1);
                foreach (GameObject go in GameDatabase.Instance.databaseModel)
                {
                    exporter.exportModel(go, path);
                }
            }
        }

        /// <summary>
        /// Return a shader by name.  First checks the TU shader dictionary, then checks the GameDatabase.databaseShaders list, and finally falls-back to standard Unity Shader.Find() method.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Shader getShader(string name)
        {
            if (loadedShaders.ContainsKey(name))
            {
                return loadedShaders[name].shader;
            }
            Shader s = GameDatabase.Instance.databaseShaders.Find(m => m.name == name);
            if (s != null)
            {
                return s;
            }
            return Shader.Find(name);
        }

        /// <summary>
        /// Find a global texture set from database with a name that matches the input name.  Returns null if not found.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TextureSet getTextureSet(string name)
        {
            TextureSet s = null;
            if (loadedTextureSets.TryGetValue(name, out s))
            {
                return s;
            }
            MonoBehaviour.print("ERROR: Could not locate texture set for name: " + name);
            return null;
        }

        /// <summary>
        /// Return an array of texture sets for the 'name' values from within the input config node array.  Returns an empty array if none are found.
        /// </summary>
        /// <param name="setNodes"></param>
        /// <returns></returns>
        public static TextureSet[] getTextureSets(ConfigNode[] setNodes)
        {
            int len = setNodes.Length;
            TextureSet[] sets = new TextureSet[len];
            for (int i = 0; i < len; i++)
            {
                sets[i] = getTextureSet(setNodes[i].GetStringValue("name"));
            }
            return sets;
        }

        /// <summary>
        /// Return an array of texture sets for the values from within the input string array.  Returns an empty array if none are found.
        /// </summary>
        /// <param name="setNodes"></param>
        /// <returns></returns>
        public static TextureSet[] getTextureSets(string[] setNames)
        {
            int len = setNames.Length;
            TextureSet[] sets = new TextureSet[len];
            for (int i = 0; i < len; i++)
            {
                sets[i] = getTextureSet(setNames[i]);
            }
            return sets;
        }

        /// <summary>
        /// Input should be a string with R,G,B,A values specified in comma-separated byte notation
        /// </summary>
        /// <param name="stringColor"></param>
        /// <returns></returns>
        public static Texture2D getTextureColor(string stringColor)
        {
            string rgbaString;
            Color c = Utils.parseColorFromBytes(stringColor);
            //just smash the entire thing together to create a unique key for the color
            rgbaString = "" + c.r +":"+ c.g + ":" + c.b + ":" + c.a;
            Texture2D tex = null;
            if (textureColors.TryGetValue(rgbaString, out tex))
            {
                return tex;
            }
            else
            {
                int len = 64 * 64;
                Color[] pixelData = new Color[len];
                for (int i = 0; i < len; i++)
                {
                    pixelData[i] = c;
                }
                tex = new Texture2D(64, 64, TextureFormat.ARGB32, false);
                tex.SetPixels(pixelData);
                tex.Apply(false, true);
                textureColors.Add(rgbaString, tex);
                return tex;
            }
        }

        /// <summary>
        /// Return true/false if the input material uses a shader that supports transparency
        /// AND transparency is currently enabled on the material from keywords (if applicable).
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static bool isTransparentMaterial(Material mat)
        {
            TransparentShaderData tsd = null;
            if (transparentShaderData.TryGetValue(mat.shader.name, out tsd))
            {
                return tsd.isTransparencyEnabled(mat);
            }
            else
            {
                return false;
            }
        }

    }

    public class TransparentShaderData
    {
        public readonly Shader shader;
        public bool alwaysTransparent = false;
        public string[] transparentKeywords;
        public TransparentShaderData(ConfigNode node)
        {
            string shaderName = node.GetStringValue("shader");
            shader = TexturesUnlimitedLoader.getShader(shaderName);
            alwaysTransparent = node.GetBoolValue("alwaysTransparent", false);
            transparentKeywords = node.GetStringValues("keyword");
        }

        public bool isTransparencyEnabled(Material mat)
        {
            if (mat.shader != this.shader) { throw new ArgumentOutOfRangeException("Improper shader.  Expecting: " + shader.name + " was passed: " + mat.shader.name); }
            if (alwaysTransparent) { return true; }
            int len = transparentKeywords.Length;
            for (int i = 0; i < len; i++)
            {
                if (mat.IsKeywordEnabled(transparentKeywords[i]))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Shader to IconShader map <para/>
    /// Used to fix incorrect icon shaders when recoloring shaders are used.
    /// </summary>
    public class IconShaderData
    {
        public readonly Shader shader;
        public readonly Shader iconShader;

        public IconShaderData(Shader shader, Shader iconShader)
        {
            this.shader = shader;
            this.iconShader = iconShader;
        }
    }
    
    /// <summary>
    /// abstract class defining a shader property that can be loaded from a config-node
    /// </summary>
    public abstract class ShaderProperty
    {
        /// <summary>
        /// The name of the shader property that this data class will update.
        /// </summary>
        public readonly string name;

        public static ShaderProperty[] parse(ConfigNode node)
        {
            List<ShaderProperty> props = new List<ShaderProperty>();
            //direct property nodes
            ConfigNode[] propNodes = node.GetNodes("PROPERTY");
            int len = propNodes.Length;
            //this method of property lookup is a bit dirty... but functional
            for (int i = 0; i < len; i++)
            {
                if (propNodes[i].HasValue("texture"))
                {
                    props.Add(new ShaderPropertyTexture(propNodes[i]));
                }
                else if (propNodes[i].HasValue("color"))
                {
                    props.Add(new ShaderPropertyColor(propNodes[i]));
                }
                else if (propNodes[i].HasValue("float"))
                {
                    props.Add(new ShaderPropertyFloat(propNodes[i]));
                }
                else if (propNodes[i].HasValue("keyword"))
                {
                    props.Add(new ShaderPropertyKeyword(propNodes[i]));
                }
                else if (propNodes[i].HasValue("textureColor"))
                {
                    props.Add(new ShaderPropertyTextureColor(propNodes[i]));
                }
            }

            //shorthand property definition loading
            props.AddRange(parseKeywordProperties(node));
            props.AddRange(parseTextureProperties(node));
            props.AddRange(parseColorProperties(node));
            props.AddRange(parseFloatProperties(node));
            props.AddRange(parseTextureColorProperties(node));

            return props.ToArray();
        }

        protected ShaderProperty(ConfigNode node)
        {
            this.name = node.GetStringValue("name");
        }

        protected ShaderProperty(string singleLinePropertyDef)
        {
            this.name = singleLinePropertyDef.Split(',')[0].Trim();
        }

        public void apply(Material mat)
        {
            applyInternal(mat);
        }

        protected abstract void applyInternal(Material mat);

        protected bool checkApply(Material mat)
        {
            if (mat.HasProperty(name))
            {
                return true;
            }
            else
            {
                if (TexturesUnlimitedLoader.logErrors)
                {
                    MonoBehaviour.print("KSPShaderLoader -- Shader: " + mat.shader + " did not have property: " + name);
                }                
            }
            return false;
        }

        private static ShaderPropertyFloat[] parseFloatProperties(ConfigNode node)
        {
            string[] floatProps = node.GetStringValues("float");
            int len = floatProps.Length;
            ShaderPropertyFloat[] props = new ShaderPropertyFloat[len];
            for (int i = 0; i < len; i++)
            {
                props[i] = new ShaderPropertyFloat(floatProps[i]);
            }
            return props;
        }
                
        private static ShaderPropertyKeyword[] parseKeywordProperties(ConfigNode node)
        {
            string[] keywordProps = node.GetStringValues("keyword");
            int len = keywordProps.Length;
            ShaderPropertyKeyword[] props = new ShaderPropertyKeyword[len];
            for (int i = 0; i < len; i++)
            {
                props[i] = new ShaderPropertyKeyword(keywordProps[i]);
            }
            return props;
        }

        private static ShaderPropertyColor[] parseColorProperties(ConfigNode node)
        {
            string[] colorProps = node.GetStringValues("color");
            int len = colorProps.Length;
            ShaderPropertyColor[] props = new ShaderPropertyColor[len];
            for (int i = 0; i < len; i++)
            {
                props[i] = new ShaderPropertyColor(colorProps[i]);
            }
            return props;
        }

        private static ShaderPropertyTexture[] parseTextureProperties(ConfigNode node)
        {
            string[] textureProps = node.GetStringValues("texture");
            int len = textureProps.Length;
            ShaderPropertyTexture[] props = new ShaderPropertyTexture[len];
            for (int i = 0; i < len; i++)
            {
                props[i] = new ShaderPropertyTexture(textureProps[i]);
            }
            return props;
        }

        private static ShaderPropertyTextureColor[] parseTextureColorProperties(ConfigNode node)
        {
            string[] textureColorProps = node.GetStringValues("textureColor");
            int len = textureColorProps.Length;
            ShaderPropertyTextureColor[] props = new ShaderPropertyTextureColor[len];
            for (int i = 0; i < len; i++)
            {
                props[i] = new ShaderPropertyTextureColor(textureColorProps[i]);
            }
            return props;
        }

    }

    /// <summary>
    /// Non-abstract wrapper of a single shader 'Color' property
    /// </summary>
    public class ShaderPropertyColor : ShaderProperty
    {
        public readonly Color color;

        public ShaderPropertyColor(ConfigNode node) : base(node)
        {
            color = node.GetColorFromFloatCSV("color");
        }

        public ShaderPropertyColor(string line) : base(line)
        {
            string[] vals = line.Split(',');
            float r, g, b, a;
            r = Utils.safeParseFloat(vals[1]);
            g = Utils.safeParseFloat(vals[2]);
            b = Utils.safeParseFloat(vals[3]);
            a = vals.Length>=5 ? Utils.safeParseFloat(vals[4]) : 1f;
            this.color = new Color(r, g, b, a);
        }

        public ShaderPropertyColor(string name, Color color) : base(name)
        {
            this.color = color;
        }

        protected override void applyInternal(Material mat)
        {
            mat.SetColor(name, color);
        }
    }

    /// <summary>
    /// Non-abstract wrapper of a single shader 'Float' property
    /// </summary>
    public class ShaderPropertyFloat : ShaderProperty
    {
        public readonly float val;

        public ShaderPropertyFloat(ConfigNode node) : base(node)
        {
            val = node.GetFloatValue("float");
        }

        public ShaderPropertyFloat(string line) : base(line)
        {
            string[] vals = line.Split(',');
            val = Utils.safeParseFloat(vals[1].Trim());
        }

        protected override void applyInternal(Material mat)
        {
            if (checkApply(mat))
            {
                mat.SetFloat(name, val);
            }
        }
    }

    /// <summary>
    /// Non-abstract wrapper of a single shader 'Texture' property
    /// </summary>
    public class ShaderPropertyTexture : ShaderProperty
    {
        public readonly string textureName;
        public readonly bool normal;

        public ShaderPropertyTexture(ConfigNode node) : base(node)
        {
            textureName = node.GetStringValue("texture");
            normal = node.GetBoolValue("normal");
        }

        public ShaderPropertyTexture(string line) : base(line)
        {
            string[] vals = line.Split(',');
            this.textureName = vals[1].Trim();
            this.normal = textureName=="_BumpMap";
        }

        protected override void applyInternal(Material mat)
        {
            if (checkApply(mat))
            {
                Texture2D texture = GameDatabase.Instance.GetTexture(textureName, normal);
                if (texture == null && TexturesUnlimitedLoader.logErrors)
                {
                    MonoBehaviour.print("ERROR: KSPShaderLoader - Texture could not be located for name: " + textureName + " for texture slot: "+name+" while loading textures for material: " + mat);
                }
                mat.SetTexture(name, texture);
            }
        }
    }

    /// <summary>
    /// Non-abstract wrapper of a single shader 'Keyword' property
    /// </summary>
    public class ShaderPropertyKeyword : ShaderProperty
    {
        public string keyword;
        public bool enable;

        public ShaderPropertyKeyword(ConfigNode node) : base(node)
        {
            keyword = node.GetStringValue("keyword");
            enable = node.GetBoolValue("enable", true);
        }

        public ShaderPropertyKeyword(string line) : base(line)
        {
            string[] vals = line.Split(',');
            this.keyword = vals[1].Trim();
            this.enable = Utils.safeParseBool(vals[2].Trim());
        }

        protected override void applyInternal(Material mat)
        {
            if (enable)
            {
                mat.EnableKeyword(keyword);
            }
            else
            {
                mat.DisableKeyword(keyword);
            }
        }
    }

    /// <summary>
    /// Non-abstract wrapper of a single shader 'TextureColor' property
    /// </summary>
    public class ShaderPropertyTextureColor : ShaderProperty
    {
        public string colorString;

        public ShaderPropertyTextureColor(ConfigNode node) : base(node)
        {
            colorString = node.GetStringValue("textureColor");
        }

        public ShaderPropertyTextureColor(string line) : base(line)
        {
            string[] vals = line.Split(',');
            colorString = vals[1];
        }

        protected override void applyInternal(Material mat)
        {
            if (checkApply(mat))
            {
                Texture2D texture = TexturesUnlimitedLoader.getTextureColor(colorString);
                if (texture == null && TexturesUnlimitedLoader.logErrors)
                {
                    MonoBehaviour.print("ERROR: KSPShaderLoader - TextureColor could not be created for string: " + colorString + " for texture slot: " + name + " while loading textures for material: " + mat);
                }
                mat.SetTexture(name, texture);
            }
        }
    }
    
    public struct RecoloringDataPreset
    {
        public string name;
        public string title;
        public Color color;
        public float specular;
        public float metallic;

        public RecoloringDataPreset(ConfigNode node)
        {
            name = node.GetStringValue("name");
            title = node.GetStringValue("title");
            color = Utils.parseColorFromBytes(node.GetStringValue("color"));
            specular = node.GetFloatValue("specular") / 255f;//specified in byte, stored/used as float
            metallic = node.GetFloatValue("metallic") / 255f;//specified in byte, stored/used as float
        }

        public RecoloringData getRecoloringData()
        {
            return new RecoloringData(color, specular, metallic);
        }
    }

    public class PresetColor
    {
        private static List<RecoloringDataPreset> colorList = new List<RecoloringDataPreset>();
        private static Dictionary<String, RecoloringDataPreset> presetColors = new Dictionary<string, RecoloringDataPreset>();
        
        internal static void loadColors()
        {
            colorList.Clear();
            presetColors.Clear();
            ConfigNode[] colorNodes = GameDatabase.Instance.GetConfigNodes("KSP_COLOR_PRESET");
            int len = colorNodes.Length;
            for (int i = 0; i < len; i++)
            {
                RecoloringDataPreset data = new RecoloringDataPreset(colorNodes[i]);
                if (!presetColors.ContainsKey(data.name))
                {
                    presetColors.Add(data.name, data);
                    colorList.Add(data);
                }
            }
        }

        public static RecoloringDataPreset getColor(string name)
        {
            if (!presetColors.ContainsKey(name))
            {
                MonoBehaviour.print("ERROR: No Color data for name: " + name);
            }
            return presetColors[name];
        }

        public static List<RecoloringDataPreset> getColorList() { return colorList; }

    }

}
