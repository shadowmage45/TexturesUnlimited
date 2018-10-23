using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using System.Collections;
using System.IO;

namespace SSTUTools
{
    [DatabaseLoaderAttrib((new string[] { "kbm" }))]
    public class SMFBundleDefinitionReader : DatabaseLoader<GameObject>
    {
        public override IEnumerator Load(UrlDir.UrlFile urlFile, FileInfo file)
        {
            // KSP-PartTools built AssetBunldes are in the Web format, 
            // and must be loaded using a WWW reference; you cannot use the 
            // AssetBundle.CreateFromFile/LoadFromFile methods unless you 
            // manually compiled your bundles for stand-alone use
            string path = urlFile.fullPath.Replace('\\', '/');
            WWW www = CreateWWW(path);

            //not sure why the yield statement here, have not investigated removing it.
            yield return www;

            if (!string.IsNullOrEmpty(www.error))
            {
                MonoBehaviour.print("Error while loading AssetBundle model: " + www.error+" for url: "+urlFile.url+" :: "+path);
                yield break;
            }
            else if (www.assetBundle == null)
            {
                MonoBehaviour.print("Could not load AssetBundle from WWW - " + www);
                yield break;
            }

            AssetBundle bundle = www.assetBundle;

            //TODO clean up linq
            string modelName = bundle.GetAllAssetNames().FirstOrDefault(assetName => assetName.EndsWith("prefab"));
            AssetBundleRequest abr = bundle.LoadAssetAsync<GameObject>(modelName);
            while (!abr.isDone) { yield return abr; }//continue to yield until the asset load has returned from the loading thread
            if (abr.asset == null)//if abr.isDone==true and asset is null, there was a major error somewhere, likely file-system related
            {
                MonoBehaviour.print("ERROR: Failed to load model from asset bundle!");
                yield break;
            }
            GameObject model = GameObject.Instantiate((GameObject)abr.asset);//make a copy of the asset
            //modelDebugOutput(model);
            setupModelTextures(urlFile.root, model);
            this.obj = model;
            this.successful = true;
            //this unloads the compressed assets inside the bundle, but leaves any instantiated models in-place
            bundle.Unload(false);
        }

        /// <summary>
        /// Creates a WWW URL reference for the input file-path
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

        private static void setupModelTextures(UrlDir dir, GameObject model)
        {
            Renderer[] renders = model.GetComponentsInChildren<Renderer>(true);
            Material m;
            List<Material> adjustedMaterials = new List<Material>();
            foreach (Renderer render in renders)
            {
                m = render.sharedMaterial;
                if (adjustedMaterials.Contains(m)) { continue; }//already fixed that material (many are shared across transforms), so skip it
                else { adjustedMaterials.Add(m); }
                replaceShader(m, m.shader.name);
                replaceTexture(m, "_MainTex", false);
                replaceTexture(m, "_SpecMap", false);
                replaceTexture(m, "_MetallicGlossMap", false);
                replaceTexture(m, "_BumpMap", true);
                replaceTexture(m, "_Emissive", false);
                replaceTexture(m, "_AOMap", false);
            }
        }

        /// <summary>
        /// Returns -ALL- children/grand-children/etc transforms of the input; everything in the heirarchy.
        /// </summary>
        /// <param name="transform"></param>
        /// <returns></returns>
        private static Transform[] GetAllChildren(Transform transform)
        {
            List<Transform> trs = new List<Transform>();
            recurseAddChildren(transform, trs);
            return trs.ToArray();
        }

        private static void recurseAddChildren(Transform transform, List<Transform> trs)
        {
            int len = transform.childCount;
            foreach (Transform child in transform)
            {
                trs.Add(child);
                recurseAddChildren(child, trs);
            }
        }

        private static void replaceShader(Material m, string name)
        {
            m.shader = Shader.Find(name);
        }

        private static void replaceTexture(Material m, string name, bool nrm = false)
        {
            if (m == null)
            {
                MonoBehaviour.print("Material was null for model...");
                return;
            }
            Texture tex = m.GetTexture(name);
            if (tex == null)
            {
                //MonoBehaviour.print("Model had null texture for shader property: " + name);
                return;
            }
            MonoBehaviour.print("Attempting to replace SMF model texture: " +  tex.name);
            if (string.IsNullOrEmpty(tex.name))
            {
                MonoBehaviour.print("Model texture name was null.");
                return;
            }
            Texture newTex = findTexture(tex.name, nrm);
            MonoBehaviour.print("Found replacement texture of: " + newTex);
            if (newTex != null)
            {
                m.SetTexture(name, null);//clear existing texture reference; unity does silly caching stuff
                m.SetTexture(name, newTex);
            }
        }

        private static Texture2D findTexture(string name, bool nrm = false)
        {
            foreach (GameDatabase.TextureInfo t in GameDatabase.Instance.databaseTexture)
            {
                if (t.file.url.EndsWith(name))
                {
                    if (nrm) { return t.normalMap; }
                    return t.texture;
                }
            }
            return null;
        }
        
    }

}
