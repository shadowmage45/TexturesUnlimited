using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TUSetDetailArray : MonoBehaviour 
{
	
	public Texture2D tex1;
	public Texture2D tex2;
	public Texture2D tex3;

	// Use this for initialization
	void Start () 
	{
        int width = tex1.width;
        int height = tex1.height;
        Texture2DArray array = new Texture2DArray(width, height, 3, TextureFormat.DXT5, false);
        Graphics.CopyTexture(tex1, 0, 0, array, 0, 0);
        Graphics.CopyTexture(tex2, 0, 0, array, 1, 0);
        Graphics.CopyTexture(tex3, 0, 0, array, 2, 0);
        gameObject.GetComponent<Renderer>().material.SetTexture("_DetailArray", array);
    }
	
	// Update is called once per frame
	void Update ()
    {
		
	}
}
