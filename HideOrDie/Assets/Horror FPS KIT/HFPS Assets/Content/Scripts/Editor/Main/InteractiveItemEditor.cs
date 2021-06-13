using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(InteractiveItem)), CanEditMultipleObjects]
public class InteractiveItemEditor : Editor {

    //Types
    public SerializedProperty ItemType_Prop;
    public SerializedProperty ExamineType_Prop;
    public SerializedProperty ExamineRotate_Prop;
    public SerializedProperty MessageType_Prop;
    public SerializedProperty DisableType_Prop;

    public SerializedProperty Amount_Prop;
    public SerializedProperty WeaponID_Prop;
    public SerializedProperty InventoryID_Prop;
    public SerializedProperty BackpackExpand_Prop;
    public SerializedProperty PickupSwitch_Prop;
    public SerializedProperty FloatingIcon_prop;
    public SerializedProperty ShowItemName_prop;
    public SerializedProperty AutoShortcut_prop;

    public SerializedProperty MessageText_prop;
    public SerializedProperty MessageTime_prop;
    public SerializedProperty MessageTips_prop;

    public SerializedProperty PickupSound_Prop;
    public SerializedProperty ExamineSound_Prop;
    public SerializedProperty PickupVolume_Prop;
    public SerializedProperty ExamineVolume_Prop;

    //Examine
    public SerializedProperty ExamineName_Prop;
    public SerializedProperty ExamineDistance_Prop;
    public SerializedProperty CameraFace_Prop;
    public SerializedProperty FaceRotation_Prop;

    public SerializedProperty ColDisable_Prop;
    public SerializedProperty ColEnable_Prop;

    public SerializedProperty PaperMessage_prop;
    public SerializedProperty PaperMessageSize_prop;
    public SerializedProperty ExamineCollect_prop;
    public SerializedProperty EnableCursor_prop;

    public SerializedProperty Hashtables_prop;


    void OnEnable()
    {
        //Enums
        ItemType_Prop = serializedObject.FindProperty("ItemType");
        ExamineType_Prop = serializedObject.FindProperty("examineType");
        ExamineRotate_Prop = serializedObject.FindProperty("examineRotate");
        MessageType_Prop = serializedObject.FindProperty("messageType");
        DisableType_Prop = serializedObject.FindProperty("disableType");

        MessageTips_prop = serializedObject.FindProperty("MessageTips");

        //Inventory
        WeaponID_Prop = serializedObject.FindProperty("weaponID");
        InventoryID_Prop = serializedObject.FindProperty("inventoryID");
        BackpackExpand_Prop = serializedObject.FindProperty("backpackExpandAmount");

        //Texts
        ExamineName_Prop = serializedObject.FindProperty("examineName");
        MessageText_prop = serializedObject.FindProperty("itemMessage");
        PaperMessage_prop = serializedObject.FindProperty("paperMessage");

        //Others
        Amount_Prop = serializedObject.FindProperty("pickupAmount");
        MessageTime_prop = serializedObject.FindProperty("messageShowTime");
        ExamineDistance_Prop = serializedObject.FindProperty("examineDistance");
        PaperMessageSize_prop = serializedObject.FindProperty("paperMessageSize");

        PickupSwitch_Prop = serializedObject.FindProperty("pickupSwitch");
        ExamineCollect_prop = serializedObject.FindProperty("examineCollect");
        EnableCursor_prop = serializedObject.FindProperty("enableCursor");
        ShowItemName_prop = serializedObject.FindProperty("showItemName");
        AutoShortcut_prop = serializedObject.FindProperty("autoShortcut");
        FloatingIcon_prop = serializedObject.FindProperty("floatingIconEnabled");
        CameraFace_Prop = serializedObject.FindProperty("faceToCamera");

        Hashtables_prop = serializedObject.FindProperty("itemHashtables");
        FaceRotation_Prop = serializedObject.FindProperty("faceRotation");
        ColDisable_Prop = serializedObject.FindProperty("CollidersDisable");
        ColEnable_Prop = serializedObject.FindProperty("CollidersEnable");

        //Sounds
        PickupSound_Prop = serializedObject.FindProperty("pickupSound");
        PickupVolume_Prop = serializedObject.FindProperty("pickupVolume");
        ExamineSound_Prop = serializedObject.FindProperty("examineSound");
        ExamineVolume_Prop = serializedObject.FindProperty("examineVolume");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        InteractiveItem.Type type = (InteractiveItem.Type)ItemType_Prop.enumValueIndex;
        InteractiveItem.ExamineType exmType = (InteractiveItem.ExamineType)ExamineType_Prop.enumValueIndex;
        InteractiveItem.MessageType msg = (InteractiveItem.MessageType)MessageType_Prop.enumValueIndex;

        EditorGUILayout.PropertyField(ItemType_Prop);

        if (type != InteractiveItem.Type.OnlyExamine)
        {
            EditorGUILayout.PropertyField(MessageType_Prop);
            EditorGUILayout.PropertyField(DisableType_Prop);

            if (msg != InteractiveItem.MessageType.None)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Message Options", EditorStyles.boldLabel);

                switch (msg)
                {
                    case InteractiveItem.MessageType.PickupHint:
                        EditorGUILayout.PropertyField(MessageText_prop, new GUIContent("Item Name"));
                        break;
                    case InteractiveItem.MessageType.Message:
                        EditorGUILayout.PropertyField(MessageText_prop, new GUIContent("Message"));
                        break;
                    case InteractiveItem.MessageType.ItemName:
                        EditorGUILayout.PropertyField(MessageText_prop, new GUIContent("Item Name"));
                        break;
                }

                if (msg != InteractiveItem.MessageType.Message && msg != InteractiveItem.MessageType.ItemName)
                {
                    EditorGUILayout.PropertyField(MessageTime_prop);
                    EditorGUILayout.PropertyField(MessageTips_prop, true);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Item Options", EditorStyles.boldLabel);

            switch (type)
            {
                case InteractiveItem.Type.InventoryItem:
                    EditorGUILayout.PropertyField(InventoryID_Prop, new GUIContent("Inventory ID"));
                    EditorGUILayout.PropertyField(Amount_Prop, new GUIContent("Item Amount"));
                    EditorGUILayout.PropertyField(AutoShortcut_prop, new GUIContent("Auto Assign Shortcut"));
                    break;

                case InteractiveItem.Type.ArmsItem:
                    EditorGUILayout.PropertyField(InventoryID_Prop, new GUIContent("Inventory ID"));
                    EditorGUILayout.PropertyField(WeaponID_Prop, new GUIContent("Arms ID"));
                    EditorGUILayout.PropertyField(Amount_Prop, new GUIContent("Item Amount"));
                    EditorGUILayout.PropertyField(PickupSwitch_Prop, new GUIContent("Auto Switch"));
                    EditorGUILayout.PropertyField(AutoShortcut_prop, new GUIContent("Auto Assign Shortcut"));
                    break;

                case InteractiveItem.Type.BackpackExpand:
                    EditorGUILayout.PropertyField(BackpackExpand_Prop, new GUIContent("Expand Amount"));
                    break;
            }
        }

        EditorGUILayout.PropertyField(ShowItemName_prop, new GUIContent("UIInfo Item Name"));
        EditorGUILayout.PropertyField(FloatingIcon_prop, new GUIContent("Enable Floating Icon"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Examine Options", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(ExamineType_Prop);

        if (exmType != InteractiveItem.ExamineType.None)
        {
            EditorGUILayout.PropertyField(ExamineRotate_Prop);

            if (exmType == InteractiveItem.ExamineType.Object || exmType == InteractiveItem.ExamineType.AdvancedObject)
            {
                EditorGUILayout.PropertyField(ExamineName_Prop, new GUIContent("Examine Name"));
            }

            EditorGUILayout.PropertyField(ExamineDistance_Prop, new GUIContent("Examine Distance"));
            EditorGUILayout.PropertyField(EnableCursor_prop, new GUIContent("Enable Cursor"));

            if (EnableCursor_prop.boolValue)
            {
                EditorGUILayout.PropertyField(ExamineCollect_prop, new GUIContent("Click Collect"));
            }

            EditorGUILayout.Space();

            if (exmType == InteractiveItem.ExamineType.AdvancedObject)
            {
                EditorGUILayout.PropertyField(ColDisable_Prop, new GUIContent("Colliders Disable"), true);
                EditorGUILayout.PropertyField(ColEnable_Prop, new GUIContent("Colliders Enable"), true);
            }

            if (exmType == InteractiveItem.ExamineType.Paper)
            {
                EditorGUILayout.PropertyField(PaperMessage_prop, new GUIContent("Paper Text"));
                EditorGUILayout.PropertyField(PaperMessageSize_prop, new GUIContent("Text Size"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(CameraFace_Prop, new GUIContent("Face To Camera"));

            if (CameraFace_Prop.boolValue)
            {
                EditorGUILayout.PropertyField(FaceRotation_Prop, new GUIContent("Face Rotation"));
            }
        }
        else
        {
            if (ShowItemName_prop.boolValue)
            {
                EditorGUILayout.PropertyField(ExamineName_Prop, new GUIContent("Examine Name"));
            }
        }

        if(type == InteractiveItem.Type.InventoryItem)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Custom Item Data", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(Hashtables_prop, new GUIContent("KeyValue Data"), true);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sounds", EditorStyles.boldLabel);

        if (type != InteractiveItem.Type.OnlyExamine)
        {
            EditorGUILayout.PropertyField(PickupSound_Prop, new GUIContent("Pickup Sound"));
            EditorGUILayout.PropertyField(PickupVolume_Prop, new GUIContent("Pickup Volume"));
        }
        if (exmType != InteractiveItem.ExamineType.None)
        {
            EditorGUILayout.PropertyField(ExamineSound_Prop, new GUIContent("Examine Grab Sound"));
            EditorGUILayout.PropertyField(ExamineVolume_Prop, new GUIContent("Examine Volume"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
