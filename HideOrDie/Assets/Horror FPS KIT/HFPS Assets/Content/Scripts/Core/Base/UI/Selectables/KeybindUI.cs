using UnityEngine;
using UnityEngine.UI;

public class KeybindUI : MonoBehaviour
{
    public Button KeybindButton;
    public Text ActionNameText;

    [HideInInspector]
    public KeyMouse KeyBind;
    [HideInInspector]
    public string ActionName;

    public void Initialize(KeyMouse key, string action, string name = "")
    {
        ActionNameText.text = string.IsNullOrEmpty(name) ? action : name;
        ActionName = action;
        KeybindButton.GetComponentInChildren<Text>().text = key.ToString();
        KeyBind = key;
    }

    public void RebindKey(KeyMouse newKey)
    {
        KeyBind = newKey;
        KeybindButton.GetComponentInChildren<Text>().text = newKey.ToString();
    }

    public void ResetKey()
    {
        KeybindButton.GetComponentInChildren<Text>().text = KeyBind.ToString();
    }

    public void KeyText(string text)
    {
        KeybindButton.GetComponentInChildren<Text>().text = text;
    }
}
