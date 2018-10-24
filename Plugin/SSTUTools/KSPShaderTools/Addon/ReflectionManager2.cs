using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using System.IO;
using System;
using System.Diagnostics;

namespace KSPShaderTools
{
    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class ReflectionManager2 : MonoBehaviour
    {

        #region CONSTANTS

        public const int galaxyMask = 1 << 18;
        public const int atmosphereMask = 1 << 9;
        public const int scaledSpaceMask = 1 << 10;
        public const int sceneryMask = (1 << 4) | (1 << 15) | (1<<23);// used to also contain (1<<17) -- used by EVE or something? or was I trying to cap kerbs in reflections before?
        public const int kerbalLayers = 1 << 17;
        public const int partsMask = 1 << 0;
        public const int fullSceneMask = ~0;

        #endregion

        #region CONFIG FIELDS
        
        /// <summary>
        /// Size of the rendered reflection map.  Higher resolutions result in higher fidelity reflections, but at a much higher run-time cost.
        /// Must be a power-of-two size; e.g. 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048.
        /// </summary>
        public int envMapSize = 256;

        /// <summary>
        /// Number of frames inbetween reflection map updates.
        /// </summary>
        public int mapUpdateSpacing = 60;

        #endregion

        #region DEBUG FIELDS

        //set through the reflection debug GUI
        public bool reflectionsEnabled = false;
        public bool multiPassRender = false;
        public bool debugSphereActive = false;
        public bool sphereShowsSkybox = false;

        public bool renderGalaxySky = true;
        public bool renderAtmoSky = true;
        public bool renderScaledSky = true;
        public bool renderScenerySky = false;
        public bool renderStandardSky = false;

        public bool renderGalaxyProbe = false;
        public bool renderAtmoProbe = false;
        public bool renderScaledProbe = false;
        public bool renderSceneryProbe = false;
        public bool renderStandardProbe = false;


        #endregion

        #region INTERNAL FIELDS

        public ReflectionProbe probe;
        public GameObject cameraObject;
        public GameObject cameraObject2;
        public Camera reflectionCamera;
        public Camera reflectionCamera2;
        public RenderTexture skyboxTexture;
        public RenderTexture capTex;
        public Material skyboxMaterial;
        private static Shader skyboxShader;

        //Mod interop stuff

        public bool eveInstalled = true;
        //public CameraAlphaFix eveCameraFix;

        //internal debug fields
        private bool export = false;
        private bool debug = false;
        

        private static ApplicationLauncherButton debugAppButton;
        private ReflectionDebugGUI2 gui;

        private GameObject debugSphere;
        private Material debugMaterialStd;
        private Material debugMaterialSky;
        private Stopwatch stopwatch = new Stopwatch();
        
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

            Texture2D tex;
            if (debugAppButton == null && debug)//static reference; track if the button was EVER created, as KSP keeps them even if the addon is destroyed
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

                cameraObject2 = new GameObject("TRReflectionCamera");
                reflectionCamera2 = cameraObject2.AddComponent<Camera>();
                reflectionCamera2.enabled = false;
                reflectionCamera2.clearFlags = CameraClearFlags.SolidColor;
                reflectionCamera2.nearClipPlane = 0.01f;
                reflectionCamera2.farClipPlane = 3.0e7f;
                reflectionCamera2.cullingMask = 0;
                MonoBehaviour.print("TUREFMAN2 - created camera");
            }
            if (skyboxTexture == null)
            {
                skyboxTexture = new RenderTexture(envMapSize, envMapSize, 24);
                skyboxTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;
                skyboxTexture.format = RenderTextureFormat.ARGB32;
                skyboxTexture.wrapMode = TextureWrapMode.Clamp;
                skyboxTexture.filterMode = FilterMode.Trilinear;
                skyboxTexture.autoGenerateMips = false;

                capTex = new RenderTexture(envMapSize, envMapSize, 24);
                capTex.dimension = UnityEngine.Rendering.TextureDimension.Cube;
                capTex.format = RenderTextureFormat.ARGB32;
                capTex.wrapMode = TextureWrapMode.Clamp;
                capTex.filterMode = FilterMode.Trilinear;
                capTex.autoGenerateMips = false;
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
                RenderSettings.skybox = skyboxMaterial;
                MonoBehaviour.print("TUREFMAN2 - assigned skybox material to RenderSettings.skybox");
            }
            if (probe == null)
            {
                probe = cameraObject.AddComponent<ReflectionProbe>();
                probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
                probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.EveryFrame;
                probe.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
                probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
                probe.hdr = false;
                probe.size = new Vector3(2000, 2000, 2000);
                probe.resolution = envMapSize;
                probe.enabled = true;
                probe.cullingMask = 0;
                MonoBehaviour.print("TUREFMAN2 - created refl. probe");
            }
            if (debug)
            {
                if (debugMaterialSky == null)
                {
                    debugMaterialSky = new Material(skyboxShader);
                    debugMaterialSky.SetTexture("_Tex", skyboxTexture);
                    Shader metallic = TexturesUnlimitedLoader.getShader("TU/Metallic");
                    debugMaterialStd = new Material(metallic);
                    debugMaterialStd.SetFloat("_Metallic", 1);
                    debugMaterialStd.SetFloat("_Smoothness", 1);
                }
                if (debugSphere == null)
                {
                    debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    debugSphere.name = "ReflectionDebugSphere";
                    GameObject.DestroyImmediate(debugSphere.GetComponent<Collider>());
                    debugSphere.transform.localScale = Vector3.one * 10f;
                    debugSphere.GetComponent<MeshRenderer>().material = debugMaterialStd;
                }
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
                //noop, let the probe do its thing... wtf layer should it be set to?
                //if (EditorLogic.fetch.ship != null)
                //{
                //    ShipConstruct ship = EditorLogic.fetch.ship;
                //    // //ship.shipSize
                //}
            }
            else if (HighLogic.LoadedSceneIsFlight && FlightIntegrator.ActiveVesselFI != null)
            {
                //update sphere location even if reflections are currently disabled; as they may be disabled for debugging purposes...
                if (debug && debugSphereActive)
                {
                    debugSphere.transform.position = FlightIntegrator.ActiveVesselFI.transform.position;
                }
                if (!reflectionsEnabled)
                {
                    return;
                }
                stopwatch.Start();
                if (multiPassRender)
                {
                    multiPassSkyboxRender();
                }
                else
                {
                    singlePassSkyboxRender();
                }
                //really shouldn't be needed
                //skyboxMaterial.SetTexture("_Tex", skyboxTexture);
                
                int camMask = 0;
                if (renderGalaxyProbe) { camMask = camMask | galaxyMask; }
                if (renderAtmoProbe) { camMask = camMask | atmosphereMask; }
                if (renderScaledProbe) { camMask = camMask | scaledSpaceMask; }
                if (renderSceneryProbe) { camMask = camMask | sceneryMask; }
                probe.cullingMask = camMask;

                stopwatch.Stop();
                //MonoBehaviour.print("SW Elapsed: " + stopwatch.ElapsedMilliseconds);
                stopwatch.Reset();
            }
            else //space center, main menu, others
            {
                //TODO -- handle reflective setups for space center, main menu, map view?
            }
        }

        private void multiPassSkyboxRender()
        {
            CameraClearFlags flags = reflectionCamera.clearFlags;
            Color cColor = reflectionCamera.backgroundColor;
            reflectionCamera.backgroundColor = new Color(0, 0, 0, 0);
            //complete clear for first pass, black it out for next layer
            reflectionCamera.clearFlags = CameraClearFlags.SolidColor;
            reflectionCamera.enabled = true;
            bool firstPassRendered = false;
            MonoBehaviour.print("skybox tex: " + skyboxTexture);
            if (renderGalaxySky)
            {
                cameraSetup(GalaxyCubeControl.Instance.transform.position, galaxyMask, 0.1f, 20f);
                reflectionCamera.RenderToCubemap(skyboxTexture);
                firstPassRendered = true;
            }
            if (renderAtmoSky)
            {
                cameraSetup(ScaledSpace.Instance.transform.position, atmosphereMask, 1f, 3.0e7f);
                if (firstPassRendered)
                {
                    reflectionCamera2.clearFlags = CameraClearFlags.Depth;
                    reflectionCamera2.RenderToCubemap(skyboxTexture);
                }
                else
                {
                    firstPassRendered = true;
                    reflectionCamera.RenderToCubemap(skyboxTexture);
                }                
            }
            if (renderScaledSky)
            {
                cameraSetup(ScaledSpace.Instance.transform.position, scaledSpaceMask, 0.5f, 750000f);
                if (firstPassRendered)
                {
                    reflectionCamera2.clearFlags = CameraClearFlags.Depth;
                    reflectionCamera2.RenderToCubemap(skyboxTexture);
                }
                else
                {
                    firstPassRendered = true;
                    reflectionCamera.RenderToCubemap(skyboxTexture);
                }
            }
            if (renderScenerySky)
            {
                cameraSetup(ScaledSpace.Instance.transform.position, sceneryMask, 0.5f, 750000f);
                if (firstPassRendered)
                {
                    reflectionCamera2.clearFlags = CameraClearFlags.Depth;
                    reflectionCamera2.RenderToCubemap(skyboxTexture);
                }
                else
                {
                    firstPassRendered = true;
                    reflectionCamera.RenderToCubemap(skyboxTexture);
                }
            }
            if (renderStandardSky)
            {
                cameraSetup(ScaledSpace.Instance.transform.position, partsMask, 3, 200f);
                if (firstPassRendered)
                {
                    reflectionCamera2.clearFlags = CameraClearFlags.Depth;
                    reflectionCamera2.RenderToCubemap(skyboxTexture);
                }
                else
                {
                    firstPassRendered = true;
                    reflectionCamera.RenderToCubemap(skyboxTexture);
                }
            }
            //refl probe is also on this object, so put it back at the vessel pos
            reflectionCamera.gameObject.transform.position = FlightIntegrator.ActiveVesselFI.Vessel.transform.position;
            reflectionCamera.enabled = false;
            reflectionCamera.clearFlags = flags;
            reflectionCamera.backgroundColor = cColor;
        }

        private void singlePassSkyboxRender()
        {
            int camMask = 0;
            if (renderGalaxySky) { camMask = camMask | galaxyMask; }
            if (renderAtmoSky) { camMask = camMask | atmosphereMask; }
            if (renderScaledSky) { camMask = camMask | scaledSpaceMask; }
            if (renderScenerySky) { camMask = camMask | sceneryMask; }
            if (renderStandardSky) { camMask = camMask | partsMask; }
            reflectionCamera.clearFlags = CameraClearFlags.SolidColor;
            reflectionCamera.enabled = true;
            cameraSetup(FlightIntegrator.ActiveVesselFI.Vessel.transform.position, camMask, 1, 3.0e7f);
            reflectionCamera.RenderToCubemap(skyboxTexture);
            reflectionCamera.enabled = false;
        }

        public void toggleDebugSphere(bool active)
        {
            if(debug && debugSphere!=null && active!=debugSphereActive)            
            {
                debugSphereActive = active;
                debugSphere.SetActive(debugSphereActive);
            }
        }

        public void toggleDebugSphereMaterial(bool useSkybox)
        {
            if (useSkybox != sphereShowsSkybox)
            {
                sphereShowsSkybox = useSkybox;
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

        public void setReflectionsEnabled(bool val)
        {
            if (val != reflectionsEnabled)
            {
                reflectionsEnabled = val;
                probe.enabled = val;
            }
        }

        #endregion

        #region UPDATE UTILITY METHODS
        
        private void cameraSetup(Vector3 pos, int mask, float near, float far)
        {
            reflectionCamera.transform.position = pos;
            reflectionCamera.cullingMask = mask;
            reflectionCamera.nearClipPlane = near;
            reflectionCamera.farClipPlane = far;

            reflectionCamera2.transform.position = pos;
            reflectionCamera2.cullingMask = mask;
            reflectionCamera2.nearClipPlane = near;
            reflectionCamera2.farClipPlane = far;
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

        public void dumpGalaxyAndAtmoData()
        {
            MonoBehaviour.print("Galaxy Data...");
            Renderer[] galaxyRenderers = GalaxyCubeControl.Instance.GetComponentsInChildren<Renderer>();
            int len = galaxyRenderers.Length;
            for (int i = 0; i < len; i++)
            {
                Material mat = galaxyRenderers[i].material;
                MonoBehaviour.print("Galaxy renderer: "+mat);
                MonoBehaviour.print("Galaxy rend q: " + mat.renderQueue);
                MonoBehaviour.print("Galaxy shader: " + mat.shader);                
            }
            MonoBehaviour.print("Atmo data");
            AtmosphereFromGround atmo = GameObject.FindObjectOfType<AtmosphereFromGround>();
            if (atmo != null)
            {
                Renderer atmoRend = atmo.gameObject.GetComponentInChildren<Renderer>();
                MonoBehaviour.print("Atmo mat: " + atmoRend.material);
                MonoBehaviour.print("Atmo rend q: " + atmoRend.material.renderQueue);
                MonoBehaviour.print("Atmo shader: " + atmoRend.material.shader);
            }

        }

        public void fixGalaxyAndAtmo()
        {
            MonoBehaviour.print("Galaxy Data...");
            Renderer[] galaxyRenderers = GalaxyCubeControl.Instance.GetComponentsInChildren<Renderer>();
            int len = galaxyRenderers.Length;
            for (int i = 0; i < len; i++)
            {
                Material mat = galaxyRenderers[i].material;
                mat.renderQueue = 1000;//geo - 2
                mat.SetOverrideTag("RenderType", "Background");
                MonoBehaviour.print("Galaxy renderer: " + mat);
                MonoBehaviour.print("Galaxy rend q: " + mat.renderQueue);
                MonoBehaviour.print("Galaxy shader: " + mat.shader);
            }
            MonoBehaviour.print("Atmo data");
            AtmosphereFromGround atmo = GameObject.FindObjectOfType<AtmosphereFromGround>();
            if (atmo != null)
            {
                Renderer atmoRend = atmo.gameObject.GetComponentInChildren<Renderer>();
                Material mat = atmoRend.material;
                mat.renderQueue = 1001;//geo - 1
                mat.SetOverrideTag("RenderType", "Background");
                atmoRend.gameObject.transform.localScale = Vector3.one * 100;//push it outwards?
                
                MonoBehaviour.print("Atmo mat: " + atmoRend.material);
                MonoBehaviour.print("Atmo rend q: " + atmoRend.material.renderQueue);
                MonoBehaviour.print("Atmo shader: " + atmoRend.material.shader);
            }

            GameObject[] objects = GameObject.FindObjectsOfType<GameObject>();
            len = objects.Length;
            for (int i = 0; i < len; i++)
            {
                if (objects[i].layer == 9)
                {
                    MonoBehaviour.print("Found layer 9 object: " + objects[i]);
                    Renderer[] rends = objects[i].GetComponentsInChildren<Renderer>();
                    int len2 = rends.Length;
                    for (int k = 0; k < len2; k++)
                    {
                        Material mat = rends[k].material;
                        if (mat != null)
                        {
                            mat.renderQueue = 1000;
                            mat.SetOverrideTag("RenderType", "Background");
                        }
                        MonoBehaviour.print("Set layer 9 GO to RQ=1000, type=Background");
                    }
                }
            }
        }

        #endregion DEBUG RENDERING
        
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
            manager.setReflectionsEnabled(addButtonRowToggle("Reflections Enabled", manager.reflectionsEnabled));
            manager.multiPassRender = addButtonRowToggle("Multi-Pass Reflection Capture", manager.multiPassRender);
            manager.toggleDebugSphere(addButtonRowToggle("Debug Sphere Active", manager.debugSphereActive));
            manager.toggleDebugSphereMaterial(addButtonRowToggle("Sphere Use Skybox", manager.sphereShowsSkybox));
            
            manager.renderGalaxySky = addButtonRowToggle("Render Galaxy Skybox", manager.renderGalaxySky);
            manager.renderAtmoSky = addButtonRowToggle("Render Atmo Skybox", manager.renderAtmoSky);
            manager.renderScaledSky = addButtonRowToggle("Render Scaled Skybox", manager.renderScaledSky);
            manager.renderScenerySky = addButtonRowToggle("Render Scenery Skybox", manager.renderScenerySky);
            manager.renderStandardSky = addButtonRowToggle("Render Parts Skybox", manager.renderStandardSky);

            manager.renderGalaxyProbe = addButtonRowToggle("Render Galaxy Probe", galaxy);
            manager.renderAtmoProbe = addButtonRowToggle("Render Atmo Probe", atmo);
            manager.renderScaledProbe = addButtonRowToggle("Render Scaled Probe", scaled);
            manager.renderSceneryProbe = addButtonRowToggle("Render Scenery Probe", scenery);
            manager.renderStandardProbe = addButtonRowToggle("Render Parts Probe", manager.renderStandardProbe);

            if (GUILayout.Button("Export Debug Cube Maps"))
            {
                manager.renderDebugCubes();
            }
            if (GUILayout.Button("Dump Galaxy Data"))
            {
                manager.dumpGalaxyAndAtmoData();
            }
            if (GUILayout.Button("Fix Galaxy/Atmo"))
            {
                manager.fixGalaxyAndAtmo();
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
            GUILayoutOption width = GUILayout.Width(160);
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