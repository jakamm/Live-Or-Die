/*
 * CutsceneManager.cs - by ThunderWire Studio
 * Version 1.1 Beta (May occur bugs sometimes)
 * 
 * Bugs please report here: thunderwiregames@gmail.com
*/

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace ThunderWire.Cutscenes
{
    /// <summary>
    /// Provides methods for Cutscenes Management
    /// </summary>
    public class CutsceneManager : Singleton<CutsceneManager>
    {
        public enum CutsceneTime { Duration, Manual }

        private ScriptManager scriptManager;

        [Serializable]
        public class Cutscene
        {
            public string Name;
            public CutsceneTime Time = CutsceneTime.Manual;
            public PlayableDirector Director;
        }

        public List<Cutscene> Cutscenes = new List<Cutscene>();

        [Header("Other")]
        public UIFadePanel fadePanel;

        [HideInInspector]
        public bool cutsceneRunning;

        private int queueIndex = 0;
        private bool skipCurrent = false;

        private Cutscene current = null;
        private Cutscene temp = null;
        private List<Cutscene> cutsceneQueue = new List<Cutscene>();
        private AudioListener mainListener;

        void Awake()
        {
            scriptManager = ScriptManager.Instance;
            mainListener = scriptManager.GetComponentInChildren<AudioListener>();
        }

        /// <summary>
        /// Add next Cutscene to Cutscenes Queue
        /// </summary>
        public void AddCutsceneQueue(string Name)
        {
            foreach (var cutscene in Cutscenes)
            {
                if (cutscene.Name == Name)
                {
                    cutsceneQueue.Add(cutscene);
                    break;
                }
            }

            if (current == null && cutsceneQueue.Count > 0)
            {
                PlayOrAddCutscene(Name);
            }
        }

        /// <summary>
        /// Play or Add Cutscene to Queue
        /// </summary>
        public void PlayOrAddCutscene(string Name)
        {
            if (!Cutscenes.Any(x => x.Name.Equals(Name)))
            {
                Debug.LogError($"[Cutscene Error] Cutscene {Name} does not exist!");
                return;
            }

            if (current == null)
            {
                foreach (var cutscene in Cutscenes)
                {
                    if (cutscene.Name == Name)
                    {
                        current = cutscene;
                        break;
                    }
                }

                scriptManager.m_GameManager.LockPlayerControls(false, false, false);

                if (fadePanel)
                {
                    fadePanel.FadeIn();
                    StartCoroutine(PlayQueuedCutscenesFade());
                }
                else
                {
                    StartCoroutine(PlayQueuedCutscenes());
                }
            }
            else
            {
                AddCutsceneQueue(Name);
            }
        }

        /// <summary>
        /// Skip running Cutscene
        /// </summary>
        public void SkipCutscene()
        {
            if(current != null)
            {
                skipCurrent = true;
            }
        }

        /// <summary>
        /// Abort Cutscene Sequence
        /// </summary>
        public void AbortCutscenes()
        {
            StopAllCoroutines();
            ClearCurrentQueue();
            cutsceneRunning = false;
            skipCurrent = false;
            current = null;
            queueIndex = 0;
        }

        /// <summary>
        /// Clear Cutscenes Queue
        /// </summary>
        public void ClearCurrentQueue()
        {
            if (cutsceneQueue.Count > 0)
            {
                cutsceneQueue.Clear();
            }
        }

        IEnumerator PlayQueuedCutscenes()
        {
            FreezePlayer(true);

            while (current != null)
            {
                current.Director.Play();

                yield return new WaitForEndOfFrame();

                if (current.Time == CutsceneTime.Duration)
                {
                    yield return new WaitForSeconds((float)current.Director.duration);
                }
                else
                {
                    yield return new WaitUntil(() => skipCurrent);
                }

                if (queueIndex < cutsceneQueue.Count)
                {
                    current.Director.Stop();
                    skipCurrent = false;
                    current = cutsceneQueue[queueIndex];
                    queueIndex++;

                    fadePanel.FadeIn();
                    yield return new WaitForEndOfFrame();
                    yield return new WaitUntil(() => fadePanel.IsFadedIn);
                    fadePanel.FadeOutManually();
                    continue;
                }
                else
                {
                    current.Director.Stop();
                    ClearCurrentQueue();
                    queueIndex = 0;
                    temp = current;
                    current = null;
                    break;
                }
            }

            if (fadePanel)
            {
                fadePanel.FadeIn();
                StartCoroutine(CutsceneEnd());
            }
        }

        IEnumerator PlayQueuedCutscenesFade()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitUntil(() => fadePanel.IsFadedIn);
            StartCoroutine(PlayQueuedCutscenes());
            fadePanel.FadeOutManually();
        }

        IEnumerator CutsceneEnd()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitUntil(() => fadePanel.IsFadedIn);
            SwitchCameras();
            fadePanel.FadeOutManually();
        }

        void SwitchCameras()
        {
            temp.Director.transform.GetComponentsInChildren<UnityEngine.Camera>().ToList().ForEach(x => x.gameObject.SetActive(false));
            temp = null;
            FreezePlayer(false);
        }

        void FreezePlayer(bool state)
        {
            if (state)
            {
                mainListener.enabled = false;
                scriptManager.m_GameManager.MainGamePanel.SetActive(false);
                scriptManager.MainCamera.gameObject.SetActive(false);
                scriptManager.ArmsCamera.gameObject.SetActive(false);
            }
            else
            {
                mainListener.enabled = true;
                scriptManager.m_GameManager.MainGamePanel.SetActive(true);
                scriptManager.m_GameManager.LockPlayerControls(true, true, false);
                scriptManager.MainCamera.gameObject.SetActive(true);
                scriptManager.ArmsCamera.gameObject.SetActive(true);
            }

            cutsceneRunning = state;
        }
    }
}
