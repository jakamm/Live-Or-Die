using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataTrackerManager : MonoBehaviour
{
    protected DataTracker currentTracker;

    float timer;

    bool isDeadOnce = false;
    bool isShotOnce = false;

    bool dataCarryOver = false;

    public void SetCurrentTracker(DataTracker _dataTracker)
    {
        currentTracker = _dataTracker;
        if(dataCarryOver)
        {
            SetCurrentTrackerData();
            dataCarryOver = false;
        }

    }

    void GetCurrentTrackerData()
    {
        currentTracker.ManagerGetData(ref timer, ref isDeadOnce, ref isShotOnce);
    }

    public void SetCurrentTrackerData()
    {
        currentTracker.ManagerSetData(timer, isDeadOnce, isShotOnce);
    }

    public void OnGameRetry()
    {
        //Get the data first
        GetCurrentTrackerData();
        //When new scene, save the data to the new scene;
        dataCarryOver = true;
    }

    public void OnNewGame()
    {
        dataCarryOver = false;
    }
}
