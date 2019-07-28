using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using System.IO;

namespace KSPShaderTools
{

    //disabled entirely -- use stock reflection system
    //[KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class ReflectionManager : MonoBehaviour
    {

        #region CONSTANTS

        public const int galaxyMask = 1 << 18;
        public const int atmosphereMask = 1 << 9;
        public const int scaledSpaceMask = 1 << 10;
        public const int sceneryMask = (1 << 4) | (1 << 15) | (1<<17) | (1<<23);
        public const int fullSceneMask = ~0;

        #endregion

        #region CONFIG FIELDS

        /// <summary>
        /// Number of frames inbetween reflection map updates.
        /// </summary>
        public int mapUpdateSpacing = 60;

        /// <summary>
        /// Size of the rendered reflection map.  Higher resolutions result in higher fidelity reflections, but at a much higher run-time cost.
        /// Must be a power-of-two size; e.g. 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048.
        /// </summary>
        public int envMapSize = 256;

        /// <summary>
        /// Layer to use for skybox hack
        /// </summary>
        public int skyboxLayer = 26;

        public int sphereLayer = 27;

        #endregion

        #region DEBUG FIELDS

        //set through the reflection debug GUI

        public bool renderGalaxy = true;
        public bool renderScaled = true;
        public bool renderAtmo = true;
        public bool renderScenery = true;

        public bool reflectionsEnabled = true;

        #endregion

        #region INTERNAL FIELDS

        public ReflectionProbeData probeData;
        public ReflectionSkyboxProxy proxyData;
        public GameObject cameraObject;
        public Camera reflectionCamera;
        private static Shader skyboxShader;
        private bool renderedEditor = false;
        private int editorDelay = 0;
        private int editorTarget = 2;

        //Mod interop stuff

        public bool eveInstalled = true;
        public CameraAlphaFix eveCameraFix;

        //internal debug fields
        private bool export = false;
        private bool debug = false;

        private static ApplicationLauncherButton debugAppButton;
        private ReflectionDebugGUI gui;
        private GameObject debugSphere;

        //debug/prototype stuff

        private static ReflectionManager instance;

        public static ReflectionManager Instance
        {
            get
            {
                return instance;
            }
        }

        #endregion

        #region LIFECYCLE METHODS

        public void Awake()
        {
            MonoBehaviour.print("ReflectionManager Awake()");
            instance = this;

            debug = TexturesUnlimitedLoader.configurationNode.GetBoolValue("debug", false);

            ConfigNode node = TexturesUnlimitedLoader.configurationNode.GetNode("REFLECTION_CONFIG");
            MonoBehaviour.print("TU-Reflection Manager - Loading reflection configuration: \n" + node.ToString());
            MonoBehaviour.print("TU-Reflection Manager - Alternate Render Enabled (DX9/DX11 Fix): " + TexturesUnlimitedLoader.alternateRender);
            reflectionsEnabled = node.GetBoolValue("enabled", false);
            envMapSize = node.GetIntValue("resolution", envMapSize);
            mapUpdateSpacing = node.GetIntValue("interval", mapUpdateSpacing);
            eveInstalled = node.GetBoolValue("eveInstalled", false);
            export = node.GetBoolValue("exportDebugCubes", false);

            init();

            Texture2D tex;
            if (debugAppButton == null && debug)//static reference; track if the button was EVER created, as KSP keeps them even if the addon is destroyed
            {                
                //create a new button
                tex = GameDatabase.Instance.GetTexture("Squad/PartList/SimpleIcons/RDIcon_fuelSystems-highPerformance", false);
                debugAppButton = ApplicationLauncher.Instance.AddModApplication(debugGuiEnable, debugGuiDisable, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB, tex);
            }
            else if(debugAppButton != null)
            {
                //reseat callback refs to the ones from THIS instance of the KSPAddon (old refs were stale, pointing to methods for a deleted class instance)
                debugAppButton.onTrue = debugGuiEnable;
                debugAppButton.onFalse = debugGuiDisable;
            }
        }

        /// <summary>
        /// Unity per-frame update method.  Should update any reflection maps that need updating.
        /// </summary>
        public void Update()
        {
            if (!reflectionsEnabled) { return; }
            updateReflections();
            if (debugSphere != null)
            {
                if (FlightIntegrator.ActiveVesselFI != null)
                {
                    debugSphere.transform.position = FlightIntegrator.ActiveVesselFI.transform.position;
                }
            }

            //TODO convolution on cubemap
            //https://seblagarde.wordpress.com/2012/06/10/amd-cubemapgen-for-physically-based-rendering/
            //http://codeflow.org/entries/2011/apr/18/advanced-webgl-part-3-irradiance-environment-map/
            //https://developer.nvidia.com/gpugems/GPUGems2/gpugems2_chapter10.html
            //https://gist.github.com/Farfarer/5664694
            //https://codegists.com/code/render-cubemap-unity/

            //TODO conversion of existing textures:
            //https://www.marmoset.co/posts/pbr-texture-conversion/

            //https://forum.unity.com/threads/directly-draw-a-cubemap-rendertexture.296236/
            //In function Graphics.SetRenderTarget(), you can select cubemap face. 

            //convolution processing:
            //1.) CPU based
            //      Render to standard Cubemap
            //      Sample and convolve in CPU
            //      Update Cubemap from convolved data
            //      
            //2.) GPU based
            //      Shader has single Cubemap input from raw rendered
            //      Shader samples cubemap, renders out to standard surface rendertexture, one face at a time
            //      Recompose the 6x render textures back into a single Cubemap (with MIPs)            
        }

        private void debugGuiEnable()
        {
            gui = gameObject.AddComponent<ReflectionDebugGUI>();
        }

        public void debugGuiDisable()
        {
            GameObject.Destroy(gui);
            gui = null;
        }

        public void OnDestroy()
        {
            if (instance == this) { instance = null; }
            if (gui != null)
            {
                GameObject.Destroy(gui);
                gui = null;
            }
            if (debugSphere != null)
            {
                GameObject.Destroy(debugSphere);
                debugSphere = null;
            }
            //TODO proper resource cleanup
            //TODO do materials and render textures need to be released?
        }

        #endregion

        #region FUNCTIONAL METHODS

        private void init()
        {
            if (cameraObject == null)
            {
                cameraObject = new GameObject("TRReflectionCamera");
                reflectionCamera = cameraObject.AddComponent<Camera>();
                eveCameraFix = cameraObject.AddComponent<CameraAlphaFix>();
                reflectionCamera.enabled = false;
            }
            if (skyboxShader == null)
            {
                skyboxShader = KSPShaderTools.TexturesUnlimitedLoader.getShader("SSTU/Skybox/Cubemap");
                if (skyboxShader == null)
                {
                    Log.error("ERROR: TUReflectionManager - Could not find skybox shader.");
                }
            }
            probeData = createProbe();
            if (HighLogic.LoadedSceneIsEditor)
            {
                probeData.reflectionSphere.transform.position = new Vector3(0, 10, 0);
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                //probe position updated during Update()
            }
        }

        public void forceReflectionUpdate()
        {

        }

        public void updateReflections()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (!renderedEditor)
                {
                    if (editorDelay >= editorTarget)
                    {
                        renderedEditor = true;
                    }
                    editorDelay++;
                    if (renderedEditor)
                    {
                        reflectionCamera.gameObject.SetActive(true);
                        renderFullCube(probeData.renderedCube, new Vector3(0, 10, 0));
                        updateProbe(probeData);
                        reflectionCamera.gameObject.SetActive(false);
                    }
                }
            }
            else if (HighLogic.LoadedSceneIsFlight && FlightIntegrator.ActiveVesselFI != null)
            {
                Vessel vessel = FlightIntegrator.ActiveVesselFI.Vessel;
                if (vessel != null && vessel.loaded)
                {
                    //probeData.reflectionSphere.SetActive(true);
                    probeData.reflectionSphere.transform.position = vessel.transform.position;
                    probeData.updateTime++;
                    if (probeData.updateTime >= mapUpdateSpacing)
                    {
                        reflectionCamera.gameObject.SetActive(true);
                        if (TexturesUnlimitedLoader.alternateRender)
                        {
                            renderFaceAlt(probeData.renderedCube, probeData.updateFace, vessel.transform.position, probeData.updatePass);
                        }
                        else
                        {
                            renderFace(probeData.renderedCube, probeData.updateFace, vessel.transform.position, probeData.updatePass);
                        }
                        reflectionCamera.gameObject.SetActive(false);
                        probeData.updatePass++;
                        if (probeData.updatePass >= 3)
                        {
                            probeData.updateFace++;
                            probeData.updatePass = 0;
                        }
                        if (probeData.updateFace >= 6)
                        {
                            updateProbe(probeData);
                            probeData.updatePass = 0;
                            probeData.updateTime = 0;
                            probeData.updateFace = 0;
                        }
                    }
                }
                else
                {
                    //TODO -- disable reflection probe if no vessel is active/loaded?
                }
            }
            else //space center, main menu, others
            {
                //TODO -- handle reflective setups for space center, main menu, map view?
            }
        }

        #endregion

        #region UPDATE UTILITY METHODS

        private void updateProbe(ReflectionProbeData data)
        {
            data.skyboxMateral.SetTexture("_Tex", data.renderedCube);
            data.reflectionSphere.transform.rotation = Quaternion.identity;//align to world space
            data.render.material = data.skyboxMateral;
            data.probe.RenderProbe();
        }

        private void renderFullCube(RenderTexture envMap, Vector3 partPos)
        {
            int passCount = 3;
            for (int face = 0; face < 6; face++)
            {
                for (int pass = 0; pass < passCount; pass++)
                {
                    renderFace(envMap, face, partPos, pass);
                }                
            }
        }

        private void renderFaceAlt(RenderTexture envMap, int face, Vector3 partPos, int pass)
        {
            if (proxyData == null) { createProxy(); }
            int faceMask = 1 << face;
            if (renderGalaxy && pass==0)
            {
                //render galaxy to galaxy sphere texture, the galaxy
                reflectionCamera.clearFlags = CameraClearFlags.Color;
                reflectionCamera.backgroundColor = new Color(0, 0, 0, 1);
                cameraSetup(GalaxyCubeControl.Instance.transform.position, galaxyMask, 0.1f, 20f);
                reflectionCamera.RenderToCubemap(proxyData.proxyCube, faceMask);
            }
            if (renderScaled && pass==0)
            {
                //render to galaxy sphere texture, the atmosphere+scaled space
                //clear flags handle bug in unity where re-uses the same buffer before x-fer to the target face
                reflectionCamera.clearFlags = renderGalaxy ? CameraClearFlags.Depth : CameraClearFlags.Color;
                reflectionCamera.backgroundColor = new Color(0, 0, 0, 1);
                cameraSetup(ScaledSpace.Instance.transform.position, scaledSpaceMask | atmosphereMask, 1f, 3.0e7f);
                reflectionCamera.RenderToCubemap(proxyData.proxyCube, faceMask);
            }
            if (pass == 2)
            {
                //render the proxy sphere as a background skybox
                //render the local scenery in front of this new skybox
                proxyData.proxySphere.SetActive(true);
                proxyData.proxyMaterial.SetTexture("_Tex", proxyData.proxyCube);
                eveCameraFix.overwriteAlpha = eveInstalled;
                cameraSetup(partPos, renderScenery ? (sceneryMask | (1 << sphereLayer)) : (1 << sphereLayer), 0.5f, 750000f);
                reflectionCamera.RenderToCubemap(probeData.renderedCube, faceMask);
                eveCameraFix.overwriteAlpha = false;
                proxyData.proxySphere.SetActive(false);
            }
        }

        private void renderFace(RenderTexture envMap, int face, Vector3 partPos, int pass)
        {
            int faceMask = 1 << face;
            if (pass == 0)
            {
                reflectionCamera.clearFlags = CameraClearFlags.Skybox;
            }
            else
            {
                reflectionCamera.clearFlags = CameraClearFlags.Depth;
            }
            switch (pass)
            {
                case 0:
                    if (renderGalaxy)
                    {
                        //galaxy
                        renderCubeFace(envMap, faceMask, GalaxyCubeControl.Instance.transform.position, galaxyMask, 0.1f, 20f);
                    }
                    break;
                case 1:
                    if (renderScaled)
                    {
                        //scaled space
                        renderCubeFace(envMap, faceMask, ScaledSpace.Instance.transform.position, scaledSpaceMask | atmosphereMask, 1, 3.0e7f);
                    }
                    break;
                case 2:
                    if (renderScenery)
                    {
                        //scene
                        eveCameraFix.overwriteAlpha = eveInstalled;
                        renderCubeFace(envMap, faceMask, partPos, sceneryMask, 0.5f, 750000);
                        eveCameraFix.overwriteAlpha = false;
                    }
                    break;
                default:
                    break;
            }
        }

        private void renderCubeFace(RenderTexture envMap, int faceMask, Vector3 cameraPos, int layerMask, float nearClip, float farClip)
        {
            cameraSetup(cameraPos, layerMask, nearClip, farClip);
            reflectionCamera.RenderToCubemap(envMap, faceMask);
        }

        private void cameraSetup(Vector3 pos, int mask, float near, float far)
        {
            reflectionCamera.transform.position = pos;
            reflectionCamera.cullingMask = mask;
            reflectionCamera.nearClipPlane = near;
            reflectionCamera.farClipPlane = far;
        }

        private ReflectionProbeData createProbe()
        {
            GameObject refSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foreach (Collider c in refSphere.GetComponents<Collider>())
            {
                DestroyImmediate(c);
            }
            
            refSphere.transform.localScale = new Vector3(10, 10, 10);
            refSphere.layer = skyboxLayer;
            refSphere.name = "SSTUReflectionProbe";

            MeshRenderer rend = refSphere.GetComponent<MeshRenderer>();
            Material mat = new Material(skyboxShader);
            rend.material = mat;//still has to be updated later
            ReflectionProbe probe = createReflectionProbe(refSphere);
            RenderTexture tex = createTexture(envMapSize);
            ReflectionProbeData data = new ReflectionProbeData(refSphere, rend, mat, probe, tex);
            data.updateTime = mapUpdateSpacing;//force update on the first frame it is 'loaded'
            return data;
        }

        private ReflectionProbe createReflectionProbe(GameObject host)
        {
            ReflectionProbe pr = host.AddComponent<ReflectionProbe>();
            pr.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            pr.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            pr.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor;
            pr.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces;
            //pr.boxProjection = true;
            pr.hdr = false;
            pr.size = new Vector3(2000, 2000, 2000);
            pr.resolution = envMapSize;
            pr.enabled = true;
            pr.cullingMask = 1 << skyboxLayer;
            return pr;
        }

        private RenderTexture createTexture(int size)
        {
            RenderTexture tex = new RenderTexture(size, size, 24);
            tex.dimension = UnityEngine.Rendering.TextureDimension.Cube;
            tex.format = RenderTextureFormat.ARGB32;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Trilinear;
            tex.autoGenerateMips = false;
            //TODO -- loop through texture and set to default = black
            return tex;
        }

        private void createProxy()
        {
            if (proxyData == null)
            {
                GameObject galaxySphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                GameObject.DestroyImmediate(galaxySphere.GetComponent<Collider>());
                galaxySphere.layer = sphereLayer;
                galaxySphere.transform.localScale = new Vector3(10, 10, 10);
                galaxySphere.SetActive(false);
                Material galaxyMat = new Material(skyboxShader);
                galaxyMat.renderQueue = 1;
                MeshRenderer r = galaxySphere.GetComponent<MeshRenderer>();
                r.material = galaxyMat;
                RenderTexture tex = createTexture(envMapSize);
                galaxyMat.SetTexture("_Tex", tex);
                proxyData = new ReflectionSkyboxProxy(galaxySphere, tex, galaxyMat);
            }
        }

        #endregion

        #region DEBUG CUBE RENDERING

        public void renderDebugCubes()
        {
            int size = envMapSize * 4;
            Cubemap map = new Cubemap(size, TextureFormat.ARGB32, false);
            Texture2D exportTex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            Vector3 pos = HighLogic.LoadedSceneIsEditor ? new Vector3(0, 10, 0) : FlightIntegrator.ActiveVesselFI.Vessel.transform.position;
            exportCubes(map, pos);
        }

        public void renderDebugLayers()
        {
            int size = envMapSize * 4;
            Cubemap map = new Cubemap(size, TextureFormat.RGB24, false);
            Texture2D exportTex = new Texture2D(size, size, TextureFormat.RGB24, false);
            Vector3 pos = HighLogic.LoadedSceneIsEditor ? new Vector3(0, 10, 0) : FlightIntegrator.ActiveVesselFI.Vessel.transform.position;

            reflectionCamera.enabled = true;
            float nearClip = reflectionCamera.nearClipPlane;
            float farClip = 3.0e7f;

            reflectionCamera.clearFlags = CameraClearFlags.SolidColor;
            Color bg = reflectionCamera.backgroundColor;
            reflectionCamera.backgroundColor = Color.clear;

            int len = 32;
            int mask = 0;
            for (int i = 0; i < len; i++)
            {
                mask = 1 << i;
                renderCube(map, pos, mask, nearClip, farClip);
                Utils.exportCubemap(map, "layer"+i);
            }
            reflectionCamera.clearFlags = CameraClearFlags.Depth;
            reflectionCamera.enabled = false;
        }

        private void exportCubes(Cubemap debugCube, Vector3 pos)
        {
            reflectionCamera.enabled = true;
            float nearClip = reflectionCamera.nearClipPlane;
            float farClip = 3.0e7f;

            reflectionCamera.clearFlags = CameraClearFlags.SolidColor;
            Color bg = reflectionCamera.backgroundColor;
            reflectionCamera.backgroundColor = Color.clear;

            renderCube(debugCube, GalaxyCubeControl.Instance.transform.position, galaxyMask, nearClip, farClip);
            Utils.exportCubemap(debugCube, "galaxy");
            renderCube(debugCube, ScaledSpace.Instance.transform.position, scaledSpaceMask, nearClip, farClip);
            Utils.exportCubemap(debugCube, "scaled");
            renderCube(debugCube, pos, sceneryMask, nearClip, farClip);
            Utils.exportCubemap(debugCube, "scene");
            renderCube(debugCube, pos, atmosphereMask, nearClip, farClip);
            Utils.exportCubemap(debugCube, "skybox");
            renderCube(debugCube, pos, fullSceneMask, nearClip, farClip);
            Utils.exportCubemap(debugCube, "full");
            reflectionCamera.backgroundColor = bg;

            //export the same as the active reflection setup
            reflectionCamera.clearFlags = CameraClearFlags.Depth;
            for (int i = 0; i < 6; i++)
            {
                CubemapFace face = (CubemapFace)i;

                if (renderGalaxy)
                {
                    //galaxy
                    renderCubeFace(debugCube, face, GalaxyCubeControl.Instance.transform.position, galaxyMask, nearClip, farClip);
                }
                if (renderScaled)
                {
                    //scaled space
                    renderCubeFace(debugCube, face, ScaledSpace.Instance.transform.position, scaledSpaceMask, nearClip, farClip);
                }
                if (renderAtmo)
                {
                    //atmo
                    renderCubeFace(debugCube, face, pos, atmosphereMask, nearClip, farClip);
                }
                if (renderScenery)
                {
                    //scene
                    eveCameraFix.overwriteAlpha = eveInstalled;
                    renderCubeFace(debugCube, face, pos, sceneryMask, nearClip, farClip);
                    eveCameraFix.overwriteAlpha = false;
                }
            }
            Utils.exportCubemap(debugCube, "reflect");
            reflectionCamera.enabled = false;
        }

        private void renderCubeFace(Cubemap envMap, CubemapFace face, Vector3 cameraPos, int layerMask, float nearClip, float farClip)
        {
            cameraSetup(cameraPos, layerMask, nearClip, farClip);
            int faceMask = 1 << (int)face;
            reflectionCamera.RenderToCubemap(envMap, faceMask);
        }

        private void renderCube(Cubemap envMap, Vector3 cameraPos, int layerMask, float nearClip, float farClip)
        {
            cameraSetup(cameraPos, layerMask, nearClip, farClip);
            reflectionCamera.RenderToCubemap(envMap);
        }

        #endregion DEBUG RENDERING

        #region DEBUG SPHERE

        public void toggleDebugSphere()
        {
            if (debugSphere != null)
            {
                GameObject.Destroy(debugSphere);
                debugSphere = null;
            }
            else
            {
                debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugSphere.name = "ReflectionDebugSphere";
                GameObject.DestroyImmediate(debugSphere.GetComponent<Collider>());
                debugSphere.transform.localScale = Vector3.one * 10f;
                Shader metallic = TexturesUnlimitedLoader.getShader("SSTU/PBR/Metallic");
                Material mat = new Material(metallic);
                mat.SetFloat("_Metallic", 1);
                mat.SetFloat("_Smoothness", 1);
                debugSphere.GetComponent<MeshRenderer>().material = mat;
            }
        }

        #endregion DEBUG SPHERE

        #region CONTAINER CLASSES

        public enum ReflectionPass
        {
            GALAXY,
            SCALED,
            LOCAL,
        }

        public class VesselReflectionData
        {
            public readonly Vessel vessel;
            public readonly ReflectionProbeData probeData;
            public VesselReflectionData(Vessel v, ReflectionProbeData data)
            {
                this.vessel = v;
                this.probeData = data;
            }
        }

        public class EditorReflectionData
        {
            public readonly ReflectionProbeData probeData;
            public EditorReflectionData(ReflectionProbeData data)
            {
                this.probeData = data;
            }
        }

        public class ReflectionProbeData
        {
            public readonly GameObject reflectionSphere;//also the owner of the probe
            public readonly ReflectionProbe probe;
            public readonly RenderTexture renderedCube;
            public readonly Material skyboxMateral;
            public readonly MeshRenderer render;
            public int updateFace = 0;
            public int updatePass = 0;
            public int updateTime = 0;
            public ReflectionProbeData(GameObject sphere, MeshRenderer rend, Material mat, ReflectionProbe probe, RenderTexture envMap)
            {
                this.reflectionSphere = sphere;
                this.render = rend;
                this.skyboxMateral = mat;
                this.probe = probe;
                this.renderedCube = envMap;
            }
        }

        public class ReflectionSkyboxProxy
        {
            public readonly GameObject proxySphere;//background sphere used for skybox
            public readonly RenderTexture proxyCube;
            public readonly Material proxyMaterial;
            public ReflectionSkyboxProxy(GameObject sphere, RenderTexture tex, Material mat)
            {
                this.proxySphere = sphere;
                this.proxyCube = tex;
                this.proxyMaterial = mat;
            }
        }

        //from unity post: https://forum.unity.com/threads/render-texture-alpha.2065/
        //potential fix to EVE writing 0 into alpha channel on areas subject to cloud textures
        //this should be somehow ran a single time -after- the last layer of a cube-side is rendered
        //had to move to a pre-compiled shader as apparently run-time compilation is completely unsupported now
        public class CameraAlphaFix : MonoBehaviour
        {
            private float alpha = 1.0f;
            private Material mat;
            public bool overwriteAlpha = true;
     
            public void Start()
            {
                Shader setAlpha = KSPShaderTools.TexturesUnlimitedLoader.getShader("SSTU/SetAlpha");
                mat = new Material(setAlpha);
            }

            public void OnPostRender()
            {
                if (overwriteAlpha)
                {
                    overwriteAlphaChannel();
                }
            }

            public void overwriteAlphaChannel()
            {
                GL.PushMatrix();
                GL.LoadOrtho();
                mat.SetFloat("_Alpha", alpha);
                mat.SetPass(0);
                GL.Begin(GL.QUADS);
                GL.Vertex3(0, 0, 0.5f);
                GL.Vertex3(1, 0, 0.5f);
                GL.Vertex3(1, 1, 0.5f);
                GL.Vertex3(0, 1, 0.5f);
                GL.End();
                GL.PopMatrix();
            }

        }

        #endregion

    }
}