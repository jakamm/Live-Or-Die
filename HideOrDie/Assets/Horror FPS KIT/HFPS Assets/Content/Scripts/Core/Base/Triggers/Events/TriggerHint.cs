using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using ThunderWire.Utility;
using ThunderWire.CrossPlatform.Input;

public class TriggerHint : MonoBehaviour, ISaveable {

    public string Hint;
    public float TimeShow;
    public float ShowAfter = 0f;

    [Header("Extra")]
    public AudioClip HintSound;

    private float timer;
    private bool timedShow;
    private bool isShown;

    private CrossPlatformInput crossPlatformInput;
    private HFPS_GameManager gameManager;
    private AudioSource soundEffects;

    void Start()
    {
        crossPlatformInput = CrossPlatformInput.Instance;
        gameManager = HFPS_GameManager.Instance;
        soundEffects = GetComponent<AudioSource>() ? GetComponent<AudioSource>() : null;

        if (HintSound && !soundEffects)
        {
            Debug.LogError("[TriggerHint] HintSound require an a AudioSource Component!");
        }

        if (soundEffects)
        {
            soundEffects.spatialBlend = 0f;
        }
    }

    public void SetTrigger(bool state)
    {
        isShown = state;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isShown && gameManager && crossPlatformInput.inputsLoaded)
        {
            char[] hintChars = Hint.ToCharArray();

            if (hintChars.Contains('{') && hintChars.Contains('}'))
            {
                string key = crossPlatformInput.ControlOf(Hint.GetBetween('{', '}')).Control;
                Hint = Hint.ReplacePart('{', '}', key);
            }

            if (ShowAfter > 0)
            {
                timedShow = true;
            }
            else
            {
                if (!string.IsNullOrEmpty(Hint)) { gameManager.ShowHint(Hint, TimeShow); }

                if (HintSound && soundEffects)
                {
                    soundEffects.clip = HintSound;
                    soundEffects.Play();
                }

                isShown = true;
            }
        }
    }

    void Update()
    {
        if (timedShow && !isShown)
        {
            timer += Time.unscaledDeltaTime;

            if(timer >= ShowAfter)
            {
                if (!string.IsNullOrEmpty(Hint)) { gameManager.ShowHint(Hint, TimeShow); }
                if (HintSound && soundEffects)
                {
                    soundEffects.clip = HintSound;
                    soundEffects.Play();
                }
                isShown = true;
            }
        }
    }

    void OnDisable()
    {
        isShown = true;
    }

    public Dictionary<string, object> OnSave()
    {
        return new Dictionary<string, object>()
        {
            { "enabled", enabled },
            { "isShown", isShown }
        };
    }

    public void OnLoad(JToken token)
    {
        isShown = token["isShown"].ToObject<bool>();
        enabled = token["enabled"].ToObject<bool>();
    }
}
