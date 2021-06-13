using UnityEngine;
using UnityEngine.UI;

public class SavedGame : MonoBehaviour {

    public Text sceneName;
    public Text dateTime;

    [HideInInspector] public string save;
    [HideInInspector] public string scene;

    public void SetSavedGame (string SaveName, string SceneName, string DateTime) {
        sceneName.text = SceneName;
        dateTime.text = DateTime;
        scene = SceneName;
        save = SaveName;
    }
}
