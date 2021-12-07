using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DataTracker : MonoBehaviour, IPauseEvent, ISaveable
{
    float timer;
    bool isPaused = true;

    bool isDeadOnce = false;
    bool isShotOnce = false;

    string tt = "TempTimer";
    string td = "TempIsDead";
    string ts = "TempIsShot";

    DataTrackerManager DTM;

    void Start()
    {
        DTM = FindObjectOfType<DataTrackerManager>();

        if (!DTM)
        {
            Debug.Log("Can't find DTM");
            return;
        }

        DTM.SetCurrentTracker(this);
        isPaused = false;







        //SceneManager.sceneLoaded += OnSceneLoaded;
        //if (PlayerPrefs.HasKey(tt)) timer = PlayerPrefs.GetFloat(tt);
        //if (PlayerPrefs.HasKey(td)) isDeadOnce = PlayerPrefs.GetFloat(td) == 1 ? true : false;
        //if (PlayerPrefs.HasKey(ts)) isShotOnce = PlayerPrefs.GetFloat(ts) == 1 ? true : false;
    }

    //private void OnSceneLoaded(Scene currScene, LoadSceneMode sceneMode)
    //{
    //    if (!new int[] {0,1,2}.Contains(currScene.buildIndex)) //if current scene is not 0 or 1 or 2
    //    {
    //        isPaused = false;
    //    }
    //}

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!isPaused) timer += Time.deltaTime;
    }

    public float getTime() => timer;
    public bool getIsShot() => isShotOnce;
    public bool getIsDead() => isDeadOnce;

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 100, 20), timer.ToString());
    }

    public void OnPauseEvent(bool isPaused)
    {
        this.isPaused = isPaused;
    }

    public void shoted() => isShotOnce = true;
    public void deaded() => isDeadOnce = true;

    public void ResetAll()
    {
        timer = 0;
        isPaused = false;
        isDeadOnce = false;
        isShotOnce = false;
        //PlayerPrefs.DeleteKey("TempTimer");
        //PlayerPrefs.DeleteKey("TempIsShot");
        //PlayerPrefs.DeleteKey("TempIsDead");
    }

    //Call when reload the scene or go to next scene
    public void SaveData()
    {
        DTM.OnGameRetry();
        //PlayerPrefs.SetFloat("TempTimer", timer);
        //PlayerPrefs.SetFloat("TempIsShot", isShotOnce ? 1 : 0);
        //PlayerPrefs.SetFloat("TempIsDead", isDeadOnce ? 1 : 0);
    }

    public Dictionary<string, object> OnSave()
    {
        return new Dictionary<string, object>()
        {
            {"timer", timer},
            {"isDead", isDeadOnce },
            {"isShot", isShotOnce }
        };
    }

    public void OnLoad(JToken token)
    {
        timer = (float)token["timer"];
        isDeadOnce = (bool)token["isDead"];
        isShotOnce = (bool)token["isShot"];
    }

    public void ManagerGetData(ref float _time, ref bool _dead, ref bool _shot)
    {
        _time = timer;
        _dead = isDeadOnce;
        _shot = isShotOnce;
    }

    public void ManagerSetData(float _time, bool _dead, bool _shot)
    {
        timer = _time;
        isDeadOnce = _dead;
        isShotOnce = _shot; 
    }
}
