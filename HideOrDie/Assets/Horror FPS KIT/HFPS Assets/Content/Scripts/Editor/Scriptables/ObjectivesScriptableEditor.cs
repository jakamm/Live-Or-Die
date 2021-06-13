using UnityEditor;
using Malee.Editor;

[CustomEditor(typeof(ObjectivesScriptable)), CanEditMultipleObjects]
public class ObjectivesScriptableEditor : Editor
{
    ObjectivesScriptable objectives;
    ReorderableList re_list;
    SerializedProperty list;

    private void OnEnable()
    {
        objectives = target as ObjectivesScriptable;
        list = serializedObject.FindProperty("Objectives");
        re_list = new ReorderableList(list);
        re_list.onAddCallback += delegate { OnAdd(); };
        re_list.onRemoveCallback += delegate { OnRemove(re_list.Selected); };
        re_list.onReorderCallback += delegate { ReorderEvent(); };
    }

    private void OnAdd()
    {
        ObjectivesScriptable.Objective objective = new ObjectivesScriptable.Objective
        {
            shortName = "New Objective"
        };
        objectives.Objectives.Add(objective);
        objectives.Reseed();
    }

    private void OnRemove(int[] ids)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            objectives.Objectives.RemoveAt(ids[i]);
        }

        objectives.Reseed();
    }

    private void ReorderEvent()
    {
        objectives.Reseed();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        re_list.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}
