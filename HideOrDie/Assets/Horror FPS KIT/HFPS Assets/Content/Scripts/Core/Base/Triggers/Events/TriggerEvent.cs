using UnityEngine;
using UnityEngine.Events;

public class TriggerEvent : MonoBehaviour {

    public enum Modes { Once, MoreTimes }

    public Modes Mode = Modes.Once;
    [Space(5)]
    public UnityEvent triggerEvent;

    [SaveableField]
    public bool isPlayed;

    public void Trigger()
    {
        if (!isPlayed)
        {
            triggerEvent.Invoke();

            if (Mode == Modes.Once)
            {
                isPlayed = true;
            }
        }
    }

    void Update()
    {
        if (GetComponent<Collider>() && !GetComponent<Collider>().enabled)
        {
            isPlayed = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && !isPlayed)
        {
            triggerEvent.Invoke();
            isPlayed = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player" && isPlayed && Mode == Modes.MoreTimes)
        {
            isPlayed = false;
        }
    }
}
