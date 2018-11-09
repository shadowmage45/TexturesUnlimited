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
        
    //rayliegh scattering constants
    public float rayScaleHeight = 8500;
    public float rayScatterCoefficient;

    //mie scattering constants
    public float mieScaleHeight = 1200;
    public float mieScatterCoefficient;

    //private internal vars, object caches
    private float scaleFactor;
    Vector3[] frustumCorners = new Vector3[4];
    Material mat;
    RenderTexture tex;
    Camera effectCam;

    // Use this for initialization
    void Start ()
    {
        mat = new Material(Shader.Find("Hidden/TU-Scattering"));
        effectCam = GetComponent<Camera>();
        effectCam.depthTextureMode = DepthTextureMode.Depth;
        tex = new RenderTexture(Screen.width, Screen.height, 0);
        tex.autoGenerateMips = false;
	}

    void OnAwake()
    {
        if (mat == null || tex == null || effectCam == null)
        {
            mat = new Material(Shader.Find("Hidden/TU-Scattering"));
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
        effectCam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), 1, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        
        for (int i = 0; i < 4; i++)
        {
            frustumCorners[i] = effectCam.transform.TransformVector(frustumCorners[i]);
        }

        Vector3 botLeft = frustumCorners[0];
        Vector3 topLeft = frustumCorners[1];        
        Vector3 topRight = frustumCorners[2];
        Vector3 botRight = frustumCorners[3];

        mat.SetVector("_Left", topLeft);
        mat.SetVector("_Right", topRight);
        mat.SetVector("_Left2", botLeft); 
        mat.SetVector("_Right2", botRight);

        mat.SetVector("_SunPos", sun.transform.position);
        mat.SetVector("_PlanetPos", planet.transform.position);
        mat.SetVector("_SunDir", (sun.transform.position - effectCam.transform.position).normalized);
        mat.SetFloat("_PlanetSize", planetSize);
        mat.SetFloat("_AtmoSize", planetSize + atmoHeight);        

        mat.SetFloat("_RayScaleHeight", rayScaleHeight);
        mat.SetFloat("_MieScaleHeight", mieScaleHeight);

        float scaleAdjust = planetRealSize / planetSize;
        mat.SetFloat("_ScaleAdjustFactor", scaleAdjust);

        //extract pass, where the magic happens
        Graphics.Blit(source, tex, mat, 0);
        mat.SetTexture("_SecTex", tex);
        
        //combine pass, just blend it onto the original source image
        Graphics.Blit(source, dest, mat, 1);
    }
    
}
