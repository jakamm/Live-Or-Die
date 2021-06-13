using System.Collections;
using UnityEngine;

public class UIFader {

    public enum FadeOutAfter { Bool, Time }

    public bool fadeCompleted;
    public bool fadedIn;
    public bool fadeOut;

    private Color m_fadeColor = new Color(1, 1, 1, 0);
    private float m_Alpha = 0;
    private float m_fadeSpeed;

    /// <summary>
    /// Start FadeInOut Sequence
    /// </summary>
    public IEnumerator StartFadeIO(Color startColor, float fadeInSpeed, float fadeInMax = 1f, float fadeOutSpeed = 2.5f, float fadeOutTime = 3f, FadeOutAfter fadeOutAfter = FadeOutAfter.Bool)
    {
        Color color = startColor;
        m_fadeColor = startColor;
        m_fadeSpeed = fadeInSpeed;

        fadeCompleted = false;
        fadeOut = false;
        fadedIn = false;

        color.a = 0;
        m_fadeColor.a = 0;

        while (color.a <= fadeInMax)
        {
            color.a += Time.fixedDeltaTime * m_fadeSpeed;
            m_fadeColor = color;
            yield return null;
        }

        color.a = fadeInMax;
        m_fadeColor = color;
        m_fadeSpeed = fadeOutSpeed;
        fadedIn = true;

        if (fadeOutAfter == FadeOutAfter.Bool)
        {
            yield return new WaitUntil(() => fadeOut);
        }
        else
        {
            yield return new WaitForSecondsRealtime(fadeOutTime);
        }

        while (color.a >= 0.1f)
        {
            color.a -= Time.fixedDeltaTime * m_fadeSpeed;
            m_fadeColor = color;
            yield return null;
        }

        color.a = 0;
        m_fadeColor = color;

        yield return new WaitUntil(() => color.a <= 0.05f);

        fadeCompleted = true;

        yield return null;
    }

    /// <summary>
    /// Start FadeInOut Sequence (Alpha)
    /// </summary>
    public IEnumerator StartFadeIO(float startAlpha, float fadeInSpeed, float fadeInMax = 1f, float fadeOutSpeed = 2.5f, float fadeOutTime = 3f, FadeOutAfter fadeOutAfter = FadeOutAfter.Bool)
    {
        float alpha = startAlpha;
        m_Alpha = startAlpha;
        m_fadeSpeed = fadeInSpeed;

        fadeCompleted = false;
        fadeOut = false;
        fadedIn = false;

        alpha = 0;
        m_Alpha = 0;

        while (alpha <= fadeInMax)
        {
            alpha += Time.fixedDeltaTime * m_fadeSpeed;
            m_Alpha = alpha;
            yield return null;
        }

        alpha = fadeInMax;
        m_Alpha = alpha;
        m_fadeSpeed = fadeOutSpeed;
        fadedIn = true;

        if (fadeOutAfter == FadeOutAfter.Bool)
        {
            yield return new WaitUntil(() => fadeOut);
        }
        else
        {
            yield return new WaitForSecondsRealtime(fadeOutTime);
        }

        while (alpha >= 0.1f)
        {
            alpha -= Time.fixedDeltaTime * m_fadeSpeed;
            m_Alpha = alpha;
            yield return null;
        }

        alpha = 0;
        m_Alpha = alpha;

        yield return new WaitUntil(() => alpha <= 0.05f);

        fadeCompleted = true;

        yield return null;
    }

    /// <summary>
    /// Start FadeOut Sequence
    /// </summary>
    public IEnumerator StartFadeOut(Color startColor, float fadeOutSpeed)
    {
        Color color = startColor;
        m_fadeColor = startColor;
        m_fadeSpeed = fadeOutSpeed;

        fadeCompleted = false;
        fadedIn = true;

        while (color.a >= 0.1f)
        {
            color.a -= Time.fixedDeltaTime * m_fadeSpeed;
            m_fadeColor = color;
            yield return null;
        }

        color.a = 0;
        m_fadeColor = color;

        yield return new WaitUntil(() => color.a <= 0.05f);

        fadeCompleted = true;

        yield return null;
    }

    /// <summary>
    /// Start FadeOut Sequence (Alpha)
    /// </summary>
    public IEnumerator StartFadeOut(float startAlpha, float fadeOutSpeed)
    {
        float alpha = startAlpha;
        m_Alpha = startAlpha;
        m_fadeSpeed = fadeOutSpeed;

        fadeCompleted = false;
        fadedIn = true;

        while (alpha >= 0.1f)
        {
            alpha -= Time.fixedDeltaTime * m_fadeSpeed;
            m_Alpha = alpha;
            yield return null;
        }

        alpha = 0;
        m_Alpha = alpha;

        yield return new WaitUntil(() => alpha <= 0.05f);

        fadeCompleted = true;

        yield return null;
    }

    public void SetColor(Color color)
    {
        m_fadeColor = color;
    }

    public void SetFadeSpeed(float speed)
    {
        m_fadeSpeed = speed;
    }

    /// <summary>
    /// Get Fader Result Color
    /// </summary>
    public Color GetFadeColor()
    {
        return m_fadeColor;
    }

    /// <summary>
    /// Get Fader Result Alpha
    /// </summary>
    public float GetFadeAlpha()
    {
        return m_Alpha;
    }
}
