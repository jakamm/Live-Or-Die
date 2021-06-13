using Diagnostics = System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UsefulTools = ThunderWire.Utility;

public class SaveGameMenu : EditorWindow
{
    void OnGUI()
    {
        SaveGameHandler handler = FindObjectOfType<SaveGameHandler>();
        SerializedObject serializedObject = new SerializedObject(handler);
        SerializedProperty list = serializedObject.FindProperty("saveableDataPairs");

        GUIStyle boxStyle = GUI.skin.GetStyle("HelpBox");
        boxStyle.fontSize = 10;
        boxStyle.alignment = TextAnchor.MiddleCenter;

        int count = handler.constantSaveables.Count;
        string warning = "";
        MessageType messageType = MessageType.None;

        if (count > 0 && handler.constantSaveables.All(pair => pair.Instance != null))
        {
            warning = "SaveGame Handler is set up successfully!";
            messageType = MessageType.Info;
        }
        else if (count > 0 && handler.constantSaveables.Any(pair => pair.Instance == null))
        {
            warning = "Some of saveable instances are missing! Please find scene saveables again!";
            messageType = MessageType.Error;
        }
        else if(count < 1)
        {
            warning = "In order to use SaveGame feature in your scene, you must find saveables first!";
            messageType = MessageType.Warning;
        }

        EditorGUI.HelpBox(new Rect(1, 0, EditorGUIUtility.currentViewWidth - 2, 40), warning, messageType);
        EditorGUI.HelpBox(new Rect(1, 40, EditorGUIUtility.currentViewWidth - 2, 30), "Found Saveables: " + count, MessageType.None);

        GUIContent btnTxt = new GUIContent("Find Saveables");
        var rt = GUILayoutUtility.GetRect(btnTxt, GUI.skin.button, GUILayout.Width(150), GUILayout.Height(30));
        rt.center = new Vector2(EditorGUIUtility.currentViewWidth / 2, rt.center.y);
        rt.y = 80;

        if (GUI.Button(rt, btnTxt, GUI.skin.button))
        {
            SetupSaveGame();
        }
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }

    static void SetupSaveGame()
    {
        SaveGameHandler handler = FindObjectOfType<SaveGameHandler>();

        if (handler != null)
        {
            Diagnostics.Stopwatch stopwatch = new Diagnostics.Stopwatch();
            stopwatch.Reset();
            stopwatch.Start();

            var saveablesQuery = from Instance in UsefulTools.Tools.FindAllSceneObjects<MonoBehaviour>()
                                 where typeof(ISaveable).IsAssignableFrom(Instance.GetType()) && !Instance.GetType().IsInterface && !Instance.GetType().IsAbstract
                                 let key = string.Format("{0}_{1}", Instance.GetType().Name, System.Guid.NewGuid().ToString("N"))
                                 select new SaveableDataPair(SaveableDataPair.DataBlockType.ISaveable, key, Instance, new string[0]);

            var attributesQuery = from Instance in UsefulTools.Tools.FindAllSceneObjects<MonoBehaviour>()
                                  let attr = Instance.GetType().GetFields().Where(field => field.GetCustomAttributes(typeof(SaveableField), false).Count() > 0 && !field.IsLiteral && field.IsPublic).Select(fls => fls.Name).ToArray()
                                  let key = string.Format("{0}_{1}", Instance.GetType().Name, System.Guid.NewGuid().ToString("N"))
                                  where attr.Count() > 0
                                  select new SaveableDataPair(SaveableDataPair.DataBlockType.Attribute, key, Instance, attr);

            var pairs = saveablesQuery.Union(attributesQuery);
            stopwatch.Stop();

            handler.constantSaveables = pairs.ToList();
            EditorUtility.SetDirty(handler);

            Debug.Log("<color=green>[Setup SaveGame Successful]</color> Found Saveable Objects: " + pairs.Count() + ", Time Elapsed: " + stopwatch.ElapsedMilliseconds + "ms");
        }
        else
        {
            Debug.LogError("[Setup SaveGame Error] To Setup SaveGame you need to Setup your scene first.");
        }
    }
}
