using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using System.IO;
using System;

namespace KSPShaderTools
{
    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class ReflectionManager2 : MonoBehaviour
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
        /// Size of the rendered reflection map.  Higher resolutions result in higher fidelity reflections, but at a much higher run-time cost.
        /// Must be a power-of-two size; e.g. 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048.
        /// </summary>
        public int envMapSize = 256;

        #endregion

        #region DEBUG FIELDS

        //set through the reflection debug GUI
        public bool reflectionsEnabled = false;

        public bool renderGalaxySky = false;
        public bool renderScaledSky = false;
        public bool renderAtmoSky = false;
        public bool renderScenerySky = false;

        public bool renderGalaxyProbe = false;
        public bool renderScaledProbe = false;
        public bool renderAtmoProbe = false;
        public bool renderSceneryProbe = false;

        public bool sphereShowsSkybox = false;
        
        #endregion

        #region INTERNAL FIELDS

        public ReflectionProbe probe;
        public GameObject cameraObject;
        public Camera reflectionCamera;
        public RenderTexture skyboxTexture;
        public Material skyboxMaterial;
        private static Shader skyboxShader;
        
        private static ApplicationLauncherButton debugAppButton;
        private ReflectionDebugGUI2 gui;
        private GameObject debugSphere;
        private Material debugMaterialStd;
        private Material debugMaterialSky;

        //debug/prototype stuff

        private static ReflectionManager2 instance;

        public static ReflectionManager2 Instance
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
            MonoBehaviour.print("ReflectionManager2 Awake()");
            instance = this;  

            Texture2D tex;
            if (debugAppButton == null)//static reference; track if the button was EVER created, as KSP keeps them even if the addon is destroyed
            {                
                //create a new button
                tex = GameDatabase.Instance.GetTexture("Squad/PartList/SimpleIcons/R&D_node_icon_veryheavyrocketry", false);
                debugAppButton = ApplicationLauncher.Instance.AddModApplication(debugGuiEnable, debugGuiDisable, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB, tex);
            }
            else if(debugAppButton != null)
            {
                //reseat callback refs to the ones from THIS instance of the KSPAddon (old refs were stale, pointing to methods for a deleted class instance)
                debugAppButton.onTrue = debugGuiEnable;
                debugAppButton.onFalse = debugGuiDisable;
            }
        }

        public void Start()
        {
            init();
        }

        /// <summary>
        /// Unity per-frame update method.  Should update any reflection maps that need updating.
        /// </summary>
        public void Update()
        {
            updateReflections();
            if (debugSphere != null)
            {
                if (FlightIntegrator.ActiveVesselFI != null)
                {
                    debugSphere.transform.position = FlightIntegrator.ActiveVesselFI.transform.position;
                }
            } 
        }

        private void debugGuiEnable()
        {
            gui = gameObject.AddComponent<ReflectionDebugGUI2>();
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
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
            if (cameraObject == null)
            {
                cameraObject = new GameObject("TRReflectionCamera");
                reflectionCamera = cameraObject.AddComponent<Camera>();
                reflectionCamera.enabled = false;
                reflectionCamera.clearFlags = CameraClearFlags.SolidColor;
                reflectionCamera.nearClipPlane = 0.01f;
                reflectionCamera.farClipPlane = 3.0e7f;
                reflectionCamera.cullingMask = 0;
                MonoBehaviour.print("TUREFMAN2 - created camera");
            }
            if (skyboxTexture == null)
            {
                skyboxTexture = createTexture(envMapSize);
                MonoBehaviour.print("TUREFMAN2 - created skybox texture");
            }
            if (skyboxShader == null)
            {
                skyboxShader = KSPShaderTools.TexturesUnlimitedLoader.getShader("TU/Skybox");
                if (skyboxShader == null)
                {
                    MonoBehaviour.print("ERROR: SSTUReflectionManager - Could not find skybox shader.");
                }
                skyboxMaterial = new Material(skyboxShader);
                skyboxMaterial.SetTexture("_Tex", skyboxTexture);
                MonoBehaviour.print("TUREFMAN2 - created skybox material and assigned tex");
            }
            if (probe == null)
            {
                probe = createReflectionProbe(this.gameObject);
                MonoBehaviour.print("TUREFMAN2 - created refl. probe");
            }
            if (debugMaterialSky == null)
            {
                debugMaterialSky = new Material(skyboxShader);
                debugMaterialSky.SetTexture("_Tex", skyboxTexture);
                Shader metallic = TexturesUnlimitedLoader.getShader("SSTU/PBR/Metallic");
                debugMaterialStd = new Material(metallic);
                debugMaterialStd.SetFloat("_Metallic", 1);
                debugMaterialStd.SetFloat("_Smoothness", 1);
            }
            
            if (HighLogic.LoadedSceneIsEditor)
            {
                probe.transform.position = new Vector3(0, 10, 0);
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                //probe position updated during Update()
            }
        }

        public void updateReflections()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {

            }
            else if (HighLogic.LoadedSceneIsFlight && FlightIntegrator.ActiveVesselFI != null)
            {
                if (reflectionCamera == null)
                {
                    MonoBehaviour.print("CAM WAS NULL");
                }
                if (skyboxTexture == null)
                {
                    MonoBehaviour.print("SKYTEX WAS NULL");
                }
                if (skyboxMaterial == null)
                {
                    MonoBehaviour.print("SKYMAT WAS NULL");
                }
                int camMask = 0;
                if (renderGalaxySky) { camMask = camMask | galaxyMask; }
                if (renderAtmoSky) { camMask = camMask | atmosphereMask; }
                if (renderScaledSky) { camMask = camMask | scaledSpaceMask; }
                if (renderScenerySky) { camMask = camMask | sceneryMask; }
                reflectionCamera.cullingMask = camMask;
                reflectionCamera.enabled = true;
                reflectionCamera.gameObject.transform.position = FlightIntegrator.ActiveVesselFI.Vessel.transform.position;
                reflectionCamera.RenderToCubemap(skyboxTexture);
                reflectionCamera.enabled = false;
                skyboxMaterial.SetTexture("_Tex", skyboxTexture);
                RenderSettings.skybox = skyboxMaterial;
                
                probe.gameObject.transform.position = FlightIntegrator.ActiveVesselFI.Vessel.transform.position;
                camMask = 0;
                if (renderGalaxyProbe) { camMask = camMask | galaxyMask; }
                if (renderAtmoProbe) { camMask = camMask | atmosphereMask; }
                if (renderScaledProbe) { camMask = camMask | scaledSpaceMask; }
                if (renderSceneryProbe) { camMask = camMask | sceneryMask; }
                probe.cullingMask = camMask;

                probe.enabled = reflectionsEnabled;

            }
            else //space center, main menu, others
            {
                //TODO -- handle reflective setups for space center, main menu, map view?
            }
            if (debugSphere)
            {
                if (sphereShowsSkybox)
                {
                    debugMaterialSky.SetTexture("_Tex", skyboxTexture);
                    debugSphere.GetComponent<MeshRenderer>().material = debugMaterialSky;
                }
                else
                {                    
                    debugSphere.GetComponent<MeshRenderer>().material = debugMaterialStd;

                }
            }
        }

        #endregion

        #region UPDATE UTILITY METHODS

        private void renderCubeFace(RenderTexture envMap, Vector3 cameraPos, int layerMask, float nearClip, float farClip)
        {
            cameraSetup(cameraPos, layerMask, nearClip, farClip);
            reflectionCamera.RenderToCubemap(envMap);
        }

        private void cameraSetup(Vector3 pos, int mask, float near, float far)
        {
            reflectionCamera.transform.position = pos;
            reflectionCamera.cullingMask = mask;
            reflectionCamera.nearClipPlane = near;
            reflectionCamera.farClipPlane = far;
        }

        private ReflectionProbe createReflectionProbe(GameObject host)
        {
            ReflectionProbe pr = host.AddComponent<ReflectionProbe>();
            pr.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            pr.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.EveryFrame;
            pr.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
            pr.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            pr.hdr = false;
            pr.size = new Vector3(2000, 2000, 2000);
            pr.resolution = envMapSize;
            pr.enabled = true;
            pr.cullingMask = 0;//nothing -- only skybox
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
            return tex;
        }

        #endregion

        #region DEBUG CUBE RENDERING

        public void renderDebugCubes()
        {
            Utils.exportCubemap(skyboxTexture, "SkyboxRenderCap");
            if (probe.bakedTexture != null)
            {
                Utils.exportCubemap((RenderTexture)probe.bakedTexture, "RenderProbeCap");
            }
            else if (probe.customBakedTexture != null)
            {
                Utils.exportCubemap((RenderTexture)probe.customBakedTexture, "RenderProbeCap");
            }
            else
            {
                MonoBehaviour.print("ERROR: Probe did not have any baked textures!");
            }
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

                if (renderGalaxyProbe)
                {
                    //galaxy
                    renderCubeFace(debugCube, face, GalaxyCubeControl.Instance.transform.position, galaxyMask, nearClip, farClip);
                }
                if (renderScaledProbe)
                {
                    //scaled space
                    renderCubeFace(debugCube, face, ScaledSpace.Instance.transform.position, scaledSpaceMask, nearClip, farClip);
                }
                if (renderAtmoProbe)
                {
                    //atmo
                    renderCubeFace(debugCube, face, pos, atmosphereMask, nearClip, farClip);
                }
                if (renderSceneryProbe)
                {
                    //scene
                    renderCubeFace(debugCube, face, pos, sceneryMask, nearClip, farClip);
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
                debugSphere.GetComponent<MeshRenderer>().material = debugMaterialStd;
            }
        }

        #endregion DEBUG SPHERE

    }
    
    public class ReflectionDebugGUI2 : MonoBehaviour
    {

        private static Rect windowRect = new Rect(Screen.width - 900, 40, 800, 600);
        private int windowID = 0;

        public void Awake()
        {
            windowID = GetInstanceID();
        }

        public void OnGUI()
        {
            try
            {
                windowRect = GUI.Window(windowID, windowRect, updateWindow, "SSTUReflectionDebug");
            }
            catch (Exception e)
            {
                MonoBehaviour.print("Caught exception while rendering SSTUReflectionDebug GUI");
                MonoBehaviour.print(e.Message);
                MonoBehaviour.print(System.Environment.StackTrace);
            }
        }

        private void updateWindow(int id)
        {
            ReflectionManager2 manager = ReflectionManager2.Instance;
            bool galaxy = manager.renderGalaxyProbe;
            bool atmo = manager.renderAtmoProbe;
            bool scaled = manager.renderScaledProbe;
            bool scenery = manager.renderSceneryProbe;
            GUILayout.BeginVertical();
            manager.reflectionsEnabled = addButtonRowToggle("Reflections Enabled", manager.reflectionsEnabled);
            manager.sphereShowsSkybox = addButtonRowToggle("Debug Use Skybox", manager.sphereShowsSkybox);

            manager.renderGalaxySky = addButtonRowToggle("Render Galaxy Skybox", manager.renderGalaxySky);
            manager.renderAtmoSky = addButtonRowToggle("Render Atmo Skybox", manager.renderAtmoSky);
            manager.renderScaledSky = addButtonRowToggle("Render Scaled Skybox", manager.renderScaledSky);
            manager.renderScenerySky = addButtonRowToggle("Render Scenery Skybox", manager.renderScenerySky);

            manager.renderGalaxyProbe = addButtonRowToggle("Render Galaxy Probe", galaxy);
            manager.renderAtmoProbe = addButtonRowToggle("Render Atmo Probe", atmo);
            manager.renderScaledProbe = addButtonRowToggle("Render Scaled Probe", scaled);
            manager.renderSceneryProbe = addButtonRowToggle("Render Scenery Probe", scenery);

            if (GUILayout.Button("Toggle Debug Sphere"))
            {
                manager.toggleDebugSphere();
            }
            if (GUILayout.Button("Export Debug Cube Maps"))
            {
                manager.renderDebugCubes();
            }
            if (GUILayout.Button("Export Debug Cube Layer"))
            {
                manager.renderDebugLayers();
            }
            if (GUILayout.Button("Dump world data"))
            {
                Utils.dumpWorldHierarchy();
            }
            if (GUILayout.Button("Dump cam data"))
            {
                Utils.dumpCameraData();
            }
            if (GUILayout.Button("Dump Stock Refl Data"))
            {
                Utils.dumpReflectionData();
            }
            if (GUILayout.Button("Dump model UV Maps"))
            {
                TexturesUnlimitedLoader.dumpUVMaps(true);
            }
            GUILayout.EndVertical();
        }

        private bool addButtonRowToggle(string text, bool value)
        {
            GUILayout.BeginHorizontal();
            GUILayoutOption width = GUILayout.Width(100);
            GUILayout.Label(text, width);
            GUILayout.Label(value.ToString(), width);
            if (GUILayout.Button("Toggle", width))
            {
                value = !value;
            }
            GUILayout.EndHorizontal();
            return value;
        }

        private bool addButtonRow(string text)
        {
            GUILayout.BeginHorizontal();
            GUILayoutOption width = GUILayout.Width(100);
            GUILayout.Label(text, width);
            bool value = GUILayout.Button("Toggle", width);
            GUILayout.EndHorizontal();
            return value;
        }

        private bool addButtonRow(string labelText, string buttonText)
        {
            GUILayout.BeginHorizontal();
            GUILayoutOption width = GUILayout.Width(100);
            GUILayout.Label(labelText, width);
            bool value = GUILayout.Button(buttonText, width);
            GUILayout.EndHorizontal();
            return value;
        }

    }

}