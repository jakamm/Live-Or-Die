/*
 * NPCBodyPart.cs - by ThunderWire Studio
 * ver. 1.0
*/

using UnityEngine;

/// <summary>
/// NPC Damage Caller (Sends Damage Event to Main Health script)
/// </summary>
public class NPCBodyPart : MonoBehaviour {

    [HideInInspector]
    public NPCHealth health;

    public bool isHead;

    public void ApplyDamage(int damage)
    {
        if (isHead)
        {
            health.Damage(health.headshotDamage);
        }
        else
        {
            health.Damage(damage);
        }
    }
}
