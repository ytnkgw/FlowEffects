using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Skinner
{
    [CustomEditor(typeof(SkinnerTailTemplate))]
    public class SkinnerTrailTemplateEditor : Editor
    {
        #region Editor functions
        private SerializedProperty _historyLength;

        private const string _helpText =
            "The Skinner Trail renderer tries to draw trail lines as many" +
            "as possible in a single draw call, and thus the number of " +
            "lines is automatically determined from the history length.";

        private void OnEnable()
        {
            _historyLength = serializedObject.FindProperty("_historyLength");
        }

        public override void OnInspectorGUI()
        {
            var template = (SkinnerTailTemplate)target;

            // Editable properties
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_historyLength);
            var rebuild = EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            // Readonly members
            EditorGUILayout.LabelField("Line Count", template.lineCount.ToString());
            EditorGUILayout.HelpBox(_helpText, MessageType.None);

            if (rebuild) template.RebuildMesh();
        }
        #endregion

        [MenuItem("Assets/Create/Skinner/Trail Template")]
        #region Create menu item functions
        public static void CreateTemplateAsset()
        {
            // Make a proper path from the current selection.
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "")
            {
                path = path.Replace(Path.GetFileName(path), "");
            }
            var assetPathName = AssetDatabase.GenerateUniqueAssetPath(path + "New Skinner Trail Template.asset");

            // Create a template asset.
            var asset = ScriptableObject.CreateInstance<SkinnerTailTemplate>();
            AssetDatabase.CreateAsset(asset, assetPathName);
            AssetDatabase.AddObjectToAsset(asset.mesh, asset);

            // Build an initial mesh for the asset.
            asset.RebuildMesh();

            // Save asset.
            AssetDatabase.SaveAssets();

            // Activate selection.
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
        #endregion
    }
}