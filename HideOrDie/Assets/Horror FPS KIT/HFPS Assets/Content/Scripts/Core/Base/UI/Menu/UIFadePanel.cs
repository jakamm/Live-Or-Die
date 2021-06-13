using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIFadePanel : MonoBehaviour {

    readonly UIFader fader = new UIFader();

    public Image FadeImage;
    public float FadeSpeed;

    public bool startFadeOut;

    [HideInInspector] public bool IsFading { 
        get
        {
            return fader.fadeCompleted;
        }
    }

    [HideInInspector]
    public bool IsFadedIn
    {
        get
        {
            return fader.fadedIn;
        }
    }

    [HideInInspector]
    public bool IsFadedOut
    {
        get
        {
            return fader.fadeOut;
        }
    }

    void Awake()
    {
        if (FadeImage)
        {
            FadeImage.gameObject.SetActive(true);
        }
    }

    void Start()
    {
        if (startFadeOut)
        {
            FadeImage.gameObject.SetActive(true);
            StartCoroutine(fader.StartFadeOut(FadeImage.color, FadeSpeed));
        }
    }

    public void FadeOut()
    {
        FadeImage.gameObject.SetActive(true);
        StartCoroutine(fader.StartFadeOut(FadeImage.color, FadeSpeed));
    }

    public void FadeOutManually()
    {
        fader.fadeOut = true;
    }

    public void FadeOutAndDestroy(GameObject obj)
    {
        StartCoroutine(fader.StartFadeOut(FadeImage.color, FadeSpeed));
        StartCoroutine(WaitDestroy(obj));
    }

    IEnumerator WaitDestroy(GameObject @object)
    {
        yield return new WaitUntil(() => fader.fadeCompleted);
        Destroy(@object);
    }

    public void FadeIn()
    {
        FadeImage.gameObject.SetActive(true);
        StartCoroutine(fader.StartFadeIO(FadeImage.color, FadeSpeed, fadeOutAfter: UIFader.FadeOutAfter.Bool));
    }

    public void FadeBlink(float time)
    {
        FadeImage.gameObject.SetActive(true);
        StartCoroutine(fader.StartFadeIO(FadeImage.color, FadeSpeed, fadeOutTime: time, fadeOutAfter: UIFader.FadeOutAfter.Time));
    }

    void Update()
    {
        if (fader.fadeCompleted)
        {
            FadeImage.raycastTarget = false;
            FadeImage.gameObject.SetActive(false);
        }

        FadeImage.color = fader.GetFadeColor();
    }
}
