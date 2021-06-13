using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ThunderWire.Utility;

public class FloatingIcon : MonoBehaviour
{
    [HideInInspector]
    public FloatingIconManager iconManager;

    [HideInInspector]
    public bool isVisible = true;

    public GameObject FollowObject;

    private UIFader fader = new UIFader();
    private Image icon;

    private float smooth;
    private bool outOfDistance = false;
    private bool fadeOut = true;
    private Vector3 velocity = Vector3.zero;

    void OnEnable()
    {
        icon = GetComponent<Image>();
    }

    void Update()
    {
        smooth = iconManager.followSmooth;

        if (!FollowObject || !icon) return;

        if (isVisible)
        {
            if (!outOfDistance)
            {
                if (iconManager.IsVisibleFrustum(FollowObject))
                {
                    if (!fadeOut)
                    {
                        icon.enabled = true;
                    }
                    else
                    {
                        StartCoroutine(fader.StartFadeIO(icon.color.a, 2.5f, fadeOutSpeed: 4f, fadeOutAfter: UIFader.FadeOutAfter.Bool));
                        fadeOut = false;
                    }
                }
                else
                {
                    fader.fadeOut = true;
                    StartCoroutine(FadeOut());
                }
            }

            if (!fader.fadeCompleted)
            {
                Color color = icon.color;
                color.a = fader.GetFadeAlpha();
                icon.color = color;
            }

            Vector3 screenPos = Tools.MainCamera().WorldToScreenPoint(FollowObject.transform.position);
            icon.transform.position = Vector3.SmoothDamp(icon.transform.position, screenPos, ref velocity, Time.deltaTime * smooth);
        }
        else
        {
            icon.enabled = false;
        }
    }

    public void SetIconVisible(bool state)
    {
        isVisible = state;
    }

    public void OutOfDIstance(bool isOut)
    {
        outOfDistance = isOut;

        if (isOut)
        {
            fader.fadeOut = true;
            StartCoroutine(FadeOut());
        }
    }

    IEnumerator FadeOut()
    {
        yield return new WaitUntil(() => fader.fadeCompleted);
        icon.enabled = false;
        fadeOut = true;
    }
}
