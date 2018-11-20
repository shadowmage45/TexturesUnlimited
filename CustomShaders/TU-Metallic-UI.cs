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
		
		//specularSource(targetMat);				
		keywordCheckbox(targetMat, "Specular from Diffuse Alpha", "TU_STOCK_SPEC");
		keywordCheckbox(targetMat, "Subsurf", "TU_SUBSURF");		
		keywordCheckbox(targetMat, "Recoloring Enabled", "TU_RECOLOR");
		//recolorType(targetMat);
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
		string[] options = new string[]{"MetallicGloss Alpha", "Diffuse Alpha"};
		int currentIndex = 0;
		if(Array.IndexOf(mat.shaderKeywords, "TU_STOCK_SPEC")!=-1){currentIndex=1;}
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Smoothness Source");
		int index = EditorGUILayout.Popup(currentIndex, options);
		EditorGUILayout.EndHorizontal();
		if( index != currentIndex)
		{
			if(index==0)
			{
				mat.DisableKeyword("TU_STOCK_SPEC");
			}
			else if(index==1)
			{
				mat.EnableKeyword("TU_STOCK_SPEC");
			}
		}
	}
	
	private void recolorType(Material mat)
	{
		string[] options = new string[]{"Disabled", "Standard"};
		int currentIndex = 0;
		if(Array.IndexOf(mat.shaderKeywords, "TU_RECOLOR")!=-1){currentIndex=1;}
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Recolor Mode");
		int index = EditorGUILayout.Popup(currentIndex, options);
		EditorGUILayout.EndHorizontal();
		if( index != currentIndex)
		{
			if(index==0)
			{
				mat.DisableKeyword("TU_RECOLOR");
			}
			else if(index==1)
			{
				mat.EnableKeyword("TU_RECOLOR");
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
		EditorGUILayout.LabelField("Recolor Spec Norm/Input Masking Tex");
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