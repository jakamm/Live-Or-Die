/*
 * ObjectiveManager.cs - by ThunderWire Studio
 * ver. 1.2
*/

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using ThunderWire.Utility;

/// <summary>
/// Main Objectives Script
/// </summary>
public class ObjectiveManager : Singleton<ObjectiveManager> 
{
    public List<ObjectiveModel> activeObjectives = new List<ObjectiveModel>();
    public List<ObjectiveModel> objectives = new List<ObjectiveModel>();

    [Header("Main")]
    public ObjectivesScriptable SceneObjectives;

    [Header("Events")]
    public ObjectiveEvent[] objectiveEvents;

    [Header("UI")]
    public GameObject ObjectivesUI;
    public GameObject PushObjectivesUI;
    public GameObject ObjectivePrefab;
    public GameObject PushObjectivePrefab;

    [Header("Timing")]
    public float CompleteTime = 3f;

    [Header("Texts")]
    public string multipleObjectivesText = "You have new objectives, press [Inventory] and check them.";
    public string preCompleteText = "Objective Pre-Completed";
    public string updateText = "Objective Updated";

    [Header("Other")]
    public bool isUppercased;
    public bool allowPreCompleteText = true;

    [Header("Audio")]
    public AudioClip newObjective;
    public AudioClip completeObjective;
    [Range(0,1f)] public float volume;

    private AudioSource soundEffects;
    private bool objShown;

    void Awake()
    {
        if (SceneObjectives)
        {
            foreach (var obj in SceneObjectives.Objectives)
            {
                if (objectiveEvents.Any(x => x.EventID.Equals(obj.eventID)))
                {
                    foreach (var objEvent in objectiveEvents)
                    {
                        if (objEvent.EventID.Equals(obj.eventID))
                        {
                            objectives.Add(new ObjectiveModel(obj.objectiveID, obj.completeCount, obj.objectiveText, objEvent));
                            break;
                        }
                    }
                }
                else
                {
                    objectives.Add(new ObjectiveModel(obj.objectiveID, obj.completeCount, obj.objectiveText, null));
                }
            }
        }
        else
        {
            Debug.LogError("Please Assign Objectives Asset!");
        }

        soundEffects = ScriptManager.Instance.SoundEffects;
        objShown = true;
    }

    void Update()
    {
        if (objShown)
        {
            if (activeObjectives.Count > 0 && activeObjectives.Any(obj => obj.isCompleted == false))
            {
                ObjectivesUI.SetActive(true);

                foreach (var obj in activeObjectives)
                {
                    if (obj.objective != null)
                    {
                        if (obj.objectiveText.Count(ch => ch == '{') > 1 && obj.objectiveText.Count(ch => ch == '}') > 1)
                        {
                            obj.objective.GetComponentInChildren<Text>().text = string.Format(obj.objectiveText, obj.completion, obj.toComplete);
                        }
                    }
                }
            }
            else
            {
                ObjectivesUI.SetActive(false);
            }
        }
        else
        {
            ObjectivesUI.SetActive(false);
        }
    }

    void PlaySound(AudioClip audio)
    {
        if (audio != null)
        {
            soundEffects.clip = audio;
            soundEffects.volume = volume;
            soundEffects.Play();
        }
    }

    public void ShowObjectives(bool show)
    {
        objShown = show;
        ObjectivesUI.SetActive(show);
    }

    public void AddObjective(int objectiveID, float time, bool sound = true)
    {
        if (!CheckObjective(objectiveID))
        {
            ObjectiveModel objModel = objectives.FirstOrDefault(o => o.identifier == objectiveID);

            if (!objModel.isCompleted)
            {
                GameObject obj = Instantiate(ObjectivePrefab, ObjectivesUI.transform);
                obj.transform.GetChild(0).GetComponent<Text>().text = objModel.objectiveText;
                objModel.objective = obj;

                activeObjectives.Add(objModel);

                string text = objModel.objectiveText;

                if (text.Count(ch => ch == '{') > 1 && text.Count(ch => ch == '}') > 1)
                {
                    text = string.Format(text, objModel.completion, objModel.toComplete);
                }

                PushObjectiveText(text, time, isUppercased);

                if (sound) { PlaySound(newObjective); }
            }
        }
    }

    public void AddObjectives(int[] objectivesID, float time, bool sound = true)
    {
        int newObjectives = 0;
        string singleObjective = "";

        foreach (var obj in objectivesID)
        {
            if (!CheckObjective(obj))
            {
                var objModel = objectives[obj];

                if (!objModel.isCompleted)
                {
                    GameObject objObject = Instantiate(ObjectivePrefab, ObjectivesUI.transform);
                    objObject.transform.GetChild(0).GetComponent<Text>().text = objModel.objectiveText;
                    objModel.objective = objObject;
                    activeObjectives.Add(objModel);
                    singleObjective = objModel.objectiveText;
                    newObjectives++;
                }
            }
        }

        if (newObjectives != 0)
        {
            if (newObjectives > 1)
            {
                PushObjectiveText(multipleObjectivesText.GetStringWithInput('[', ']', '[', ']'), time, isUppercased);
            }
            else
            {
                PushObjectiveText(singleObjective, time, isUppercased);
            }

            if (sound) { PlaySound(newObjective); }
        }
    }

    public void AddObjectiveModel(ObjectiveModel model)
    {
        ObjectiveModel objModel = CreateWithEvent(model);

        if (!objModel.isCompleted)
        {
            GameObject objObject = Instantiate(ObjectivePrefab, ObjectivesUI.transform);
            objObject.transform.GetChild(0).GetComponent<Text>().text = objModel.objectiveText;
            objModel.objective = objObject;
            activeObjectives.Add(objModel);
        }
    }

    ObjectiveModel CreateWithEvent(ObjectiveModel model)
    {
        foreach (var obj in SceneObjectives.Objectives)
        {
            if(obj.objectiveID == model.identifier)
            {
                if (objectiveEvents.Any(x => x.EventID.Equals(obj.eventID)))
                {
                    foreach (var objEvent in objectiveEvents)
                    {
                        if (objEvent.EventID.Equals(obj.eventID))
                        {
                            return new ObjectiveModel(model.identifier, model.toComplete, model.isCompleted, objEvent)
                            {
                                objectiveText = obj.objectiveText
                            };
                        }
                    }
                }
                else
                {
                    return new ObjectiveModel(model.identifier, model.toComplete, model.isCompleted, null)
                    {
                        objectiveText = obj.objectiveText
                    };
                }
            }
        }

        return default;
    }

    void PushObjectiveText(string text, float time, bool upper = false)
    {
        GameObject obj = Instantiate(PushObjectivePrefab, PushObjectivesUI.transform);
        obj.GetComponent<Notification>().SetMessage(text, time, upper: upper);
    }

    public void CompleteObjective(int ID, bool sound = true)
    {
        foreach (var obj in activeObjectives)
        {
            if(obj.identifier == ID)
            {
                obj.completion++;

                if(obj.completion >= obj.toComplete)
                {
                    obj.isCompleted = true;
                    if(obj.objectiveEvent != null) obj.objectiveEvent.Event?.Invoke();

                    Destroy(obj.objective);
                    PushObjectiveText(updateText, CompleteTime);
                    if (sound) { PlaySound(completeObjective); }
                }
            }
        }
    }

    public void PreCompleteObjective(int ID)
    {
        foreach (var obj in objectives)
        {
            if (obj.identifier == ID)
            {
                obj.completion++;
                obj.isTouched = true;

                if (obj.completion >= obj.toComplete)
                {
                    obj.isCompleted = true;
                    if (obj.objectiveEvent != null) obj.objectiveEvent.Event?.Invoke();

                    if (allowPreCompleteText)
                    {
                        PushObjectiveText(preCompleteText, CompleteTime);
                        PlaySound(completeObjective);
                    }
                }
            }
        }
    }

    public bool CheckObjective(int ID)
    {
        foreach (var obj in activeObjectives)
        {
            if (obj.identifier == ID)
            {
                if (obj.isCompleted)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool ContainsObjective(int ID)
    {
        foreach (var obj in activeObjectives)
        {
            if (obj.identifier == ID)
            {
                return true;
            }
        }

        return false;
    }

    public int[] ReturnNonExistObjectives(int[] Objectives)
    {
        int[] result = Objectives.Except(activeObjectives.Select(x => x.identifier).ToArray()).ToArray();
        return result;
    }
}

[System.Serializable]
public class ObjectiveEvent
{
    public string EventID;
    public UnityEvent Event;
}

public class ObjectiveModel
{
    public string objectiveText;
    public int identifier;

    public int toComplete;
    public int completion;

    public GameObject objective;
    public bool isCompleted;
    public bool isTouched;

    public ObjectiveEvent objectiveEvent;

    public ObjectiveModel(int id, int count, string text, ObjectiveEvent objEvent)
    {
        identifier = id;
        toComplete = count;
        objectiveText = text;
        objectiveEvent = objEvent;
    }

    public ObjectiveModel(int id, int count, bool completed, ObjectiveEvent objEvent)
    {
        identifier = id;
        toComplete = count;
        isCompleted = completed;
        objectiveText = "";
        objectiveEvent = objEvent;
    }

    public ObjectiveModel() { }
}
