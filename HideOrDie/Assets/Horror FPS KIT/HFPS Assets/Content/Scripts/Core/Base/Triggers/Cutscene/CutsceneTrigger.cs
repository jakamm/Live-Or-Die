using UnityEngine;

namespace ThunderWire.Cutscenes
{
    /// <summary>
    /// Provides Cutscene Trigger Methods
    /// </summary>
    public class CutsceneTrigger : MonoBehaviour
    {
        private CutsceneManager manager;

        public string[] cutscenesQueue;

        [HideInInspector, SaveableField]
        public bool isTriggered;

        void Awake()
        {
            manager = CutsceneManager.Instance;
        }

        public void SkipCutscene()
        {
            manager.SkipCutscene();
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !isTriggered)
            {
                if (cutscenesQueue.Length > 0)
                {
                    foreach (var item in cutscenesQueue)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            manager.PlayOrAddCutscene(item);
                        }
                    }
                }
                else
                {
                    Debug.LogError("[Cutscene Trigger] Cutscenes Queue could not be empty!");
                }

                isTriggered = true;
            }
        }
    }
}
