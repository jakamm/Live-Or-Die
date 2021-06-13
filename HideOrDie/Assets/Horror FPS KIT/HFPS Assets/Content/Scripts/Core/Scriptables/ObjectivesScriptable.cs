using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectivesScriptable : ScriptableObject {

    public List<Objective> Objectives = new List<Objective>();

    [Serializable]
    public class Objective
    {
        public string shortName;
        public string eventID;
        [Multiline]
        public string objectiveText;
        public int completeCount;
        [ReadOnly]
        public int objectiveID;
    }

    public void Reseed()
    {
        foreach (Objective obj in Objectives)
        {
            obj.objectiveID = Objectives.IndexOf(obj);
        }
    }
}
