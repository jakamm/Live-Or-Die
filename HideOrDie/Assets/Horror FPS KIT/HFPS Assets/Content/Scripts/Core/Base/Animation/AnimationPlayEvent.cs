using UnityEngine;
using System.Collections;

public enum wrapModes { Default, Once, Loop, PingPong, ClampForever }

public class AnimationPlayEvent : MonoBehaviour {

    public Animation m_animation;
    public string animationName;
    public wrapModes wrapMode = wrapModes.Default;

    [Header("Destroy")]
    public bool destroyAfterPlay;
    public bool sendMessageDestroy;
    public GameObject sendMessageObject;

    [HideInInspector, SaveableField]
    public bool isPlayed = false;

    public void PlayAnimation()
    {
        if (!isPlayed)
        {
            m_animation[animationName].wrapMode = (WrapMode)System.Enum.Parse(typeof(WrapMode), wrapMode.ToString());
            m_animation.Play(animationName);

            if (destroyAfterPlay)
            {
                StartCoroutine(DestroyAfter());
            }

            isPlayed = true;
        }
    }

    IEnumerator DestroyAfter()
    {
        yield return new WaitUntil(() => !m_animation.isPlaying);

        if (!sendMessageDestroy)
        {
            Destroy(gameObject);
        }
        else
        {
            sendMessageObject.SendMessage("DestroyEvent", SendMessageOptions.DontRequireReceiver);
        }
    }
}
