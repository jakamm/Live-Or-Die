using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ThunderWire.Helpers;
using ThunderWire.Utility;

public class UIObjectInfo : MonoBehaviour {

    public string objectTitle;

    [Header("Main")]
    public MonoBehaviour Script;
    public string FieldName;

    [Header("Titles")]
    public string useText = "Use";
    public string trueTitle = "Close";
    public string falseTitle = "Open";

    [Header("Settings")]
    public bool changeUseText = false;
    public bool isUppercased;

    private FieldInfo field;

    void Start()
    {
        if (!string.IsNullOrEmpty(objectTitle) || changeUseText) return;

        if (!Script)
        {
            Debug.LogError("Please assign which script you want to use!");
        }
        else if (string.IsNullOrEmpty(FieldName))
        {
            Debug.LogError("Please assign TitleParameter!");
        }

        field = Script.GetType().GetFields().SingleOrDefault(fls => fls.Name == FieldName && fls.IsPublic);

        if (field == null)
        {
            Debug.LogError("[" + gameObject.GameObjectPath() +"] Unable to find Title Parameter or Title Parameter field is private!");
        }
    }

    void Update()
    {
        if (field != null)
        {
            try
            {
                bool fieldValue = Parser.Convert<bool>(Script.GetType().InvokeMember(FieldName, BindingFlags.GetField, null, Script, null).ToString());

                if (fieldValue)
                {
                    objectTitle = trueTitle;
                }
                else
                {
                    objectTitle = falseTitle;
                }
            }
            catch(Exception ex)
            {
                Debug.Log(ex.Message);
            }
        }

        if (isUppercased && !string.IsNullOrEmpty(objectTitle))
        {
            objectTitle = objectTitle.ToUpper();
        }
    }
}
