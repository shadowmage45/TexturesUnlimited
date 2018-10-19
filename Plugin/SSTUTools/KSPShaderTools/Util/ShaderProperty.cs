using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPShaderTools
{
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
                else if (propNodes[i].HasValue("scale"))
                {
                    props.Add(new ShaderPropertyTextureScale(propNodes[i]));
                }
                else if (propNodes[i].HasValue("offset"))
                {
                    props.Add(new ShaderPropertyTextureOffset(propNodes[i]));
                }
            }

            //shorthand single-line property definition loading
            props.AddRange(parseKeywordProperties(node));
            props.AddRange(parseTextureProperties(node));
            props.AddRange(parseColorProperties(node));
            props.AddRange(parseFloatProperties(node));
            props.AddRange(parseTextureColorProperties(node));
            props.AddRange(parseTextureScaleProperties(node));
            props.AddRange(parseTextureOffsetProperties(node));

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

        private static ShaderPropertyTextureScale[] parseTextureScaleProperties(ConfigNode node)
        {
            string[] textureScaleProps = node.GetStringValues("textureScale");
            int len = textureScaleProps.Length;
            ShaderPropertyTextureScale[] props = new ShaderPropertyTextureScale[len];
            for (int i = 0; i < len; i++)
            {
                props[i] = new ShaderPropertyTextureScale(textureScaleProps[i]);
            }
            return props;
        }

        private static ShaderPropertyTextureOffset[] parseTextureOffsetProperties(ConfigNode node)
        {
            string[] textureOffsetProps = node.GetStringValues("textureOffset");
            int len = textureOffsetProps.Length;
            ShaderPropertyTextureOffset[] props = new ShaderPropertyTextureOffset[len];
            for (int i = 0; i < len; i++)
            {
                props[i] = new ShaderPropertyTextureOffset(textureOffsetProps[i]);
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
            a = vals.Length >= 5 ? Utils.safeParseFloat(vals[4]) : 1f;
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
            this.normal = textureName == "_BumpMap";
        }

        protected override void applyInternal(Material mat)
        {
            if (checkApply(mat))
            {
                Texture2D texture = GameDatabase.Instance.GetTexture(textureName, normal);
                if (texture == null && TexturesUnlimitedLoader.logErrors)
                {
                    MonoBehaviour.print("ERROR: KSPShaderLoader - Texture could not be located for name: " + textureName + " for texture slot: " + name + " while loading textures for material: " + mat);
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
            this.keyword = vals[0].Trim();
            if (vals.Length > 1)
            {
                this.enable = Utils.safeParseBool(vals[1].Trim());
            }
            else
            {
                this.enable = true;
            }            
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

    /// <summary>
    /// Non-abstract wrapper class around a texture-scale shader property.
    /// </summary>
    public class ShaderPropertyTextureScale : ShaderProperty
    {
        private Vector2 scale;

        public ShaderPropertyTextureScale(ConfigNode node) : base(node)
        {
            scale = node.GetVector2("scale");
        }

        public ShaderPropertyTextureScale(string line) : base(line)
        {
            string[] splits = line.Split(',');
            float a = 1;
            float b = 1;
            if (splits.Length >= 3)//name, x, y
            {
                a = Utils.safeParseFloat(splits[1]);
                b = Utils.safeParseFloat(splits[2]);
            }
            scale = new Vector2(a, b);
        }

        protected override void applyInternal(Material mat)
        {
            if (checkApply(mat))
            {
                mat.SetTextureScale(name, scale);
            }
        }
    }

    /// <summary>
    /// Non-abstract wrapper class around a texture-offset shader property.
    /// </summary>
    public class ShaderPropertyTextureOffset : ShaderProperty
    {
        private Vector2 offset;

        public ShaderPropertyTextureOffset(ConfigNode node) : base(node)
        {
            offset = node.GetVector2("offset");
        }

        public ShaderPropertyTextureOffset(string line) : base(line)
        {
            string[] splits = line.Split(',');
            float a = 1;
            float b = 1;
            if (splits.Length >= 3)//name, x, y
            {
                a = Utils.safeParseFloat(splits[1]);
                b = Utils.safeParseFloat(splits[2]);
            }
            offset = new Vector2(a, b);
        }

        protected override void applyInternal(Material mat)
        {
            if (checkApply(mat))
            {
                mat.SetTextureOffset(name, offset);
            }
        }
    }
}
