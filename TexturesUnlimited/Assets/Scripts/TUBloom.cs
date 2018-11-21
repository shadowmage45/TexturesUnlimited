using UnityEngine;
using System;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class TUBloom : MonoBehaviour
{
	
	//based on -- https://catlikecoding.com/unity/tutorials/advanced-rendering/bloom/
	
	private Material material;
	private bool init = false;
	
	[Range(1, 16)]
	public int iterations = 1;
	
	public void OnRenderImage(RenderTexture source, RenderTexture dest)
	{
		if(!init)
		{
			initialize();
			init = true;
		}
		RenderTexture temp = RenderTexture.GetTemporary(source.width/2, source.height/2, 0, source.format);
		Graphics.Blit(source, temp, material);
		
		Graphics.Blit(temp, dest, material);
		MonoBehaviour.print("GFX BLIT!");
	}
	
	private void initialize()
	{
		material = new Material(Shader.Find("Hidden/Post FX/Bloom"));
		material.SetFloat("_Threshold", 1);
	}
}