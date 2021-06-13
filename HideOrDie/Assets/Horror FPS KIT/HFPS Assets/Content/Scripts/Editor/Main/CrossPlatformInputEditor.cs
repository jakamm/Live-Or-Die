using UnityEditor;
using ThunderWire.CrossPlatform.Input;

[CustomEditor(typeof(CrossPlatformInput)), CanEditMultipleObjects]
public class CrossPlatformInputEditor : Editor
{
    CrossPlatformInput input;

    void OnEnable()
    {
        input = target as CrossPlatformInput;
    }

    public override void OnInspectorGUI()
    {
        Device device = input.deviceType;
        CrossPlatformControlScheme.SchemeType scheme = input.schemeType;
        string gpScheme = device == Device.Gamepad ? input.activeGamepadSchemeName : "KB Layout";

        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.HelpBox($"Device Type: {device.ToString()}\nScheme Type: {scheme.ToString()}\nActive Scheme: {gpScheme}", MessageType.Info);
        EditorGUILayout.Space();
        DrawPropertiesExcluding(serializedObject, "m_Script");

        if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
    }
}
