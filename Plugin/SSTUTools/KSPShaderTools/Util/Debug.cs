using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPShaderTools
{
    public static class Debug
    {

        public static string getMaterialPropertiesDebug(Material mat, int indent = 0)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(' ', indent + 3);
            builder.AppendLine("Material Name: " + mat.name);
            Shader s = mat.shader;
            if (s != null)
            {
                builder.Append(' ', indent + 7);
                builder.AppendLine("shader  : " + mat.shader.name);
                builder.Append(' ', indent + 7);
                builder.AppendLine("renderQ : " + mat.renderQueue);
                getPropertyTexture(mat, "_MainTex", builder, indent);
                getPropertyTexture(mat, "_BumpMap", builder, indent);
                getPropertyTexture(mat, "_MetallicGlossMap", builder, indent);
                getPropertyTexture(mat, "_SpecMap", builder, indent);
                getPropertyTexture(mat, "_SpecGlossMap", builder, indent);
                getPropertyTexture(mat, "_Emissive", builder, indent);
                getPropertyTexture(mat, "_AOMap", builder, indent);
                getPropertyTexture(mat, "_MaskTex", builder, indent);
                getPropertyTexture(mat, "_Thickness", builder, indent);
                getPropertyFloat(mat, "_Smoothness", builder, indent);
                getPropertyFloat(mat, "_Shininess", builder, indent);
                getPropertyFloat(mat, "_Metal", builder, indent);
                getPropertyVector(mat, "_Color", builder, indent);
                getPropertyVector(mat, "_EmissiveColor", builder, indent);
                getPropertyVector(mat, "_MaskColor1", builder, indent);
                getPropertyVector(mat, "_MaskColor2", builder, indent);
                getPropertyVector(mat, "_MaskColor3", builder, indent);
                getPropertyVector(mat, "_MaskMetallic", builder, indent);
                getPropertyVector(mat, "_DiffuseNorm", builder, indent);
                getPropertyVector(mat, "_SmoothnessNorm", builder, indent);
                getPropertyVector(mat, "_MetalNorm", builder, indent);
                getPropertyVector(mat, "_SpecularNorm", builder, indent);
                foreach (string keyword in mat.shaderKeywords)
                {
                    getPropertyKeyword(mat, keyword, builder, indent);
                }
            }
            return builder.ToString();
        }

        private static void getPropertyTexture(Material m, string prop, StringBuilder builder, int indent)
        {
            if (m.HasProperty(prop))
            {
                builder.Append(' ', indent + 7);
                builder.AppendLine(prop + " : " + m.GetTexture(prop));
                builder.Append(' ', indent + 7);
                builder.AppendLine(prop + "Offset" + " : " + m.GetTextureOffset(prop));
            }
        }

        private static void getPropertyFloat(Material m, string prop, StringBuilder builder, int indent)
        {
            if (m.HasProperty(prop))
            {
                builder.Append(' ', indent + 7);
                builder.AppendLine(prop + " : " + m.GetFloat(prop));
            }
        }

        private static void getPropertyVector(Material m, string prop, StringBuilder builder, int indent)
        {
            if (m.HasProperty(prop))
            {
                builder.Append(' ', indent + 7);
                builder.AppendLine(prop + " : " + m.GetVector(prop));
            }
        }

        private static void getPropertyKeyword(Material m, string prop, StringBuilder builder, int indent)
        {
            if (m.shaderKeywords.Contains(prop))
            {
                builder.Append(' ', indent + 7);
                builder.AppendLine(prop + " : " + m.IsKeywordEnabled(prop));
            }
        }

    }
}
