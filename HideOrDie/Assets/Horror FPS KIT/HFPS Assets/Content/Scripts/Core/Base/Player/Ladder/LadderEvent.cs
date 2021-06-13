using System.Linq;
using UnityEngine;

public class LadderEvent : MonoBehaviour
{
    public LadderTrigger ladder;
    public bool isDownTrigger;

    [HideInInspector]
    public bool blockTrigger;

    private bool isTriggered;

    void FixedUpdate()
    {
        Vector3 size = transform.TransformVector(transform.localScale / 2);
        size.x = Mathf.Abs(size.x);
        size.y = Mathf.Abs(size.y);
        size.z = Mathf.Abs(size.z);

        Collider[] touch = Physics.OverlapBox(transform.position, size, Quaternion.identity);

        if (!blockTrigger)
        {
            if (touch.Any(x => x.CompareTag("Player")))
            {
                if (!isTriggered)
                {
                    ladder.OnClimbFinish(!isDownTrigger);
                    isTriggered = true;
                }
            }
            else
            {
                isTriggered = false;
            }
        }
        else
        {
            isTriggered = true;
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 size = transform.TransformVector(transform.localScale / 2);
        size.x = Mathf.Abs(size.x);
        size.y = Mathf.Abs(size.y);
        size.z = Mathf.Abs(size.z);

        Color color = Color.green;
        color.a = 0.25f;

        Gizmos.color = color;
        Gizmos.DrawCube(transform.position, size);
    }
}
