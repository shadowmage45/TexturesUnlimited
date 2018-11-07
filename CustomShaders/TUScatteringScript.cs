using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class TUScatteringScript : MonoBehaviour
{

    Material mat;
    RenderTexture tex;
    Camera camera;
    public Vector3 planetPos;
    public Vector3 sunPos;
    public float planetSize = 1;
    public float atmoSize = 1.1f;
    public float scaleHeight = 1;
    public float lightScale = 1;
    // Use this for initialization
    void Start ()
    {
        mat = new Material(Shader.Find("Hidden/TU-Scattering"));
        camera = GetComponent<Camera>();
        camera.depthTextureMode = DepthTextureMode.Depth;
        tex = new RenderTexture(Screen.width, Screen.height, 0);
        tex.autoGenerateMips = false;        
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

        var camera = GetComponent<Camera>();
        Vector3[] frustumCorners = new Vector3[4];
        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), 1, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        
        for (int i = 0; i < 4; i++)
        {
            frustumCorners[i] = camera.transform.TransformVector(frustumCorners[i]);
            //Debug.DrawRay(camera.transform.position, worldSpaceCorner, Color.blue);
        }

        Vector3 botLeft = frustumCorners[0];
        Vector3 topLeft = frustumCorners[1];        
        Vector3 topRight = frustumCorners[2];
        Vector3 botRight = frustumCorners[3];

        mat.SetVector("_Left", topLeft);
        mat.SetVector("_Right", topRight);
        mat.SetVector("_Left2", botLeft); 
        mat.SetVector("_Right2", botRight);

        mat.SetVector("_SunPos", sunPos);
        mat.SetVector("_PlanetPos", planetPos);
        mat.SetFloat("_PlanetSize", planetSize);
        mat.SetFloat("_AtmoSize", atmoSize);
        mat.SetFloat("_ScaleHeight", scaleHeight);
        mat.SetFloat("_SunPower", lightScale);
        //extract pass
        Graphics.Blit(source, tex, mat, 0);
        mat.SetTexture("_SecTex", tex);
        //combine pass
        Graphics.Blit(source, dest, mat, 1);
    }
    
}
