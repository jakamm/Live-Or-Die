using UnityEditor;
using Malee.Editor;

[CustomEditor(typeof(InventoryScriptable)), CanEditMultipleObjects]
public class InventoryScriptableEditor : Editor
{
    InventoryScriptable database;
    ReorderableList re_list;
    SerializedProperty list;

    private void OnEnable()
    {
        database = target as InventoryScriptable;
        list = serializedObject.FindProperty("ItemDatabase");
        re_list = new ReorderableList(list);
        re_list.onAddCallback += delegate { OnAdd(); };
        re_list.onRemoveCallback += delegate { OnRemove(re_list.Selected); };
        re_list.onReorderCallback += delegate { ReorderEvent(); };
    }

    private void OnAdd()
    {
        InventoryScriptable.ItemMapper itemMapper = new InventoryScriptable.ItemMapper
        {
            Title = "New Item"
        };
        database.ItemDatabase.Add(itemMapper);
        database.Reseed();
    }

    private void OnRemove(int[] ids)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            database.ItemDatabase.RemoveAt(ids[i]);
        }

        database.Reseed();
    }

    private void ReorderEvent()
    {
        database.Reseed();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        re_list.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}