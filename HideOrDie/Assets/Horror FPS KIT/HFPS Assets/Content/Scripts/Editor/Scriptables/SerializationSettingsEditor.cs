using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SerializationSettings)), CanEditMultipleObjects]
public class SerializationSettingsEditor : Editor
{
    private const string RESOURCES_PATH = "Assets/Horror FPS Kit/HFPS Assets/Content/Prefabs/Resources/";

    SerializationSettings settings;

    private SerializedProperty p_EnableEncription;
    private SerializedProperty p_SerializePath;
    private SerializedProperty p_EncryptionKey;

    private void OnEnable()
    {
        settings = target as SerializationSettings;

        p_EnableEncription = serializedObject.FindProperty("EncryptData");
        p_SerializePath = serializedObject.FindProperty("SerializePath");
        p_EncryptionKey = serializedObject.FindProperty("EncryptionKey");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(p_EncryptionKey);
        EditorGUILayout.PropertyField(p_EnableEncription);
        EditorGUILayout.PropertyField(p_SerializePath);

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("Runtime Saveable Prefabs: " + settings .runtimeSaveablePaths.Count, MessageType.Info);

        EditorGUILayout.Space();

        GUIContent btnTxt = new GUIContent("Find Saveable Prefabs");
        Rect rt = GUILayoutUtility.GetRect(btnTxt, GUI.skin.button, GUILayout.Height(30));

        if (GUI.Button(rt, btnTxt, GUI.skin.button))
        {
            settings.runtimeSaveablePaths.Clear();

            string[] entries = Directory.GetFiles(RESOURCES_PATH, "*.prefab", SearchOption.AllDirectories);

            foreach (var path in entries)
            {
                string new_path = path.Substring(RESOURCES_PATH.Length, path.Length - RESOURCES_PATH.Length);
                new_path = new_path.Replace('\\', '/').Split('.')[0];

                GameObject prefab = Resources.Load<GameObject>(new_path);

                settings.runtimeSaveablePaths.Add(new SerializationSettings.RuntimeSaveablePath(new_path, prefab));
            }

            Debug.Log("<color=green>[Serialization Settings]</color> Found Runtime Saveable Prefabs: " + entries.Length);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
