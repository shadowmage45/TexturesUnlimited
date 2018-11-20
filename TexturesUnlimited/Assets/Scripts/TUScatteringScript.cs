using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class TUScatteringScript : MonoBehaviour
{

    

    //internal refs to the game objects/etc used by simulation
    public GameObject sun;
    public GameObject planet;

    //params for the actual sphere that is being rendered
    // specified in meters of radius
    // atmo is an absolute height, and total atmo radius = planetSize + atmoHeight
    public float planetSize = 1;
    public float atmoHeight = 0.1f;
    
    //params for the planet that is being simulated
    // specified in meters of radius
    public float planetRealSize = 6310000;

    /// <summary>
    /// Atmosphere color, tints the light output (multiply)
    /// </summary>
    public Color atmoColor = Color.white;

    /// <summary>
    /// Direct IO multiplier for how much light hits the atmosphere.  Should actually use some proper quadratic scale function.
    /// What units is this in?  1.0 == ??
    /// </summary>
    public float sunIntensity = 20f;

    public int viewSamples = 16;
    public int lightSamples = 8;
    
    public bool clouds = true;

    //rayliegh scattering constants
    public float rayScaleHeight = 8500f;
    public Vector3 rayScatteringCoefficient = new Vector3(0.000005804542996261093f, 0.000013562911419845635f, 0.00003026590629238531f);

    //mie scattering constants
    public float mieScaleHeight = 1200f;
    public float mieScatteringCoefficient = 0.0021f;
    public float mieAnisotropy = 0.758f;
    
    //private internal vars, object caches
    private float scaleFactor;
    Vector3[] frustumCorners = new Vector3[4];
    Material mat;
    RenderTexture tex;
    Camera effectCam;

    // Use this for initialization
    void Start ()
    {
        mat = new Material(Shader.Find("Hidden/TU-Clouds"));
        effectCam = GetComponent<Camera>();
        effectCam.depthTextureMode = DepthTextureMode.Depth;
        tex = new RenderTexture(Screen.width, Screen.height, 0);
        tex.autoGenerateMips = false;
	}

    void OnAwake()
    {
        if (mat == null || tex == null || effectCam == null)
        {
            mat = new Material(Shader.Find("Hidden/TU-Clouds"));
            effectCam = GetComponent<Camera>();
            effectCam.depthTextureMode = DepthTextureMode.Depth;
            tex = new RenderTexture(Screen.width, Screen.height, 0);
            tex.autoGenerateMips = false;
        }
    }
	
	// Update is called once per frame
	void Update ()
    {
        // this example shows the different camera frustums when using asymmetric projection matrices (like those used by OpenVR).

        //var camera = GetComponent<Camera>();
        //Vector3[] frustumCorners = new Vector3[4];
        //camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);

        //for (int i = 0; i < 4; i++)
        //{
        //    var worldSpaceCorner = camera.transform.TransformVector(frustumCorners[i]);
        //    Debug.DrawRay(camera.transform.position, worldSpaceCorner, Color.blue);
        //}
    }

    public void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        if (mat == null || tex == null || effectCam == null) { return; }
        effectCam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), 1, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        
        for (int i = 0; i < 4; i++)
        {
            frustumCorners[i] = effectCam.transform.TransformVector(frustumCorners[i]);
        }

        Vector3 botLeft = frustumCorners[0];
        Vector3 topLeft = frustumCorners[1];        
        Vector3 topRight = frustumCorners[2];
        Vector3 botRight = frustumCorners[3];

        //bounding box frustum corners, for world-space view direction decoding
        mat.SetVector("_Left", topLeft);
        mat.SetVector("_Right", topRight);
        mat.SetVector("_Left2", botLeft); 
        mat.SetVector("_Right2", botRight);

        mat.SetVector("_SunPos", sun.transform.position);
        mat.SetVector("_PlanetPos", planet.transform.position);
        mat.SetFloat("_PlanetSize", planetSize);
        mat.SetFloat("_AtmoSize", planetSize + atmoHeight);

        mat.SetVector("_SunDir", -sun.transform.forward);

        mat.SetColor("_Color", atmoColor);
        mat.SetFloat("_SunIntensity", sunIntensity);

        mat.SetInt("_ViewSamples", viewSamples);
        mat.SetInt("_LightSamples", lightSamples);

        mat.SetFloat("_RayScaleHeight", rayScaleHeight);
        mat.SetVector("_RayScatteringCoefficient", rayScatteringCoefficient);

        mat.SetFloat("_MieScaleHeight", mieScaleHeight);
        mat.SetFloat("_MieScatteringCoefficient", mieScatteringCoefficient);
        mat.SetFloat("_MieAnisotropy", mieAnisotropy);

        float scaleAdjust = planetRealSize / planetSize;
        mat.SetFloat("_ScaleAdjustFactor", scaleAdjust);

        mat.SetInt("_Clouds", clouds ? 1 : 0);

        //extract pass, where the magic happens
        Graphics.Blit(source, tex, mat, 0);
        mat.SetTexture("_SecTex", tex);
        
        //combine pass, just blend it onto the original source image
        Graphics.Blit(source, dest, mat, 1);
    }
    
}
