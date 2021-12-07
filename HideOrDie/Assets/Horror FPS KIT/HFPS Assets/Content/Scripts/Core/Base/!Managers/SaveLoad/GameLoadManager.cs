/*
 * GameLoadManager.cs - by ThunderWire Studio
 * Version 1.0
*/

using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using HFPS.Prefs;
using ThunderWire.Helpers;

/// <summary>
/// Provides additional methods for Save/Load System.
/// </summary>
public class GameLoadManager : MonoBehaviour
{
    private SaveGameHandler saveHandler;
    private HFPS_GameManager gameManager;

    public SaveGameExtension saveGameExtension;

    [Header("Save Game Panel")]
    public GameObject SavedGamePrefab;
    public Transform SavedGameContent;
    public Text EmptyText;
    public bool SelectFirstSave;

    [Header("Main Menu")]
    public bool isMainMenu;
    public bool setStartNewGame;
    public string NewGameBuildName = "Scene";
    public Button ContinueButton;
    public Button NewGameButton;

    private List<GameObject> SavesCache = new List<GameObject>();
    private SavedGame SelectedSave;

    private string lastSave;
    private bool isSaveGame;

    void Awake()
    {
        gameManager = HFPS_GameManager.Instance;
    }

    void Start()
    {
        if (isMainMenu)
        {
            LoadSaves();
        }
    }

    public async void LoadSaves()
    {
        if (saveGameExtension || (saveHandler = SaveGameHandler.Instance) != null)
        {
            List<SavedData> rawSaves = new List<SavedData>();

            if (saveGameExtension)
            {
                rawSaves = await saveGameExtension.RetrieveSavedGames();
            }
            else if (saveHandler)
            {
                rawSaves = await saveHandler.RetrieveSavedGames();
            }

            if (rawSaves != null && rawSaves.Count > 0)
            {
                EmptyText.gameObject.SetActive(false);

                foreach (var save in rawSaves)
                {
                    if (!SavesCache.Any(x => x.GetComponent<SavedGame>().save == save.SaveName))
                    {
                        GameObject obj = Instantiate(SavedGamePrefab);
                        obj.transform.SetParent(SavedGameContent);
                        obj.transform.localScale = new Vector3(1, 1, 1);
                        obj.transform.SetSiblingIndex(0);
                        Vector3 pos = obj.transform.localPosition;
                        pos.z = 0;
                        obj.transform.localPosition = pos;
                        obj.GetComponentInChildren<SavedGame>().SetSavedGame(save.SaveName, save.Scene, save.SaveTime);
                        obj.GetComponentInChildren<Button>().onClick.AddListener(delegate { OnSelect(obj); });
                        SavesCache.Add(obj);
                    }
                }
            }
            else
            {
                EmptyText.gameObject.SetActive(true);
            }
        }
        else
        {
            EmptyText.gameObject.SetActive(true);
        }

        if (isMainMenu)
        {
            InitializeContinue();
        }

        if (SelectFirstSave)
        {
            if(SavesCache.Count > 0)
            {
                GameObject save = SavesCache[SavesCache.Count - 1];
                save.GetComponentInChildren<Button>().Select();
                OnSelect(save);
            }
        }
    }

    public void OnSelect(GameObject obj)
    {
        if (obj.GetComponent<SavedGame>())
        {
            SelectedSave = SavesCache.Where(x => x == obj).Select(x => x.GetComponent<SavedGame>()).FirstOrDefault();
        }
    }

    public void LoadSelectedSave()
    {
        if (SelectedSave != null)
        {
            if (gameManager && !gameManager.isPaused)
            {
                gameManager.LockPlayerControls(false, false, false);
            }

            if (saveHandler && saveHandler.fadeControl)
            {
                saveHandler.fadeControl.FadeIn();
            }

            StartCoroutine(LoadScene());
        }
    }

    IEnumerator LoadScene()
    {
        if (saveHandler && saveHandler.fadeControl)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitUntil(() => saveHandler.fadeControl.IsFadedIn);
        }

        Prefs.Game_LoadState(1);
        Prefs.Game_SaveName(SelectedSave.save);
        Prefs.Game_LevelName(SelectedSave.scene);

        SceneManager.LoadScene(1);
    }

    public void DeleteSelectedSave()
    {
        if (SelectedSave != null && saveGameExtension)
        {
            string pathToFile = saveGameExtension.SaveLoadSettings.GetSerializationPath() + SelectedSave.save;
            File.Delete(pathToFile);
            GameObject saveObj = SelectedSave.gameObject;

            SavesCache.Remove(saveObj);
            Destroy(saveObj);

            if (SelectFirstSave)
            {
                if (SavesCache.Count > 0)
                {
                    GameObject save = SavesCache[SavesCache.Count - 1];
                    save.GetComponentInChildren<Button>().Select();
                    OnSelect(save);
                }
            }
        }
    }

    void InitializeContinue()
    {
        if (Prefs.Exist(Prefs.LOAD_SAVE_NAME))
        {
            lastSave = Prefs.Game_SaveName();

            if (SavesCache.Count > 0 && SavesCache.Any(x => x.GetComponentInChildren<SavedGame>().save.Equals(lastSave)))
            {
                ContinueButton.interactable = true;
                isSaveGame = true;
            }
            else
            {
                if (File.Exists(SerializationTool.GetSerializationPath(SerializationPath.GameDataPath) + lastSave))
                {
                    ContinueButton.interactable = true;
                }
                else
                {
                    ContinueButton.interactable = false;
                }

                isSaveGame = false;
            }
        }

        if (isMainMenu && setStartNewGame)
        {
            if (!isSaveGame)
            {
                NewGameButton.onClick.RemoveAllListeners();
                NewGameButton.onClick = new Button.ButtonClickedEvent();
                NewGameButton.onClick.AddListener(() => NewGame());
            }
        }
    }

    public void Continue()
    {
        if (isSaveGame)
        {
            SavedGame continueSave = SavesCache.Where(x => x.GetComponentInChildren<SavedGame>().save.Equals(lastSave)).Select(x => x.GetComponentInChildren<SavedGame>()).FirstOrDefault();

            Prefs.Game_LoadState(1);
            Prefs.Game_SaveName(continueSave.save);
            Prefs.Game_LevelName(continueSave.scene);
        }
        else
        {
            Prefs.Game_LoadState(2);
            Prefs.Game_SaveName(lastSave);
        }

        SceneManager.LoadScene(1);
    }

    public void NewGame()
    {
        if (!string.IsNullOrEmpty(NewGameBuildName))
        {
            Prefs.Game_LoadState(0);
            Prefs.Game_SaveName(string.Empty);
            Prefs.Game_LevelName(NewGameBuildName);

            FindObjectOfType<DataTrackerManager>().OnNewGame();
            SceneManager.LoadScene(1);
        }
        else
        {
            Debug.LogError("[GameLoadManager] New Game scene is empty!");
        }
    }

    public void NewGameScene(string sceneBuildName)
    {
        if (!string.IsNullOrEmpty(sceneBuildName))
        {
            Prefs.Game_LoadState(0);
            Prefs.Game_SaveName(string.Empty);
            Prefs.Game_LevelName(sceneBuildName);

            SceneManager.LoadScene(1);
        }
        else
        {
            Debug.LogError("[GameLoadManager] New Game scene is empty!");
        }
    }

    public void Quit()
    {
        Application.Quit();
    }
}

public struct SavedData
{
    public string SaveName;
    public string Scene;
    public string SaveTime;
}
