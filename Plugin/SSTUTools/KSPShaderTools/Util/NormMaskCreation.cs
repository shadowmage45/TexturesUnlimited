using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPShaderTools.Util
{

    public class NormMaskCreation
    {

        public static void processBatch()
        {
            long totalProcessTime = 0;
            List<string> configExport = new List<string>();
            foreach (TextureSet set in TexturesUnlimitedLoader.loadedTextureSets.Values)
            {
                if (set.supportsRecoloring)
                {
                    MonoBehaviour.print("Processing texture mask generation for texture set: " + set.name);
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    try
                    {
                        int generatedTextures = 0;
                        int setIndex = 0;
                        foreach (TextureSetMaterialData mat in set.textureData)
                        {
                            float[] diff = new float[3];
                            float[] met = new float[3];
                            float[] smth = new float[3];
                            MaskData data;
                            ShaderPropertyTexture maskProp = Array.Find(mat.shaderProperties, m => m.name == "_MaskTex") as ShaderPropertyTexture;
                            if (maskProp == null) { continue; }//no mask property;
                            Texture2D maskTex = GameDatabase.Instance.GetTexture(maskProp.textureName, false);
                            if (maskTex == null) { continue; }//No mask texture
                            ShaderPropertyTexture mainProp = Array.Find(mat.shaderProperties, m => m.name == "_MainTex") as ShaderPropertyTexture;
                            ShaderPropertyKeyword stockSpecKeyProp = Array.Find(mat.shaderProperties, m => m.name == "TU_STOCK_SPEC") as ShaderPropertyKeyword;
                            if (mainProp != null)
                            {
                                Texture2D diffTex = GameDatabase.Instance.GetTexture(mainProp.textureName, false);
                                if (diffTex != null)
                                {
                                    data = processTextures(diffTex, maskTex, set.name + "-" + setIndex + "-diffuse-normalization.png", true, false, false, false, false);
                                    diff = new float[] { (float)data.r, (float)data.g, (float)data.b};
                                    generatedTextures++;
                                    if (stockSpecKeyProp != null)
                                    {
                                        data = processTextures(diffTex, maskTex, set.name + "-" + setIndex + "-smooth-normalization.png", false, false, false, false, true);
                                        smth = new float[] { (float)data.r, (float)data.g, (float)data.b };
                                        generatedTextures++;
                                    }
                                }
                            }
                            ShaderPropertyTexture metalProp = Array.Find(mat.shaderProperties, m => m.name == "_MetallicGlossMap") as ShaderPropertyTexture;
                            if (metalProp != null)
                            {
                                Texture2D metalTex = GameDatabase.Instance.GetTexture(metalProp.textureName, false);
                                if (metalTex != null)
                                {
                                    data = processTextures(metalTex, maskTex, set.name + "-" + setIndex + "-metal-normalization.png", false, true, false, false, false);
                                    met = new float[] { (float)data.r, (float)data.g, (float)data.b };
                                    generatedTextures++;
                                }
                            }
                            ShaderPropertyTexture specPropLegacy = Array.Find(mat.shaderProperties, m => m.name == "_SpecMap") as ShaderPropertyTexture;
                            //metal from spec.a, smooth from spec.rgb luminance
                            if ((mat.shader == "SSTU/PBR/Masked" || mat.shader == "SSTU/Masked") && specPropLegacy != null )
                            {
                                Texture2D specTex = GameDatabase.Instance.GetTexture(specPropLegacy.textureName, false);
                                if (specTex != null)
                                {
                                    data = processTextures(specTex, maskTex, set.name + "-" + setIndex + "-metal-normalization.png", false, false, false, false, true);
                                    met = new float[] { (float)data.r, (float)data.g, (float)data.b };
                                    generatedTextures++;
                                    data = processTextures(specTex, maskTex, set.name + "-" + setIndex + "-smooth-normalization.png", true, false, false, false, false);
                                    smth = new float[] { (float)data.r, (float)data.g, (float)data.b };
                                    generatedTextures++;
                                    convertTextures(specTex, specPropLegacy.textureName + ".png");
                                }
                            }

                            //MonoBehaviour.print("CONFIG EXPORT START---------------");
                            string patch = 
                                         "\n@KSP_TEXTURE_SET[" + set.name + "]" +
                                         "\n{" +
                                         "\n    @MATERIAL," + setIndex +
                                         "\n    {" +
                                         "\n        vector = _DiffuseNorm," + diff[0] + "," + diff[1] + "," + diff[2]+
                                         "\n        vector = _SmoothnessNorm," + smth[0] + "," + smth[1] + "," + smth[2] +
                                         "\n        vector = _MetalNorm," + met[0] + "," + met[1] + "," + met[2] +
                                         "\n    }" +
                                         "\n}";
                            MonoBehaviour.print(patch);
                            //MonoBehaviour.print("CONFIG EXPORT END------------------");
                            configExport.Add(patch);
                            setIndex++;
                        }
                        sw.Stop();
                        totalProcessTime += sw.ElapsedMilliseconds;

                        MonoBehaviour.print("elapsed: " + sw.ElapsedMilliseconds+" generated: "+generatedTextures+" masks for set");
                    }
                    catch (Exception e)
                    {
                        MonoBehaviour.print(e);
                    }
                }
            }
            string file = "NormalizationDataExport.txt";
            string path = System.IO.Path.GetFullPath(file);
            MonoBehaviour.print("Exporting configs to: " + path);
            System.IO.File.WriteAllLines(file, configExport.ToArray());
            MonoBehaviour.print("Total process time: " + totalProcessTime);
        }

        private static void convertTextures(Texture2D specMetalMap, string exportFileName)
        {
            if(File.Exists("convertExport/" + exportFileName)) { return; }
            //because KSP loads textures as GPU only (no CPU side reads), blit the inputs to new Texture2Ds that can be read.
            //its a round-about process, but is probably the fastest way to make it work.
            //could also read the existing files from disk, again, and load them into Texture2D somehow, but I'm not sure
            //how it handles different file formats/etc.
            RenderTexture diffRendTex = new RenderTexture(specMetalMap.width, specMetalMap.height, 24, RenderTextureFormat.ARGB32);
            Graphics.Blit(specMetalMap, diffRendTex);
            Graphics.SetRenderTarget(diffRendTex);
            specMetalMap = new Texture2D(specMetalMap.width, specMetalMap.height, TextureFormat.ARGB32, false);
            specMetalMap.ReadPixels(new Rect(0, 0, specMetalMap.width, specMetalMap.height), 0, 0);
            specMetalMap.Apply();
            int w = specMetalMap.width;
            int h = specMetalMap.height;
            float a, b;
            Color c;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    c = specMetalMap.GetPixel(x, y);
                    a = c.r;
                    b = c.a;
                    c.a = a;
                    c.r = b;
                    c.g = b;
                    c.b = b;
                    specMetalMap.SetPixel(x, y, c);
                }
            }
            Directory.CreateDirectory("convertExport/");
            string exportMinusFileName = exportFileName.Substring(0, exportFileName.LastIndexOf('/'));
            Directory.CreateDirectory("convertExport/" + exportMinusFileName);
            byte[] bytes = specMetalMap.EncodeToPNG();
            File.WriteAllBytes("convertExport/" + exportFileName, bytes);
            Graphics.SetRenderTarget(null);
            GameObject.DestroyImmediate(diffRendTex);
            GameObject.DestroyImmediate(specMetalMap);
        }

        private static MaskData processTextures(Texture2D diffuse, Texture2D mask, string exportFileName, bool lum, bool red, bool green, bool blue, bool alpha)
        {
            try
            {
                //skip if already exists
                //if (File.Exists("maskExport/" + exportFileName))
                //{
                //    MonoBehaviour.print("Skipping export due to file already existing.");
                //    return null;
                //}

                //because KSP loads textures as GPU only (no CPU side reads), blit the inputs to new Texture2Ds that can be read.
                //its a round-about process, but is probably the fastest way to make it work.
                //could also read the existing files from disk, again, and load them into Texture2D somehow, but I'm not sure
                //how it handles different file formats/etc.
                RenderTexture diffRendTex = new RenderTexture(diffuse.width, diffuse.height, 24, RenderTextureFormat.ARGB32);
                Graphics.Blit(diffuse, diffRendTex);
                Graphics.SetRenderTarget(diffRendTex);
                diffuse = new Texture2D(diffuse.width, diffuse.height, TextureFormat.ARGB32, false);
                diffuse.ReadPixels(new Rect(0, 0, diffuse.width, diffuse.height), 0, 0);
                diffuse.Apply();

                RenderTexture maskRendTex = new RenderTexture(mask.width, mask.height, 24, RenderTextureFormat.ARGB32);
                Graphics.Blit(mask, maskRendTex);
                Graphics.SetRenderTarget(maskRendTex);
                mask = new Texture2D(mask.width, mask.height, TextureFormat.ARGB32, false);
                mask.ReadPixels(new Rect(0, 0, mask.width, mask.height), 0, 0);
                mask.Apply();
                Graphics.SetRenderTarget(null);

                MaskData mData = new MaskData(mask);
                mData.processMaskCount(diffuse, lum, red, green, blue, alpha);
                Texture2D output = new Texture2D(diffuse.width, diffuse.height, TextureFormat.RGBA32, false);
                mData.writeOutputs(output);
                Directory.CreateDirectory("maskExport/");
                byte[] bytes = output.EncodeToPNG();
                File.WriteAllBytes("maskExport/" + exportFileName, bytes);
                GameObject.DestroyImmediate(output);
                GameObject.DestroyImmediate(diffuse);
                GameObject.DestroyImmediate(mask);
                GameObject.DestroyImmediate(diffRendTex);
                GameObject.DestroyImmediate(maskRendTex);
                return mData;
            }
            catch (Exception e)
            {
                MonoBehaviour.print("EXCEPTION DURING MASK CREATION\n" + e);
            }
            return null;
        }

        private class MaskData
        {

            public double aggregateValueR;
            public double aggregateValueG;
            public double aggregateValueB;

            public double sampleCountR;
            public double sampleCountG;
            public double sampleCountB;

            public double r;
            public double g;
            public double b;
            
            float minR = 1;
            float maxR = 0;
            float minG = 1;
            float maxG = 0;
            float minB = 1;
            float maxB = 0;

            Texture2D mask;            

            public MaskData(Texture2D mask)
            {
                this.mask = mask;
            }

            public void processMaskCount(Texture2D inputTexture, bool useLum, bool useRed, bool useGreen, bool useBlue, bool useAlpha)
            {
                int width = inputTexture.width;
                int height = inputTexture.height;
                Color maskColor;
                Color imageColor;
                float luminosity;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        maskColor = mask.GetPixel(x, y);
                        if (maskColor.r > 0 || maskColor.g > 0 || maskColor.b > 0)
                        {
                            imageColor = inputTexture.GetPixel(x, y);
                            //TODO is grayscale correct?
                            if (useLum) { luminosity = imageColor.grayscale; }
                            else if (useRed) { luminosity = imageColor.r; }
                            else if (useGreen) { luminosity = imageColor.g; }
                            else if (useBlue) { luminosity = imageColor.b; }
                            else if (useAlpha) { luminosity = imageColor.a; }
                            else { luminosity = 0; }//should be noop/invalid input
                            if (maskColor.r > 0)
                            {
                                sampleCountR++;
                                minR = Math.Min(minR, luminosity);
                                maxR = Math.Max(maxR, luminosity);
                                aggregateValueR += luminosity;
                            }
                            if (maskColor.g > 0)
                            {
                                sampleCountG++;
                                minG = Math.Min(minR, luminosity);
                                maxG = Math.Max(maxR, luminosity);
                                aggregateValueG += luminosity;
                            }
                            if (maskColor.b > 0)
                            {
                                sampleCountB++;
                                minB = Math.Min(minR, luminosity);
                                maxB = Math.Max(maxR, luminosity);
                                aggregateValueB += luminosity;
                            }
                        }
                    }
                }
            }

            public void writeOutputs(Texture2D map)
            {
                Color maskColor;
                int width = map.width;
                int height = map.height;
                double valR = r = sampleCountR == 0? 0 : aggregateValueR / sampleCountR;
                double valG = g = sampleCountG == 0? 0 : aggregateValueG / sampleCountG;
                double valB = b = sampleCountB == 0? 0 : aggregateValueB / sampleCountB;
                //really, these values are all that are needed for the normalization values
                //the rest of the math is already in the shader in the form of the mask-based recolor values
                //MonoBehaviour.print("export vals-double: " + valR + "," + valG + "," + valB);                
                //MonoBehaviour.print("export vals-byte: " + (byte)(valR*255) + "," + (byte)(valG*255) + "," + (byte)(valB*255));
                double maskR, maskG, maskB;
                double maskSum;
                double colorValR;
                double colorValG;
                double colorValB;
                float outColor;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        maskColor = mask.GetPixel(x, y);
                        maskR = maskColor.r;
                        maskG = maskColor.g;
                        maskB = maskColor.b;
                        maskSum = maskR + maskG + maskB;
                        if (maskSum > 0)
                        {
                            colorValR = 0;
                            colorValG = 0;
                            colorValB = 0;
                            //normalize the mask value
                            double length = Math.Sqrt(maskR * maskR + maskG * maskG + maskB * maskB);
                            if (length > 1)
                            {
                                maskR = maskR / length;
                                maskG = maskG / length;
                                maskB = maskB / length;
                            }
                            colorValR = maskR * valR;
                            colorValG = maskG * valG;
                            colorValB = maskB * valB;
                            outColor = (float)(colorValR + colorValG + colorValB);
                            map.SetPixel(x, y, new Color(outColor, outColor, outColor, 1));// Color.FromArgb(255, outColor, outColor, outColor));
                        }
                        else
                        {
                            //TODO read the existing texture, only write to it once; or do a pre-fill of the texture to black
                            map.SetPixel(x, y, new Color(0, 0, 0, 1));// Color.FromArgb(255, 0, 0, 0));
                        }
                    }
                }
            }

        }

    }

}
