using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AchievementTriggerCaller : MonoBehaviour
{
    public void OnDeadBodyFinished()
    {
        AchievementManager.On_Dead_Body_Complete();
    }

    public void OnFailEscape()
    {
        AchievementManager.On_Fail_Escape();
    }

    public void OnGameFinished()
    {
        DataTracker dataTracker = FindObjectOfType<DataTracker>();

        AchievementManager.On_Game_Finish(dataTracker.getTime(), 
                                        dataTracker.getIsDead(), 
                                        dataTracker.getIsShot());
    }

}
