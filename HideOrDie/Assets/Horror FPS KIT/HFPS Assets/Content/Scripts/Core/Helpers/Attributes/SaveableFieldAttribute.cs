using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class SaveableField : Attribute {
    public string CustomKey;

    public SaveableField(string key = "")
    {
        CustomKey = key;
    }
}
