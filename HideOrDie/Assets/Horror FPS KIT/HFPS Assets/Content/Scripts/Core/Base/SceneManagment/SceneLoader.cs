using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ThunderWire.Scene;
using HFPS.Prefs;
using ThunderWire.CrossPlatform.Input;

public class SceneLoader : MonoBehaviour
{
    private CrossPlatformInput input;
    private GameObject MainCamera;

    public List<SceneInfo> sceneInfos = new List<SceneInfo>();

    [Space]
    public FadePanelControl fadeController;
    public TipsManager tipsManager;
    public GameObject SpinnerGO;
    public Text sceneName;
    public Text sceneDescription;
    public Image backgroundImg;
    public GameObject manuallySwitchText;

    [Space][Tooltip("Switch scene by pressing any button")]
    public bool SwitchManually;

    [Space][Tooltip("Background Loading Priority")]
    public ThreadPriority threadPriority = ThreadPriority.High;
    public int timeBeforeLoad;

    void Awake()
    {
        input = CrossPlatformInput.Instance;
    }

    void Start()
    {
        Time.timeScale = 1f;
        SpinnerGO.SetActive(true);
        manuallySwitchText.SetActive(false);
        SceneTool.threadPriority = threadPriority;

        if (tipsManager)
        {
            tipsManager.TipsText.gameObject.SetActive(true);
        }

        if (Prefs.Exist(Prefs.LOAD_LEVEL_NAME))
        {
            string scene = Prefs.Game_LevelName();
            LoadLevelAsync(scene);
        }
        else
        {
            SpinnerGO.GetComponentInChildren<Spinner>().isSpinning = false;
            Debug.LogError("Loading Error: There is no scene to load!");
        }

        if (FindObjectOfType<Camera>() != null)
        {
            MainCamera = FindObjectOfType<Camera>().gameObject;
        }
        else
        {
            MainCamera = null;
        }
    }

    public void LoadLevelAsync(string scene)
    {
        sceneName.text = scene;

        if (sceneInfos.Count > 0)
        {
            SceneInfo sceneInfo = sceneInfos.SingleOrDefault(info => info.SceneName == scene);
            if (sceneInfo != null)
            {
                sceneDescription.text = sceneInfo.SceneDescription;
                backgroundImg.sprite = sceneInfo.Background;
            }
            else
            {
                sceneDescription.text = "";
            }
        }
        else
        {
            sceneDescription.text = "";
        }

        StartCoroutine(LoadScene(scene, timeBeforeLoad));
    }

    IEnumerator LoadScene(string scene, int timeWait)
    {
        yield return new WaitForSeconds(timeWait);

        if (!SwitchManually)
        {
            StartCoroutine(SceneTool.LoadSceneAsyncSwitch(scene));
        }
        else
        {
            StartCoroutine(SceneTool.LoadSceneAsync(scene, UnityEngine.SceneManagement.LoadSceneMode.Single));

            yield return new WaitUntil(() => SceneTool.LoadingDone);

            SpinnerGO.SetActive(false);
            manuallySwitchText.SetActive(true);

            if (tipsManager)
            {
                tipsManager.TipsText.gameObject.SetActive(false);
            }

            RemoveDontDestroyOnLoad();

            yield return new WaitUntil(() => input.AnyControlPressed());

            if (!fadeController)
            {
                if (MainCamera != null)
                {
                    Destroy(MainCamera);
                }

                SceneTool.AllowSceneActivation();
            }
            else
            {
                fadeController.FadeInPanel();
                yield return new WaitUntil(() => !fadeController.isFading());

                if (MainCamera != null)
                {
                    Destroy(MainCamera);
                }

                SceneTool.AllowSceneActivation();
            }
        }
    }

    void RemoveDontDestroyOnLoad() 
    {
        DontDestroyLoad[] objs = FindObjectsOfType<DontDestroyLoad>();

        if (objs.Length > 0)
        {
            GameObject go = new GameObject("_Temp");

            foreach (var obj in objs)
            {
                obj.transform.SetParent(go.transform);
            }
        }
    }
}

[System.Serializable]
public class SceneInfo
{
    public string SceneName;
    [Multiline]
    public string SceneDescription;
    public Sprite Background;

    public SceneInfo(string name, string desc, Sprite sprite)
    {
        SceneName = name;
        SceneDescription = desc;
        Background = sprite;
    }
}