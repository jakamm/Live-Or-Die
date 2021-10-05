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
        }
    }

    string Const_DataPath = "/Data/CM.txt";

    // Jack in the Box list
    // Used to save all the boxed if destroyed.
    // True if destroyed, false otherwise. 
    public int JITB_Total = 0;
    public bool[] box_List;

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

    public bool GetIfDestroyed(int index)
    {
        if (index < 0 || index > box_List.Length) Debug.LogError("Out of Bounds");
        return box_List[index];
    }

    public int GetTotal() => box_List.Length;
    public int GetDestroyedTotal()
    {
        int result = 0;
        for (int i = 0; i < box_List.Length; i++)
        {
            if (box_List[i]) result++;
        }
        return result;
    }
    public int GetUndestroyedTotal() => box_List.Length - GetDestroyedTotal();


    // Setup
    private void InitialSetup()
    {
        box_List = new bool[JITB_Total];
    }

    // Set up the data by reading the save
    private void ReadFromSave()
    {
        if(File.Exists(Application.dataPath + Const_DataPath))
        {
            string _output = File.ReadAllText(Application.dataPath + Const_DataPath);
            Debug.Log(" out : " + _output);
            string[] datas = _output.Split('\n');


            #region Jack In The Box
            string[] jitb_datas = datas[0].Split(' ', ',');

            for (int i = 1; i < jitb_datas.Length; i++)
            {
                box_List[i - 1] = jitb_datas[i] == "t";
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
        SaveTheData();
    }

    //public Dictionary<string, object> OnSave()
    //{
    //    Dictionary<string, object> boxList = new Dictionary<string, object>();
    //    for (int i = 0; i < box_List.Length; i++)
    //    {
    //        boxList.Add(i.ToString(), box_List[i]);
    //    }


    //    return new Dictionary<string, object>
    //    {
    //        {"DestroyedBoxes", boxList }
    //    };

    //}

    //public void OnLoad(JToken token)
    //{
    //    Dictionary<string, object> temp = token["DestroyedBoxes"].ToObject<Dictionary<string, object>>();
    //    foreach (var item in temp)
    //    {
    //        box_List[int.Parse(item.Key)] = (bool)item.Value;
    //    }
    //}
}
