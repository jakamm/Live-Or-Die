using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using ThunderWire.Json;
using ThunderWire.Helpers;
using HFPS.Prefs;

public class LoadManager : MonoBehaviour
{
    public const string LOAD_STATE = "LoadState";
    public const string LOAD_LEVEL_NAME = "LevelToLoad";
    public const string LOAD_SAVE_NAME = "LoadSaveName";

    private JsonManager jsonManager;
    public SerializationSettings SaveLoadSettings;

    [Space(10)]

    public GameObject SavedGamePrefab;
    public Transform SavedGameContent;

    public Text EmptyText;
    public Button continueButton;
    public Button loadButton;
    public Button deleteButton;

    private string lastSave;
    private bool isSaveGame = false;

    private List<GameObject> saveObjects = new List<GameObject>();
    private List<SavedData> savesCache = new List<SavedData>();
    private SavedGame selectedSave;

    void Awake()
    {
        jsonManager = new JsonManager(SaveLoadSettings);
    }

    void Start()
    {
        Time.timeScale = 1f;

        LoadSaves();
        InitializeContinue();

        loadButton.onClick.AddListener(Load);
        deleteButton.onClick.AddListener(Delete);
    }

    /// <summary>
    /// Get Saved Games Asynchronously.
    /// </summary>
    public async Task<List<SavedData>> RetrieveSavedGames()
    {
        List<SavedData> result = new List<SavedData>();
        string filepath = SaveLoadSettings.GetSerializationPath();

        if (Directory.Exists(filepath))
        {
            DirectoryInfo dinfo = new DirectoryInfo(filepath);
            FileInfo[] finfo = dinfo.GetFiles("*.sav");

            if (finfo.Length > 0)
            {
                foreach (var file in finfo)
                {
                    await Task.Run(() => jsonManager.DeserializeDataAsync(file.Name));
                    result.Add(new SavedData()
                    {
                        SaveName = file.Name,
                        Scene = (string)jsonManager.Json()["scene"],
                        SaveTime = (string)jsonManager.Json()["dateTime"]
                    });
                }

                return result.OrderBy(x => x.SaveTime).ToList();
            }
        }

        return default;
    }

    async void LoadSaves()
    {
        savesCache = await Task.Run(() => RetrieveSavedGames());

        if (savesCache.Count > 0)
        {
            EmptyText.gameObject.SetActive(false);

            foreach (var save in savesCache)
            {
                GameObject savedGame = Instantiate(SavedGamePrefab);
                savedGame.transform.SetParent(SavedGameContent);
                savedGame.transform.localScale = new Vector3(1, 1, 1);
                savedGame.transform.SetSiblingIndex(0);
                savedGame.GetComponent<SavedGame>().SetSavedGame(save.SaveName, save.Scene, save.SaveTime);
                saveObjects.Add(savedGame);
            }
        }
        else
        {
            EmptyText.gameObject.SetActive(true);
        }
    }

    void InitializeContinue()
    {
        if (Prefs.Exist(Prefs.LOAD_SAVE_NAME))
        {
            lastSave = Prefs.Game_SaveName();

            if (savesCache.Count > 0 && savesCache.Any(x => x.SaveName == lastSave))
            {
                continueButton.interactable = true;
                isSaveGame = true;
            }
            else
            {
                if (File.Exists(SerializationTool.GetSerializationPath(SerializationPath.GameDataPath) + lastSave))
                {
                    continueButton.interactable = true;
                }
                else
                {
                    continueButton.interactable = false;
                }

                isSaveGame = false;
            }
        }
    }

    public void Continue()
    {
        if (isSaveGame)
        {
            SavedGame saved = saveObjects.Select(x => x.GetComponent<SavedGame>()).Where(x => x.save == lastSave).SingleOrDefault();
            Prefs.Game_LoadState(1);
            Prefs.Game_SaveName(saved.save);
            Prefs.Game_LevelName(saved.scene);
        }
        else
        {
            Prefs.Game_LoadState(2);
            Prefs.Game_SaveName(lastSave);
        }

        SceneManager.LoadScene(1);
    }

    public void Delete()
    {
        string pathToFile =  selectedSave.save;
        File.Delete(pathToFile);

        foreach (Transform g in SavedGameContent)
        {
            Destroy(g.gameObject);
        }

        saveObjects.Clear();
        LoadSaves();
    }

    public void Load()
    {
        Prefs.Game_LoadState(1);
        Prefs.Game_SaveName(selectedSave.save);
        Prefs.Game_LevelName(selectedSave.scene);

        SceneManager.LoadScene(1);
    }

    public void NewGame(string scene)
    {
        Prefs.Game_LoadState(0);
        Prefs.Game_SaveName(string.Empty);
        Prefs.Game_LevelName(scene);

        SceneManager.LoadScene(1);
    }

    void Update()
    {
        if(selectedSave != null)
        {
            loadButton.interactable = true;
            deleteButton.interactable = true;
        }
        else
        {
            loadButton.interactable = false;
            deleteButton.interactable = false;
        }

        if (EventSystem.current.currentSelectedGameObject)
        {
            GameObject select = EventSystem.current.currentSelectedGameObject;

            if (saveObjects.Contains(select))
            {
                SelectSave(select);
            }
        }
        else
        {
            Deselect();
        }
    }

    private void SelectSave(GameObject SaveObject)
    {
        if (SaveObject.GetComponent<SavedGame>())
        {
            selectedSave = saveObjects[saveObjects.IndexOf(SaveObject)].GetComponent<SavedGame>();
        }
    }

    public void Deselect()
    {
        selectedSave = null;
    }

    public void Quit()
    {
        Application.Quit();
    }
}
