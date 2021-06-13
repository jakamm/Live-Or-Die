using System.Linq;
using UnityEditor;

[CustomEditor(typeof(SaveGameHandler)), CanEditMultipleObjects]
public class SaveGameHandlerEditor : Editor
{
    private SerializedProperty p_SaveLoadSettings;
    private SerializedProperty p_crossScene;
    private SerializedProperty p_forceSaveLoad;
    private SerializedProperty p_fadeControl;
    private SerializedProperty p_saveableDataPairs;
    private SerializedProperty p_run_saveableDataPairs;
    private SaveGameHandler handler;

    void OnEnable()
    {
        handler = target as SaveGameHandler;
        p_SaveLoadSettings = serializedObject.FindProperty("SaveLoadSettings");
        p_crossScene = serializedObject.FindProperty("crossSceneSaving");
        p_forceSaveLoad = serializedObject.FindProperty("forceSaveLoad");
        p_fadeControl = serializedObject.FindProperty("fadeControl");
        p_saveableDataPairs = serializedObject.FindProperty("constantSaveables");
        p_run_saveableDataPairs = serializedObject.FindProperty("runtimeSaveables");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        bool forceSL = p_forceSaveLoad.boolValue;

        if (handler.SaveLoadSettings != null)
        {
            if (handler.constantSaveables.Count > 0 && handler.runtimeSaveables.Count > 0)
            {
                if (handler.constantSaveables.All(pair => pair.Instance != null) || handler.runtimeSaveables.All(pair => pair.Data.All(x => x.Instance != null)))
                {
                    EditorGUILayout.HelpBox($"Constant Saveables: {p_saveableDataPairs.arraySize}\nRuntime Saveables: {p_run_saveableDataPairs.arraySize}", MessageType.Info);
                }
                else if(!forceSL)
                {
                    EditorGUILayout.HelpBox("Some of saveable instances are missing! Please find scenes saveables again!", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("Some of saveable instances are missing!", MessageType.Warning);
                }
            }
            else if(handler.constantSaveables.Count > 0)
            {
                if (handler.constantSaveables.All(pair => pair.Instance != null))
                {
                    EditorGUILayout.HelpBox("Constant Saveables: " + p_saveableDataPairs.arraySize, MessageType.Info);
                }
                else if(!forceSL)
                {
                    EditorGUILayout.HelpBox("Some of saveable instances are missing! Please find scenes saveables again!", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("Some of saveable instances are missing!", MessageType.Warning);
                }
            }
            else if (handler.runtimeSaveables.Count > 0)
            {
                if (handler.runtimeSaveables.All(pair => pair.Data.All(x => x.Instance != null)))
                {
                    EditorGUILayout.HelpBox("Runtime Saveables: " + p_run_saveableDataPairs.arraySize, MessageType.Info);
                }
                else if(!forceSL)
                {
                    EditorGUILayout.HelpBox("Some of the Runtime instances are missing!", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("Some of the Runtime instances are missing!", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("If you want to find saveable objects, select Saveables Manager from Tools menu!", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Missing SaveLoadSettings!", MessageType.Error);
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Main Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(p_SaveLoadSettings);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Other Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(p_crossScene);
        EditorGUILayout.PropertyField(p_forceSaveLoad);
        EditorGUILayout.PropertyField(p_fadeControl);

        serializedObject.ApplyModifiedProperties();
    }
}
