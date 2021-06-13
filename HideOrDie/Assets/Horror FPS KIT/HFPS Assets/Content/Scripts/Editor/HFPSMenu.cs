using System.IO;
using System.Text;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using ThunderWire.Helpers;
using TWTools = ThunderWire.Utility.Tools;

public class HFPSMenu : EditorWindow
{
    private bool encrypt;
    private SerializationPath filePath;
    private string key;

    public const string MMANAGER = "_MAINMENU";
    public const string GMANAGER = "_GAMEUI";
    public const string HERO = "HEROPLAYER";
    public const string PLAYER = "FPSPLAYER";

    public const string PATH_MMANAGER = "Setup/MainMenu/" + MMANAGER;
    public const string PATH_GMANAGER = "Setup/Game/" + GMANAGER;
    public const string PATH_HERO = "Setup/Game/" + HERO;
    public const string PATH_PLAYER = "Setup/Game/" + PLAYER;
    public const string PATH_SCRIPTABLES = "Assets/Horror FPS Kit/HFPS Assets/Scriptables/";
    public const string PATH_GAMESETTINGS = "Assets/Horror FPS Kit/HFPS Assets/Scriptables/Game Scriptables/";

    [MenuItem("Tools/HFPS KIT/Setup/Game/FirstPerson")]
    static void SetupFPS()
    {
        GameObject GameManager = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>(PATH_GMANAGER)) as GameObject;
        GameObject Player = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>(PATH_PLAYER)) as GameObject;

        Player.transform.position = new Vector3(0, 0, 0);
        GameManager.GetComponent<HFPS_GameManager>().Player = Player;
        GameManager.GetComponent<SaveGameHandler>().constantSaveables = new System.Collections.Generic.List<SaveableDataPair>();
        Player.GetComponentInChildren<ScriptManager>().m_GameManager = GameManager.GetComponent<HFPS_GameManager>();
    }

    [MenuItem("Tools/HFPS KIT/Setup/Game/FirstPerson", true)]
    static bool CheckSetupFPS()
    {
        if (GameObject.Find(MMANAGER))
        {
            return false;
        }

        if (GameObject.Find(GMANAGER))
        {
            return false;
        }

        if (GameObject.Find(PLAYER))
        {
            return false;
        }

        return true;
    }

    [MenuItem("Tools/HFPS KIT/Setup/Game/FirstPerson Body")]
    static void SetupFPSB()
    {
        GameObject GameManager = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>(PATH_GMANAGER)) as GameObject;
        GameObject Player = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>(PATH_HERO)) as GameObject;

        Player.transform.position = new Vector3(0, 0, 0);
        GameManager.GetComponent<HFPS_GameManager>().Player = Player;
        GameManager.GetComponent<SaveGameHandler>().constantSaveables = new System.Collections.Generic.List<SaveableDataPair>();
        Player.GetComponentInChildren<ScriptManager>().m_GameManager = GameManager.GetComponent<HFPS_GameManager>();
    }

    [MenuItem("Tools/HFPS KIT/Setup/Game/FirstPerson Body", true)]
    static bool CheckSetupFPSB()
    {
        if (GameObject.Find(MMANAGER))
        {
            return false;
        }

        if (GameObject.Find(GMANAGER))
        {
            return false;
        }

        if (GameObject.Find(HERO))
        {
            return false;
        }

        return true;
    }

    [MenuItem("Tools/HFPS KIT/Setup/MainMenu")]
    static void SetupMainMenu()
    {
        if (DestroyAll())
        {
            PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>(PATH_MMANAGER));
        }
    }

    [MenuItem("Tools/HFPS KIT/Setup/MainMenu", true)]
    static bool CheckSetupMainMenu()
    {
        if (GameObject.Find(MMANAGER))
        {
            return false;
        }

        if (GameObject.Find(GMANAGER))
        {
            return false;
        }

        if (GameObject.Find(HERO) || GameObject.Find(PLAYER))
        {
            return false;
        }

        return true;
    }

    [MenuItem("Tools/HFPS KIT/Setup/Fix/FirstPerson")]
    static void FixFirstPerson()
    {
        GameObject GameManager;
        GameObject Player;

        if (FindObjectWith<HFPS_GameManager>())
        {
            GameManager = FindObjectWith<HFPS_GameManager>();
        }
        else
        {
            GameManager = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>(PATH_GMANAGER)) as GameObject;
        }

        if (FindObjectWith<PlayerController>())
        {
            Player = FindObjectWith<PlayerController>();
        }
        else
        {
            Player = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>(PATH_PLAYER)) as GameObject;
        }

        GameManager.GetComponent<HFPS_GameManager>().Player = Player;
        Player.GetComponentInChildren<ScriptManager>().m_GameManager = GameManager.GetComponent<HFPS_GameManager>();

        EditorUtility.SetDirty(GameManager.GetComponent<HFPS_GameManager>());
        EditorUtility.SetDirty(Player.GetComponentInChildren<ScriptManager>());

        Debug.Log("<color=green>Everything should be OK!</color>");
    }

    [MenuItem("Tools/HFPS KIT/Setup/Fix/FirstPerson Body")]
    static void FixFirstPersonBody()
    {
        GameObject GameManager;
        GameObject Player;

        if (FindObjectWith<HFPS_GameManager>())
        {
            GameManager = FindObjectWith<HFPS_GameManager>();
        }
        else
        {
            GameManager = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>(PATH_GMANAGER)) as GameObject;
        }

        if (FindObjectWith<PlayerController>())
        {
            Player = FindObjectWith<PlayerController>();
        }
        else
        {
            Player = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>(PATH_HERO)) as GameObject;
        }

        GameManager.GetComponent<HFPS_GameManager>().Player = Player;
        Player.GetComponentInChildren<ScriptManager>().m_GameManager = GameManager.GetComponent<HFPS_GameManager>();

        EditorUtility.SetDirty(GameManager.GetComponent<HFPS_GameManager>());
        EditorUtility.SetDirty(Player.GetComponentInChildren<ScriptManager>());

        Debug.Log("<color=green>Everything should be OK!</color>");
    }

    static bool DestroyBase()
    {
        if (TWTools.MainCamera().gameObject != null)
        {
            DestroyImmediate(TWTools.MainCamera().gameObject);
            return true;
        }

        return true;
    }

    static bool DestroyAll()
    {
        if (FindObjectsOfType<GameObject>().Length > 0)
        {
            foreach (GameObject o in FindObjectsOfType<GameObject>().Select(obj => obj.transform.root.gameObject).ToArray())
            {
                DestroyImmediate(o);
            }

            if (FindObjectsOfType<GameObject>().Length < 1)
            {
                return true;
            }
        }
        else
        {
            return true;
        }

        return true;
    }

    static GameObject FindObjectWith<T>() where T : MonoBehaviour
    {
        T find = FindObjectOfType<T>();
        return find != null ? find.gameObject : null;
    }

    [MenuItem("Tools/HFPS KIT/" + "Scriptables" + "/New Inventory Database")]
    static void CreateInventoryDatabase()
    {
        CreateAssetFile<InventoryScriptable>("InventoryDatabase");
    }

    [MenuItem("Tools/HFPS KIT/" + "Scriptables" + "/New Scene Objectives")]
    static void CreateObjectiveDatabase()
    {
        CreateAssetFile<ObjectivesScriptable>(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " Objectives", "Objectives 2");
    }

    [MenuItem("Tools/HFPS KIT/" + "Scriptables" + "/New Cross-Platform Scheme")]
    static void CreateCrossPlatformAsset()
    {
        CreateAssetFile<CrossPlatformControlScheme>("ControlScheme");
    }

    [MenuItem("Tools/HFPS KIT/" + "Scriptables" + "/New Cross-Platform Sprites")]
    static void CreateCrossPlatformSprites()
    {
        CreateAssetFile<CrossPlatformSprites>("CrossPlatformSprites");
    }

    [MenuItem("Tools/HFPS KIT/" + "Scriptables" + "/New Surface Details")]
    static void CreateSurfaceDetails()
    {
        CreateAssetFile<SurfaceDetailsScriptable>("Surface Details");
    }

    [MenuItem("Tools/HFPS KIT/" + "Scriptables" + "/New Serialization Settings")]
    static void ShowWindow()
    {
        EditorWindow window = GetWindow<HFPSMenu>(false, "Create Serialization Settings", true);
        window.minSize = new Vector2(500, 130);
        window.maxSize = new Vector2(500, 130);
        window.Show();
    }

    [MenuItem("Tools/HFPS KIT/SaveGame/" + "Delete All Saved Games")]
    static void DeleteSavedGame()
    {
        if (Directory.Exists(GetPath()))
        {
            DirectoryInfo dinfo = new DirectoryInfo(GetPath());
            FileInfo[] finfo = dinfo.GetFiles("*.sav");

            if (finfo.Length > 0)
            {
                for (int i = 0; i < finfo.Length; i++)
                {
                    File.Delete(finfo[i].FullName);
                }

                if (finfo.Length > 1)
                {
                    EditorUtility.DisplayDialog("SavedGames Deleted", $"{finfo.Length} Saved Games was deleted successfully.", "Okay");
                }
                else
                {
                    EditorUtility.DisplayDialog("SavedGames Deleted", "Saved Game was deleted successfully.", "Okay");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Directory empty", "Folder is empty.", "Okay");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Directory not found", "Failed to find Directory:  " + GetPath(), "Okay");
        }
    }

    [MenuItem("Tools/HFPS KIT/SaveGame/" + "Saveables Manager")]
    static void SavedGameManager()
    {
        if (FindObjectOfType<SaveGameHandler>())
        {
            EditorWindow window = GetWindow<SaveGameMenu>(true, "Saveables Editor", true);
            window.minSize = new Vector2(500, 130);
            window.maxSize = new Vector2(500, 130);
            window.Show();
        }
        else
        {
            Debug.LogError("[SaveableManager] Could not find a SaveGameHandler script!");
        }
    }

    [MenuItem("Tools/HFPS KIT/Add FloatingIcons")]
    static void AddFloatingIcon()
    {
        if (Selection.gameObjects.Length > 0)
        {
            FloatingIconManager uIFloatingItem = FindObjectOfType<FloatingIconManager>();

            foreach (var obj in Selection.gameObjects)
            {
                uIFloatingItem.FloatingIcons.Add(obj);
            }

            EditorUtility.SetDirty(uIFloatingItem);
            Debug.Log("<color=green>" + Selection.gameObjects.Length + " objects are marked as Floating Icon</color>");
        }
        else
        {
            Debug.Log("<color=red>Please select one or more items which will be marked as Floating Icon</color>");
        }
    }

    void OnGUI()
    {
        encrypt = EditorGUILayout.Toggle("Enable Encryption:", encrypt);
        filePath = (SerializationPath)EditorGUILayout.EnumPopup("Serialization Path:", SerializationPath.GameDataPath);
        key = EditorGUILayout.TextField("Encryption Key:", key);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Create", GUILayout.Width(100), GUILayout.Height(30)))
        {
            SerializationSettings asset = CreateInstance<SerializationSettings>();

            asset.EncryptData = encrypt;
            asset.SerializePath = filePath;
            asset.EncryptionKey = MD5Hash(key);

            AssetDatabase.CreateAsset(asset, PATH_GAMESETTINGS + "SerializationSettings" + ".asset");
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = asset;
        }
    }

    private static void CreateAssetFile<T>(string AssetName, string Folder = "") where T : ScriptableObject
    {
        var asset = CreateInstance<T>();

        Folder = !string.IsNullOrEmpty(Folder) && !Folder.Contains("/") ? Folder + "/" : Folder;
        string folderpath = PATH_GAMESETTINGS + Folder;

        if(!Directory.Exists(PATH_GAMESETTINGS + Folder))
        {
            Directory.CreateDirectory(PATH_GAMESETTINGS + Folder);
            AssetDatabase.Refresh();
        }

        ProjectWindowUtil.CreateAsset(asset, folderpath + "New " + AssetName + ".asset");
    }

    public static string MD5Hash(string Data)
    {
        MD5 md5 = new MD5CryptoServiceProvider();
        byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(Data));

        StringBuilder stringBuilder = new StringBuilder();

        foreach (byte b in hash)
        {
            stringBuilder.AppendFormat("{0:x2}", b);
        }

        return stringBuilder.ToString();
    }

    private static string GetPath()
    {
        if (Directory.Exists(PATH_GAMESETTINGS))
        {
            if (Directory.GetFiles(PATH_GAMESETTINGS).Length > 0)
            {
                return SerializationTool.GetSerializationPath(AssetDatabase.LoadAssetAtPath<SerializationSettings>(PATH_GAMESETTINGS + "SerializationSettings.asset").SerializePath);
            }
            return SerializationTool.GetSerializationPath(SerializationPath.GameDataPath);
        }
        else
        {
            return SerializationTool.GetSerializationPath(SerializationPath.GameDataPath);
        }
    }
}

public static class ScriptableFinder
{
    public static T GetScriptable<T>(string AssetName) where T : ScriptableObject
    {
        string path = HFPSMenu.PATH_GAMESETTINGS;

        if (Directory.Exists(path))
        {
            if (Directory.GetFiles(path).Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<T>(path + AssetName + ".asset");
            }
            return null;
        }
        else
        {
            return null;
        }
    }
}
