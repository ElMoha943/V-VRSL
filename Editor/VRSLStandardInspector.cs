#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace VRSL.Shaders
{
    // Use Unity's built-in Standard shader inspector for built-in render pipeline materials.
    public class VRSLStandardInspector : ShaderGUI
    {
        private ShaderGUI standardGUI = null;
        private bool guiCheckComplete;

        private void EnsureShaderGUIAvailable()
        {
            if (guiCheckComplete) return;
            guiCheckComplete = true;

            try
            {
                Assembly editorAssembly = typeof(EditorGUILayout).Assembly;
                Type standardGUIType = editorAssembly.GetType("UnityEditor.StandardShaderGUI");
                if (standardGUIType != null) standardGUI = Activator.CreateInstance(standardGUIType) as ShaderGUI;
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to create Standard GUI: " + e.Message);
            }
        }

        // material changed check
        public override void ValidateMaterial(Material material)
        {
            EnsureShaderGUIAvailable();
            standardGUI?.ValidateMaterial(material);
        }

        // shader change check
        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            EnsureShaderGUIAvailable();
            standardGUI?.AssignNewShaderToMaterial(material, oldShader, newShader);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            EnsureShaderGUIAvailable();
            standardGUI?.OnGUI(materialEditor, properties);
        }
    }
}
#endif
