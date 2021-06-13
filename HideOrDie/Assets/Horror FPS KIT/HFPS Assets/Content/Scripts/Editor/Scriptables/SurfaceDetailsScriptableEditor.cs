using UnityEditor;
using Malee.Editor;

[CustomEditor(typeof(SurfaceDetailsScriptable)), CanEditMultipleObjects]
public class SurfaceDetailsScriptableEditor : Editor 
{
    SurfaceDetailsScriptable surfaceDetails;
    ReorderableList re_list;
    SerializedProperty list;

    private void OnEnable()
    {
        surfaceDetails = target as SurfaceDetailsScriptable;
        list = serializedObject.FindProperty("surfaceDetails");
        re_list = new ReorderableList(list);
        re_list.onAddCallback += delegate { OnAdd(); };
        re_list.onRemoveCallback += delegate { OnRemove(re_list.Selected); };
        re_list.onReorderCallback += delegate { ReorderEvent(); };
    }

    private void OnAdd()
    {
        SurfaceDetails surface = new SurfaceDetails
        {
            SurfaceTag = "Tag"
        };

        surfaceDetails.surfaceDetails.Add(surface);
        surfaceDetails.Reseed();
    }

    private void OnRemove(int[] ids)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            surfaceDetails.surfaceDetails.RemoveAt(ids[i]);
        }

        surfaceDetails.Reseed();
    }

    private void ReorderEvent()
    {
        surfaceDetails.Reseed();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        re_list.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}
