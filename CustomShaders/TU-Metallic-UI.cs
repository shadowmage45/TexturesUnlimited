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
		keywordCheckbox(targetMat, "Bump Map", "TU_BUMPMAP");
		keywordCheckbox(targetMat, "Smoothness from Diffuse Alpha", "TU_STOCK_SPEC");
		keywordCheckbox(targetMat, "Subsurf", "TU_SUBSURF");
		keywordCheckbox(targetMat, "Emission", "TU_EMISSIVE");
		keywordCheckbox(targetMat, "Recolor Standard", "TU_RECOLOR_STANDARD");
		keywordCheckbox(targetMat, "Std. Recolor Spec Normalization", "TU_RECOLOR_NORM");
		keywordCheckbox(targetMat, "Std. Recolor Spec Control Mask", "TU_RECOLOR_INPUT");
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
	
}