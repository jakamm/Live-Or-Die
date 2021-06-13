using System.Collections.Generic;

public interface ISaveable
{
    Dictionary<string, object> OnSave();
    void OnLoad(Newtonsoft.Json.Linq.JToken token);
}

public interface ISaveableArmsItem
{
    Dictionary<string, object> OnSave();
    void OnLoad(Newtonsoft.Json.Linq.JToken token);
}