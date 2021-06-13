/*
 * ScriptManager.cs - by ThunderWire Studio
 * ver. 1.0
*/

using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// Manages HFPS Scripts and Variables
/// </summary>
public class ScriptManager : Singleton<ScriptManager> {

    [Header("Main Scripts")]
    public HFPS_GameManager m_GameManager;

    [Header("Cameras")]
    public Camera MainCamera;
    public Camera ArmsCamera;

    [Header("Post-Processing")]
    public PostProcessVolume MainPostProcess;
    public PostProcessVolume ArmsPostProcess;

    [Header("Other")]
    public AudioSource AmbienceSource;
    public AudioSource SoundEffects;

    [HideInInspector] public bool ScriptEnabledGlobal;
    [HideInInspector] public bool ScriptGlobalState;

    [HideInInspector] public bool IsExamineRaycast;
    [HideInInspector] public bool IsGrabRaycast;

    void Start()
    {
        ScriptEnabledGlobal = true;
        ScriptGlobalState = true;
    }

    public T GetScript<T>() where T : MonoBehaviour
    {
        return (T)ReturnScript(typeof(T));
    }

    private object ReturnScript(Type type)
    {
        Component component = GetComponentInChildren(type, true);

        if (component != null)
        {
            return GetComponentInChildren(type, true);
        }

        return null;
    }
}
