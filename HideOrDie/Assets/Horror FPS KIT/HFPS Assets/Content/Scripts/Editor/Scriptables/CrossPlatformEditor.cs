using System.Linq;
using UnityEditor;
using UnityEngine;
using Malee.Editor;

[CustomEditor(typeof(CrossPlatformControlScheme)), CanEditMultipleObjects]
public class CrossPlatformEditor : Editor
{
    CrossPlatformControlScheme controlScheme;

    SerializedProperty prop_schemeType;
    SerializedProperty prop_activeGPScheme;

    ReorderableList relist_keyboardScheme;
    ReorderableList relist_gamepadScheme;

    private void OnEnable()
    {
        controlScheme = target as CrossPlatformControlScheme;

        prop_schemeType = serializedObject.FindProperty("schemeType");
        prop_activeGPScheme = serializedObject.FindProperty("activeGamepadScheme");

        relist_keyboardScheme = new ReorderableList(serializedObject.FindProperty("keyboardScheme"));
        relist_keyboardScheme.onAddCallback += OnAddKeyboard;

        relist_gamepadScheme = new ReorderableList(serializedObject.FindProperty("gamepadScheme"));
        relist_gamepadScheme.onAddCallback += OnAddGamepad;
    }

    private void OnAddKeyboard(ReorderableList list)
    {
        controlScheme.keyboardScheme.Add(new CrossPlatformControlScheme.KeyboardControl()
        {
            ActionName = "New Input"
        });
    }

    private void OnAddGamepad(ReorderableList list)
    {
        controlScheme.gamepadScheme.Add(new CrossPlatformControlScheme.NestedGamepadControls()
        {
            schemeName = "New Gamepad Scheme"
        });
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        CrossPlatformControlScheme.SchemeType schemeType = (CrossPlatformControlScheme.SchemeType)prop_schemeType.enumValueIndex;

        EditorGUILayout.PropertyField(prop_schemeType);
        EditorGUILayout.Space();

        if (schemeType == CrossPlatformControlScheme.SchemeType.Keyboard)
        {
            relist_keyboardScheme.DoLayoutList();
        }
        else if (schemeType == CrossPlatformControlScheme.SchemeType.Gamepad)
        {
            EditorGUILayout.PropertyField(prop_activeGPScheme);
            EditorGUILayout.Space();
            relist_gamepadScheme.DoLayoutList();
        }

        EditorGUILayout.Space();

        GUIContent btnTxt = new GUIContent("Copy Other Actions");
        Rect rt = GUILayoutUtility.GetRect(btnTxt, GUI.skin.button, GUILayout.Height(30));

        if (GUI.Button(rt, btnTxt, GUI.skin.button))
        {
            CopyActions(schemeType);
        }

        serializedObject.ApplyModifiedProperties();
    }

    void CopyActions(CrossPlatformControlScheme.SchemeType scheme)
    {
        var script = target as CrossPlatformControlScheme;

        if (scheme == CrossPlatformControlScheme.SchemeType.Keyboard)
        {
            foreach (var action in script.gamepadScheme[prop_activeGPScheme.intValue].gamepadControls)
            {
                if(!script.keyboardScheme.Any(x => x.ActionName == action.ActionName))
                {
                    script.keyboardScheme.Add(new CrossPlatformControlScheme.KeyboardControl() { ActionName = action.ActionName });
                }
            }
        }
        else
        {
            foreach (var action in script.keyboardScheme)
            {
                if (!script.gamepadScheme[prop_activeGPScheme.intValue].gamepadControls.Any(x => x.ActionName == action.ActionName))
                {
                    script.gamepadScheme[prop_activeGPScheme.intValue].gamepadControls.Add(new CrossPlatformControlScheme.GamepadControl() { ActionName = action.ActionName });
                }
            }
        }

        EditorUtility.SetDirty(script);

        Debug.Log("All actions has been copied!");
    }
}
