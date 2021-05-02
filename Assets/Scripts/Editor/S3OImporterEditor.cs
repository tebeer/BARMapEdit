using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(S3OImporter))]
public class S3OImporterEditor : ScriptedImporterEditor
{
    public override void OnInspectorGUI()
    {
        //var colorShift = new GUIContent("Color Shift");
        //var prop = serializedObject.FindProperty("m_ColorShift");
        //EditorGUILayout.PropertyField(prop, colorShift);
        EditorGUILayout.LabelField("S3O Model");
        base.ApplyRevertGUI();
    }
}