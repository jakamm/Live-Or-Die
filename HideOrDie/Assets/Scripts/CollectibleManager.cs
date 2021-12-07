using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CollectibleManager : MonoBehaviour
{
    private static CollectibleManager _i;

    private void Awake()
    {
        if (_i != null)
        {
            Destroy(gameObject);
        }
        else
        {
            _i = this;
            DontDestroyOnLoad(gameObject);

            InitialSetup();
            ReadFromSave();
            calculateInteracted();
        }
    }

    string Const_DataPath = "/Data/CM.txt";

    // Jack in the Box list
    // Used to save all the boxed if destroyed.
    // True if destroyed, false otherwise. 
    int JITB_Total = 0;
    int clip_Total = 0;
    int letter_Total = 0;
    public bool[] box_List;
    public bool[] clip_List;
    public bool[] letter_List;

    public int JITB_Inteacted_Total = 0;
    public int clip_Inteacted_Total = 0;
    public int letter_Inteacted_Total = 0;

    public CollectibleObjectList coList;

    [Tooltip("If checked, whatever in here will override the save in txt file")]
    public bool isOverride;
    [Tooltip("If checked, it will not read data from save, and it will not save the data")]
    public bool isDebug;

    public void Interacted(CollectibleObject.ObjectType type, int iD)
    {
        Debug.Log("Called 1");
        switch (type)
        {
            case CollectibleObject.ObjectType.JITB:
                {
                    if (!box_List[iD])
                    {
                        box_List[iD] = true;
                        calculateInteracted(type);
                        AchievementManager.On_JITB_Open(JITB_Inteacted_Total);
                    }
                }
                break;
            case CollectibleObject.ObjectType.Clip:
                {
                    Debug.Log("Called 2");
                    if (!clip_List[iD])
                    {
                        clip_List[iD] = true;
                        calculateInteracted(type);
                        AchievementManager.On_Tape_Open(clip_Inteacted_Total);
                    }
                }
                break;
            case CollectibleObject.ObjectType.Letter:
                {
                    Debug.Log("Called 2");
                    if (!letter_List[iD])
                    {
                        letter_List[iD] = true;
                        calculateInteracted(type);
                        AchievementManager.On_Letter_Open(letter_Inteacted_Total);
                    }
                }
                break;
            default:
                break;
        }
    }

    public void ResetAll()
    {
        for (int i = 0; i < box_List.Length; i++)
        {
            box_List[i] = false;
        }
    }

    public void DestroyingNewBox(int index)
    {
        if (box_List[index]) Debug.LogError("Box should be destroyed");
        box_List[index] = true;
    }

    public bool GetIfDestroyed(CollectibleObject.ObjectType type, int index)
    {
        switch (type)
        {
            case CollectibleObject.ObjectType.JITB:
                {
                    if (index < 0 || index > box_List.Length) Debug.LogError("Out of Bounds");
                    return box_List[index];
                }
            case CollectibleObject.ObjectType.Clip:
                {
                    if (index < 0 || index > clip_List.Length) Debug.LogError("Out of Bounds");
                    return clip_List[index];
                }
            case CollectibleObject.ObjectType.Letter:
                {
                    if (index < 0 || index > letter_List.Length) Debug.LogError("Out of Bounds");
                    return letter_List[index];
                }
            default:
                break;
        }
        return false;
    }

    public int GetTotal() => box_List.Length;
    public int GetTotal(CollectibleObject.ObjectType type)
    {
        switch (type)
        {
            case CollectibleObject.ObjectType.JITB:
                return JITB_Total;
            case CollectibleObject.ObjectType.Clip:
                return clip_Total;
            case CollectibleObject.ObjectType.Letter:
                return letter_Total;
            case CollectibleObject.ObjectType._MAX:
                break;
            default:
                break;
        }
        return -1;
    }
    //public int GetDestroyedTotal()
    //{
    //    int result = 0;
    //    for (int i = 0; i < box_List.Length; i++)
    //    {
    //        if (box_List[i]) result++;
    //    }
    //    return result;
    //}

    public int GetInteractedTotal(CollectibleObject.ObjectType type)
    {
        switch (type)
        {
            case CollectibleObject.ObjectType.JITB:
                return JITB_Inteacted_Total;
            case CollectibleObject.ObjectType.Clip:
                return clip_Inteacted_Total;
            case CollectibleObject.ObjectType.Letter:
                return letter_Inteacted_Total;
            case CollectibleObject.ObjectType._MAX:
                break;
            default:
                break;
        }
        return -1;
    }

    // Setup
    private void InitialSetup()
    {
        JITB_Total = coList.JITB_Total;
        clip_Total = coList.Clip_Total;
        letter_Total = coList.Letter_Total;

        box_List = new bool[JITB_Total];
        clip_List = new bool[clip_Total];
        letter_List = new bool[letter_Total];
    }

    // Set up the data by reading the save
    private void ReadFromSave()
    {
        if (File.Exists(Application.dataPath + Const_DataPath))
        {
            string _output = File.ReadAllText(Application.dataPath + Const_DataPath);
            Debug.Log(" out : " + _output);
            string[] datas = _output.Split('\n');


            #region Jack In The Box
            string[] jitb_datas = datas[0].Split(' ', ',');
            int dataStart = 1;
            for (int i = dataStart; i < jitb_datas.Length; i++)
            {
                box_List[i - dataStart] = jitb_datas[i] == "t";
            }
            #endregion


            #region Clip
            string[] clip_datas = datas[1].Split(' ', ',');
            for (int i = dataStart; i < clip_datas.Length; i++)
            {
                clip_List[i - dataStart] = clip_datas[i] == "t";
            }
            #endregion

            #region Letter
            string[] letter_datas = datas[2].Split(' ', ',');
            for (int i = dataStart; i < letter_datas.Length; i++)
            {
                letter_List[i - dataStart] = letter_datas[i] == "t";
            }
            #endregion
        }
    }

    // Save the data to the txt
    private void SaveTheData()
    {
        string _output = "";

        //Jack In the Box
        _output += "JITB: ";
        for (int i = 0; i < JITB_Total; i++)
        {
            string addon = box_List[i] ? "t" : "f";
            if (i != JITB_Total - 1) addon += ",";
            _output += addon;
        }
        _output += "\n";

        //clip
        _output += "clip: ";
        for (int i = 0; i < clip_Total; i++)
        {
            string addon = clip_List[i] ? "t" : "f";
            if (i != clip_Total - 1) addon += ",";
            _output += addon;
        }
        _output += "\n";

        //letter
        _output += "letter: ";
        for (int i = 0; i < letter_Total; i++)
        {
            string addon = letter_List[i] ? "t" : "f";
            if (i != letter_Total - 1) addon += ",";
            _output += addon;
        }
        _output += "\n";

        File.WriteAllText(Application.dataPath + Const_DataPath, _output);
    }

    //Clear the save
    private void ClearSave()
    {
        string _output = "";
        File.WriteAllText(Application.dataPath + Const_DataPath, _output);
    }

    private void OnApplicationQuit()
    {
#if UNITY_EDITOR
        if (!isOverride || isDebug) return;
#endif
        SaveTheData();
    }

    void calculateInteracted()
    {
        calculateInteracted(CollectibleObject.ObjectType.Clip);
        calculateInteracted(CollectibleObject.ObjectType.JITB);
        calculateInteracted(CollectibleObject.ObjectType.Letter);
    }

    void calculateInteracted(CollectibleObject.ObjectType type)
    {
        switch (type)
        {
            case CollectibleObject.ObjectType.JITB:
                JITB_Inteacted_Total = 0;
                for (int i = 0; i < box_List.Length; i++)
                {
                    if (box_List[i]) JITB_Inteacted_Total++;
                }
                break;
            case CollectibleObject.ObjectType.Clip:
                clip_Inteacted_Total = 0;
                for (int i = 0; i < clip_List.Length; i++)
                {
                    if (clip_List[i]) clip_Inteacted_Total++;
                }
                break;
            case CollectibleObject.ObjectType.Letter:
                letter_Inteacted_Total = 0;
                for (int i = 0; i < letter_List.Length; i++)
                {
                    if (letter_List[i]) letter_Inteacted_Total++;
                }
                break;
            case CollectibleObject.ObjectType._MAX:
                break;
            default:
                break;
        }
    }
}
