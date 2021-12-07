using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public class AchievementManager : MonoBehaviour
{
    #region Steam Define
    //Steam API name
    static string[] AchievementAPIName = new string[] 
    {
      "ACHI_WHATS_THIS_THING", //
      "ACHI_I_HATE_CLOWNS", //
      "ACHI_I_CANT_READ", //
      "ACHI_THIS_IS_KIND_OF_FUN", //
      "ACHI_MATH_IS_AWESOME", //
      "ACHI_I_REALLY_HATE_CLOWNS",  //
      "ACHI_I_CAN_READ_NOW", //
      "ACHI_I_AM_THE_BEST", //
      "ACHI_I_THOUGHT_I_WAS_OUT", //
      "ACHI_SPEEDY", //
      "ACHI_THATS_LOUD", //
      "ACHI_INVINCIBLE", //
      "ACHI_THE_GAMES_MASTER" //
    };

    static string[] StatAPIName = new string[]
    {
        "STAT_JITB",
        "STAT_ZOMBIE",
        "STAT_LETTER",
        "STAT_TAPE"
    };

    enum eAchievement
    {
        WHATS_THIS_THING,
        I_HATE_CLOWNS,
        I_CANT_READ,
        THIS_IS_KIND_OF_FUN,
        MATH_IS_AWESOME,
        I_REALLY_HATE_CLOWNS,
        I_CAN_READ_NOW,
        I_AM_THE_BEST,
        I_THOUGHT_I_WAS_OUT,
        SPEEDY,
        THATS_LOUD,
        INVINCIBLE,
        THE_GAMES_MASTER
    }

    enum eStats
    {
        STAT_JITB,
        STAT_ZOMBIE,
        STAT_LETTER,
        STAT_TAPE,
        _MAX
    }


    #endregion
    static float[] stats = new float[(int)eStats._MAX];
    static bool IsNeedPush = false; // If its true, it means some data hasn't been pushed yet. 

    static int JITB_TOTAL;
    static int CLIP_TOTAL;
    static int LETTER_TOTAL;

    //
    // Public Functions
    // 

    public static void On_JITB_Open(int interactedTotal)
    {
        stats[(int)eStats.STAT_JITB] = interactedTotal;
        SteamUserStats.SetStat(StatAPIName[(int)eStats.STAT_JITB], stats[(int)eStats.STAT_JITB]);

        //If more than needed, unlock achievement
        if(stats[(int)eStats.STAT_JITB] >= 3)
        {
            Debug.Log("I_HATE_CLOWNS");
            SteamUserStats.SetAchievement(AchievementAPIName[(int)eAchievement.I_HATE_CLOWNS]);
            SteamUserStats.StoreStats();
        }

        if(stats[(int)eStats.STAT_JITB] == JITB_TOTAL)
        {
            Debug.Log("I_REALLY_HATE_CLOWNS");
            SteamUserStats.SetAchievement(AchievementAPIName[(int)eAchievement.I_REALLY_HATE_CLOWNS]);
            if (!CheckIfAllFinish()) SteamUserStats.StoreStats();
        }       
    }

    public static void On_Tape_Open(int interactedTotal)
    {
        stats[(int)eStats.STAT_TAPE] = interactedTotal;
        SteamUserStats.SetStat(StatAPIName[(int)eStats.STAT_TAPE], stats[(int)eStats.STAT_TAPE]);

        //If more than needed, unlock achievement
        if (stats[(int)eStats.STAT_TAPE] >= 1)
        {
            Debug.Log("WHATS_THIS_THING");
            SteamUserStats.SetAchievement(AchievementAPIName[(int)eAchievement.WHATS_THIS_THING]);
            if (!CheckIfAllFinish()) SteamUserStats.StoreStats();
        }
    }

    public static void On_Letter_Open(int interactedTotal)
    {
        stats[(int)eStats.STAT_LETTER] = interactedTotal;
        SteamUserStats.SetStat(StatAPIName[(int)eStats.STAT_LETTER], stats[(int)eStats.STAT_LETTER]);

        //If more than needed, unlock achievement
        if (stats[(int)eStats.STAT_LETTER] >= 1)
        {
            Debug.Log("I_CANT_READ");
            SteamUserStats.SetAchievement(AchievementAPIName[(int)eAchievement.I_CANT_READ]);
            SteamUserStats.StoreStats();
        }

        if (stats[(int)eStats.STAT_LETTER] == LETTER_TOTAL)
        {
            Debug.Log("I_CAN_READ_NOW");
            SteamUserStats.SetAchievement(AchievementAPIName[(int)eAchievement.I_CAN_READ_NOW]);
            if (!CheckIfAllFinish()) SteamUserStats.StoreStats();
        }
    }

    public static void On_Zombie_Kill()
    {
        stats[(int)eStats.STAT_ZOMBIE]++;
        SteamUserStats.SetStat(StatAPIName[(int)eStats.STAT_ZOMBIE], stats[(int)eStats.STAT_ZOMBIE]);

        //If more than needed, unlock achievement
        if (stats[(int)eStats.STAT_ZOMBIE] == 10)
        {
            Debug.Log("THIS_IS_KIND_OF_FUN");
            SteamUserStats.SetAchievement(AchievementAPIName[(int)eAchievement.THIS_IS_KIND_OF_FUN]);
            if (!CheckIfAllFinish()) SteamUserStats.StoreStats();
        }
    }

    public static void On_Dead_Body_Complete()
    {
        Debug.Log("MATH_IS_AWESOME");
        SteamUserStats.SetAchievement(AchievementAPIName[(int)eAchievement.MATH_IS_AWESOME]);
        if (!CheckIfAllFinish()) SteamUserStats.StoreStats();
    }

    public static void On_Game_Finish(float usedTime, bool isInjured, bool isShoted)
    {
        Debug.Log("I_AM_THE_BEST");
        SteamUserStats.SetAchievement(AchievementAPIName[(int)eAchievement.I_AM_THE_BEST]);
        if (!CheckIfAllFinish()) SteamUserStats.StoreStats();

        float oneHour = 60 * 60;
        if( usedTime < oneHour )
        {
            Debug.Log("SPEEDY");
            SteamUserStats.SetAchievement(AchievementAPIName[(int)eAchievement.SPEEDY]);
            if (!CheckIfAllFinish()) SteamUserStats.StoreStats();
        }
        if(!isShoted)
        {
            Debug.Log("THATS_LOUD");
            SteamUserStats.SetAchievement(AchievementAPIName[(int)eAchievement.THATS_LOUD]);
            if (!CheckIfAllFinish()) SteamUserStats.StoreStats();
        }
        if(!isInjured)
        {
            Debug.Log("INVINCIBLE");
            SteamUserStats.SetAchievement(AchievementAPIName[(int)eAchievement.INVINCIBLE]);
            if (!CheckIfAllFinish()) SteamUserStats.StoreStats();
        }
    }

    public static void On_Fail_Escape()
    {
        Debug.Log("I_THOUGHT_I_WAS_OUT");
        SteamUserStats.SetAchievement(AchievementAPIName[(int)eAchievement.I_THOUGHT_I_WAS_OUT]);
        if(!CheckIfAllFinish()) SteamUserStats.StoreStats();
    }

    //
    // Private Functions
    //

    static bool IsSteamInit()
    {
        if (!SteamManager.Initialized)
        {
            Debug.Log("SteamManager is not initialized!");
            return false;
        }
        return true;
    }

    void Start()
    {
        //Find collectible Manager to get the datas.
        CollectibleManager cm = GetComponent<CollectibleManager>();

        if(!cm)
        {
            Debug.Log("Need CM here!");
            return;
        }
        JITB_TOTAL = cm.coList.JITB_Total;
        CLIP_TOTAL = cm.coList.Clip_Total;
        LETTER_TOTAL = cm.coList.Letter_Total;


        bool isInit = SteamManager.Initialized;
        if (!isInit)
        {
            Debug.Log("Check Steam Manager");
            return;
        }
        Debug.Log("Done");
        if (!SteamUserStats.RequestCurrentStats()) return;

        //Sync the stats with the server
        for (int i = 0; i < (int)eStats._MAX; i++)
        {
            SteamUserStats.GetStat(StatAPIName[i], out stats[i]);
        }


    }
    /// <summary>
    /// Call to check if player finish all achievement
    /// </summary>
    /// <returns> Return Bool </returns>
    static bool CheckIfAllFinish()
    {
        foreach (var api in AchievementAPIName)
        {
            bool isFinished = false;
            SteamUserStats.GetAchievement(api, out isFinished);
            if (!isFinished) return false;
        }

        Debug.Log("THE_GAMES_MASTER");
        SteamUserStats.SetAchievement(AchievementAPIName[(int)eAchievement.THE_GAMES_MASTER]);
        SteamUserStats.StoreStats();
        return true;
    }
}
