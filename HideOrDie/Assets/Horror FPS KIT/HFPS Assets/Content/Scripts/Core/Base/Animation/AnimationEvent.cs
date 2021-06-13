using UnityEngine;
using UnityEngine.Events;

public class AnimationEvent : MonoBehaviour {

    [System.Serializable]
    public class AnimEvents
    {
        public string EventCallName;
        public UnityEvent CallEvent;
    }

    public AnimEvents[] AnimationEvents;

	public void SendEvent (string CallName) {
        foreach(var ent in AnimationEvents)
        {
            if(ent.EventCallName == CallName)
            {
                ent.CallEvent?.Invoke();
            }
        }
	}
}
