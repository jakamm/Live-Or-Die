using System.Collections;
using UnityEngine;

public class Autosave : MonoBehaviour
{
    [SaveableField, HideInInspector]
    public bool isSaved;

    private SaveGameHandler saveGame;

    void Awake()
    {
        saveGame = SaveGameHandler.Instance;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && !isSaved)
        {
            isSaved = true;
            StartCoroutine(Save());
        }
    }

    private IEnumerator Save()
    {
        yield return new WaitUntil(() => isSaved);
        saveGame.SaveSerializedData(true);
    }
}
