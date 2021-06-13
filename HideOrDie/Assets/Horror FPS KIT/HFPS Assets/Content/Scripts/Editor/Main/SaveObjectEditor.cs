using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SaveObject)), CanEditMultipleObjects]
public class SaveObjectEditor : Editor
{
    /*
    public SerializedProperty prop_saveType;
    public SerializedProperty prop_name;
    public SerializedProperty prop_dynamicObject;
    public SerializedProperty prop_lever_hasDoor;
    public SerializedProperty prop_disableLoad;
    public SerializedProperty prop_includeAttr;

    void OnEnable()
    {
        prop_saveType = serializedObject.FindProperty("saveType");
        prop_name = serializedObject.FindProperty("uniqueName");
        prop_dynamicObject = serializedObject.FindProperty("dynamicObject");
        prop_lever_hasDoor = serializedObject.FindProperty("hasDoor");
        prop_disableLoad = serializedObject.FindProperty("disableLoad");
        prop_includeAttr = serializedObject.FindProperty("includeAttribute");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SaveObject.SaveType type = (SaveObject.SaveType)prop_saveType.enumValueIndex;
        EditorGUILayout.PropertyField(prop_saveType);
        EditorGUILayout.PropertyField(prop_name, new GUIContent("Unique Name:"));
        EditorGUILayout.PropertyField(prop_includeAttr, new GUIContent("Include Attribute:"));

        EditorGUILayout.Space();

        if(type == SaveObject.SaveType.Lever || type == SaveObject.SaveType.Valve)
        {
            EditorGUILayout.PropertyField(prop_disableLoad, new GUIContent("Disable Load"));
        }

        if(type == SaveObject.SaveType.Lever)
        {
            EditorGUILayout.PropertyField(prop_lever_hasDoor, new GUIContent("Has Door"));
            if (prop_lever_hasDoor.boolValue)
            {
                EditorGUILayout.PropertyField(prop_dynamicObject, new GUIContent("Lever Door"));
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
    */
}
