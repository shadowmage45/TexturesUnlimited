using UnityEngine;
using UnityEditor;
using System;

public class TUMetallicUI : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        // render the default gui
        base.OnGUI(materialEditor, properties);

        Material targetMat = materialEditor.target as Material;
		
		specularSource(targetMat);
		
		keywordCheckbox(targetMat, "Bump Map", "TU_BUMPMAP");		
		keywordCheckbox(targetMat, "Subsurf", "TU_SUBSURF");
		keywordCheckbox(targetMat, "Emission", "TU_EMISSIVE");
		recolorType(targetMat);
		recolorNormalizationType(targetMat);
    }
	
	private void keywordCheckbox(Material mat, string description, string keyword)
	{
        // see if keyword is set
        bool enabled = Array.IndexOf(mat.shaderKeywords, keyword) != -1;
		// tell unity that things may change in the UI, and to update the shader
        EditorGUI.BeginChangeCheck();
        enabled = EditorGUILayout.Toggle(description, enabled);
        if (EditorGUI.EndChangeCheck())
        {
            // enable or disable the keyword based on checkbox
            if (enabled)
                mat.EnableKeyword(keyword);
            else
                mat.DisableKeyword(keyword);
        }		
	}
	
	private void specularSource(Material mat)
	{
		string[] options = new string[]{"MetallicGloss Alpha", "Diffuse Alpha", "MetallicGloss R"};
		int currentIndex = 0;
		if(Array.IndexOf(mat.shaderKeywords, "TU_STD_SPEC")!=-1){currentIndex=0;}
		else if(Array.IndexOf(mat.shaderKeywords, "TU_STOCK_SPEC")!=-1){currentIndex=1;}
		else if(Array.IndexOf(mat.shaderKeywords, "TU_LEGACY_SPEC")!=-1){currentIndex=2;}
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Smoothness Source");
		int index = EditorGUILayout.Popup(currentIndex, options);
		EditorGUILayout.EndHorizontal();
		if( index != currentIndex)
		{
			if(index==0)
			{
				mat.EnableKeyword("TU_STD_SPEC");
				mat.DisableKeyword("TU_STOCK_SPEC");
				mat.DisableKeyword("TU_LEGACY_SPEC");
			}
			else if(index==1)
			{
				mat.DisableKeyword("TU_STD_SPEC");
				mat.EnableKeyword("TU_STOCK_SPEC");
				mat.DisableKeyword("TU_LEGACY_SPEC");
			}
			else if(index==2)
			{
				mat.DisableKeyword("TU_STD_SPEC");
				mat.DisableKeyword("TU_STOCK_SPEC");
				mat.EnableKeyword("TU_LEGACY_SPEC");
			}
		}
	}
	
	private void recolorType(Material mat)
	{
		string[] options = new string[]{"Disabled", "Standard", "Tinting"};
		int currentIndex = 0;
		if(Array.IndexOf(mat.shaderKeywords, "TU_RECOLOR_OFF")!=-1){currentIndex=0;}
		else if(Array.IndexOf(mat.shaderKeywords, "TU_RECOLOR_STANDARD")!=-1){currentIndex=1;}
		else if(Array.IndexOf(mat.shaderKeywords, "TU_RECOLOR_TINTING")!=-1){currentIndex=2;}
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Recolor Mode");
		int index = EditorGUILayout.Popup(currentIndex, options);
		EditorGUILayout.EndHorizontal();
		if( index != currentIndex)
		{
			if(index==0)
			{
				mat.EnableKeyword("TU_RECOLOR_OFF");
				mat.DisableKeyword("TU_RECOLOR_STANDARD");
				mat.DisableKeyword("TU_RECOLOR_TINTING");
			}
			else if(index==1)
			{
				mat.DisableKeyword("TU_RECOLOR_OFF");
				mat.EnableKeyword("TU_RECOLOR_STANDARD");
				mat.DisableKeyword("TU_RECOLOR_TINTING");
			}
			else if(index==2)
			{
				mat.DisableKeyword("TU_RECOLOR_OFF");
				mat.DisableKeyword("TU_RECOLOR_STANDARD");
				mat.EnableKeyword("TU_RECOLOR_TINTING");
			}
		}
	}
	
	private void recolorNormalizationType(Material mat)
	{
		string[] options = new string[]{"Disabled", "Normalization Only", "Input Mask Only", "Normalize + Input Mask"};
		int currentIndex = 0;
		if(Array.IndexOf(mat.shaderKeywords, "TU_RECOLOR_NORM")!=-1){currentIndex=1;}
		else if(Array.IndexOf(mat.shaderKeywords, "TU_RECOLOR_INPUT")!=-1){currentIndex=2;}
		else if(Array.IndexOf(mat.shaderKeywords, "TU_RECOLOR_NORM_INPUT")!=-1){currentIndex=3;}
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Recolor Mode");
		int index = EditorGUILayout.Popup(currentIndex, options);
		EditorGUILayout.EndHorizontal();
		if( index != currentIndex)
		{
			if(index==0)
			{
				mat.DisableKeyword("TU_RECOLOR_NORM");
				mat.DisableKeyword("TU_RECOLOR_INPUT");
				mat.DisableKeyword("TU_RECOLOR_NORM_INPUT");
			}
			else if(index==1)
			{
				mat.EnableKeyword("TU_RECOLOR_NORM");
				mat.DisableKeyword("TU_RECOLOR_INPUT");
				mat.DisableKeyword("TU_RECOLOR_NORM_INPUT");
			}
			else if(index==2)
			{
				mat.DisableKeyword("TU_RECOLOR_NORM");
				mat.EnableKeyword("TU_RECOLOR_INPUT");
				mat.DisableKeyword("TU_RECOLOR_NORM_INPUT");
			}
			else if(index==3)
			{
				mat.DisableKeyword("TU_RECOLOR_NORM");
				mat.DisableKeyword("TU_RECOLOR_INPUT");
				mat.EnableKeyword("TU_RECOLOR_NORM_INPUT");
			}
		}
	}
	
}