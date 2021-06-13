/*
 * VCRPlayer.cs - by ThunderWire Studio
 * version 1.0
*/

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using Newtonsoft.Json.Linq;

public class VCRPlayer : MonoBehaviour, IItemSelect, ISaveable, IPauseEvent
{
    public enum TextOnDisplay { StandBy, Play, Wait, Stop, Eject, Rewind }

    private Animation Anim;

    [Header("Main")]
    public List<VideoTape> VideoTapes = new List<VideoTape>();

    [Space(7)]
    public int InvVideoTapeID;
    public VCRTV TV;
    public Collider InsertTrigger;
    public UIObjectInfo PlayStopRewindTrigger;
    public AudioSource VCRAudio;

    [Header("Video Tape")]
    public InteractiveItem VideoTapeObj;
    public Texture DefaultTapeTex;

    [Header("Display Text")]
    public TextMesh StateText;
    public TextMesh TimeText;

    [Header("Lights")]
    public Light PowerDiodeLight;
    public Light DisplayLight;
    public MeshRenderer PowerDiode;

    [Space(7)]
    public Color PowerOffColor = Color.red;
    public Color PowerOnColor = Color.green;

    [Header("Properties")]
    public float RewindTimeWait = 0.06f;
    public float RewindAfterTime = 1f;

    [Header("Animations")]
    public string InsertAnim;
    public string EjectAnim;
    public string TakeAnim;

    [Header("Sounds")]
    public AudioClip LoadTapeSound;
    public AudioClip StartPlaySound;
    public AudioClip RewindTapeSound;
    public AudioClip StopSound;
    public AudioClip EjectSound;

    [Space(10)]
    public bool TurnOn;

    [HideInInspector]
    public bool isOn;

    [HideInInspector]
    public bool isPlaying;

    private VideoTape tape;
    private int? tapeID;
    private string tapeCID;
    private string tapeDescription;
    private string tapeTexPath;

    private double clipDuration;
    private int secondsDiff;
    private int duration;
    private int seconds;
    private int minutes;

    private string min;
    private string sec;

    private string txt_state;
    private string txt_time;

    private bool isEjecting;
    private bool isRewinding;
    private bool isStopped;
    private bool isTapeEnded;
    private bool isStarted;

    private bool canInsert;
    private bool canEject;

    void Start()
    {
        if (TV)
        {
            TV.VCR = this;
        }
        else
        {
            Debug.LogError("[VHS Player] Please assign VHS TV.");
            return;
        }

        if (GetComponent<Animation>())
        {
            Anim = GetComponent<Animation>();
        }
        else
        {
            Debug.LogError("[VHS Player] Could not find Animation component!");
            return;
        }

        if (TurnOn)
        {
            PowerOnOff();
        }

        canInsert = true;
        canEject = false;
    }

    public void UpdateClipTime(double time)
    {
        if (isStarted && time < duration) return;

        if (time <= clipDuration && !isStopped)
        {
            duration = (int)time;
            seconds = duration - secondsDiff;

            if (seconds >= 60)
            {
                secondsDiff += 60;
                minutes++;
            }

            min = minutes.ToString("D2");
            sec = seconds.ToString("D2");

            txt_time = string.Format("{0}:{1}", min, sec);
            TimeText.text = txt_time;
        }
    }

    private void DisplayText(TextOnDisplay text, bool resetTimer, bool forceOn = false)
    {
        switch (text)
        {
            case TextOnDisplay.StandBy:
                txt_state = "STAND BY";
                break;
            case TextOnDisplay.Play:
                txt_state = "> PLAY";
                break;
            case TextOnDisplay.Wait:
                txt_state = "Waiting";
                break;
            case TextOnDisplay.Stop:
                txt_state = "Stop";
                break;
            case TextOnDisplay.Eject:
                txt_state = "Eject";
                break;
            case TextOnDisplay.Rewind:
                txt_state = "Rewind";
                break;
        }

        if (resetTimer)
        {
            txt_time = "00:00";
        }

        if (isOn || forceOn)
        {
            StateText.text = txt_state;
            TimeText.text = txt_time;
            StateText.gameObject.SetActive(true);
            TimeText.gameObject.SetActive(true);
        }
    }

    private void SetDiodeColor(bool powerOn)
    {
        if (powerOn)
        {
            PowerDiode.material.SetColor("_EmissionColor", PowerOnColor);

            if (PowerDiodeLight)
            {
                PowerDiodeLight.color = PowerOnColor;
            }
        }
        else
        {
            PowerDiode.material.SetColor("_EmissionColor", PowerOffColor);

            if (PowerDiodeLight)
            {
                PowerDiodeLight.color = PowerOffColor;
            }
        }

        if (DisplayLight)
        {
            DisplayLight.enabled = powerOn;
        }
    }

    #region Callbacks
    public void PowerOnOff()
    {
        if (isEjecting || isRewinding) return;

        if (!isOn)
        {
            SetDiodeColor(true);

            if (canEject)
            {
                if (!isStarted)
                {
                    DisplayText(TextOnDisplay.StandBy, true, true);
                }
                else
                {
                    DisplayText(TextOnDisplay.Stop, false, true);
                    TimeText.text = string.Format("{0}:{1}", min, sec);
                }
            }
            else
            {
                DisplayText(TextOnDisplay.StandBy, true, true);
            }

            TV.SetInput(true);
            isOn = true;
        }
        else
        {
            StateText.gameObject.SetActive(false);
            TimeText.gameObject.SetActive(false);
            SetDiodeColor(false);
            StopAllCoroutines();

            if (isPlaying)
            {
                isStopped = true;
                isPlaying = false;
            }

            TV.SetInput(false);
            isOn = false;
        }
    }

    public void PlayStopRewind()
    {
        if (isOn && canEject && !isEjecting && !isRewinding)
        {
            if (!isTapeEnded)
            {
                if (!isPlaying)
                {
                    DisplayText(TextOnDisplay.Play, false);
                    TV.PlayVideo(tape.Clip);

                    if (StartPlaySound)
                    {
                        VCRAudio.clip = StartPlaySound;
                        VCRAudio.Play();
                    }

                    isStopped = false;
                    isStarted = true;
                    isPlaying = true;
                }
                else
                {
                    TV.PauseVideo();
                    StopAllCoroutines();
                    DisplayText(TextOnDisplay.Stop, false);

                    if (StopSound)
                    {
                        VCRAudio.clip = StopSound;
                        VCRAudio.Play();
                    }

                    isStopped = true;
                    isPlaying = false;
                }
            }
            else
            {
                StopAllCoroutines();
                TV.SetOsdScreen(VCRTV.OSD.Rewind);
                StartCoroutine(OnRewind());
            }
        }
    }

    public void InsertTape()
    {
        if (isOn)
        {
            if (canInsert)
            {
                Inventory.Instance.OnInventorySelect(new int[1] { InvVideoTapeID }, VideoTapes.Select(x => x.ItemTag).ToArray(), this, "Select VideoTape to Insert", "You don't have any Video Tapes!");
            }
        }
        else
        {
            HFPS_GameManager.Instance.AddMessage("Power ON VHS Player first!");
        }
    }

    public void EjectTape()
    {
        if(isOn && canEject && !isEjecting && !isRewinding)
        {
            DisplayText(TextOnDisplay.Stop, false);
            TV.PauseVideo();
            TV.SetOsdScreen(VCRTV.OSD.Rewind);

            isStopped = true;
            isRewinding = true;
            isEjecting = true;

            StopAllCoroutines();
            StartCoroutine(OnRewind());
            StartCoroutine(OnEject());
        }
    }

    public void OnTapeEnd()
    {
        isTapeEnded = true;
        isPlaying = false;
        if (PlayStopRewindTrigger) { PlayStopRewindTrigger.falseTitle = "Rewind"; }
        DisplayText(TextOnDisplay.Stop, false);

        if (StopSound)
        {
            VCRAudio.clip = StopSound;
            VCRAudio.Play();
        }
    }

    public void OnTapeTake()
    {
        clipDuration = 0;
        tapeID = null;
        tapeDescription = string.Empty;
        tapeCID = string.Empty;
        tapeTexPath = string.Empty;
        canInsert = true;

        InsertTrigger.enabled = true;
        VideoTapeObj.inventoryID = -1;
        VideoTapeObj.enabled = false;
        VideoTapeObj.gameObject.SetActive(false);

        Anim.Play(TakeAnim);
    }
    #endregion

    public void OnItemSelect(int ID, CustomItemData data)
    {
        tapeID = ID;
        tapeDescription = data.dataDictionary[Inventory.ITEM_VALUE];
        tapeCID = data.dataDictionary[Inventory.ITEM_TAG];
        tapeTexPath = data.dataDictionary[Inventory.ITEM_PATH];

        if (VideoTapes.Count > 0)
        {
            tape = VideoTapes[FindIndex(tapeCID)];
            clipDuration = tape.Clip.length;

            VideoTapeObj.inventoryID = tapeID.Value;
            List<ItemHashtable> hashtables = new List<ItemHashtable>()
            {
                new ItemHashtable(Inventory.ITEM_VALUE, tapeDescription),
                new ItemHashtable(Inventory.ITEM_TAG, tapeCID),
                new ItemHashtable(Inventory.ITEM_PATH, tapeTexPath)
            };
            VideoTapeObj.itemHashtables = hashtables;
            VideoTapeObj.CreateCustomData(hashtables);

            if (tape.VideoTapeTex != null)
            {
                VideoTapeObj.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", tape.VideoTapeTex);
            }
            else
            {
                VideoTapeObj.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", DefaultTapeTex);
            }

            canInsert = false;
            isTapeEnded = false;
            InsertTrigger.enabled = false;
            VideoTapeObj.GetComponent<Collider>().enabled = false;

            StartCoroutine(OnInsert());
        }
        else
        {
            Debug.LogError("[VHS Player] Please set Video Tape settings!");
        }
    }

    #region Enumerators
    IEnumerator OnInsert()
    {
        VideoTapeObj.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        Anim.Play(InsertAnim);

        if (LoadTapeSound)
        {
            VCRAudio.clip = LoadTapeSound;
            VCRAudio.Play();
        }

        yield return new WaitUntil(() => !Anim.isPlaying);
        yield return new WaitForSeconds(0.35f);
        TV.SetVideoSource(true, true);
        DisplayText(TextOnDisplay.Wait, true);
        canEject = true;
    }

    IEnumerator OnEject()
    {
        yield return new WaitUntil(() => !isRewinding);
        yield return new WaitForSeconds(1);

        Anim.Play(EjectAnim);

        if (EjectSound)
        {
            VCRAudio.clip = EjectSound;
            VCRAudio.Play();
        }

        yield return new WaitUntil(() => !Anim.isPlaying);

        DisplayText(TextOnDisplay.StandBy, true);
        TV.SetVideoSource(false, true);

        VideoTapeObj.GetComponent<Collider>().enabled = true;
        VideoTapeObj.enabled = true;

        canEject = false;
        isEjecting = false;
        isStarted = false;
    }

    IEnumerator OnRewind()
    {
        isRewinding = true;
        isStopped = false;

        if (duration > 0)
        {
            if (RewindTapeSound)
            {
                VCRAudio.clip = RewindTapeSound;
                VCRAudio.Play();
            }

            yield return new WaitForSeconds(RewindAfterTime);

            DisplayText(TextOnDisplay.Rewind, false);

            while (duration > 0)
            {
                if (minutes > 0 && seconds == 0)
                {
                    seconds = 60;
                    minutes--;
                }
                seconds--;

                min = minutes.ToString("D2");
                sec = seconds.ToString("D2");

                TimeText.text = $"{min}:{sec}";

                duration--;
                yield return new WaitForSeconds(RewindTimeWait);
            }
        }

        if (PlayStopRewindTrigger) { PlayStopRewindTrigger.falseTitle = "Play"; }

        if (StopSound && (isStarted || isTapeEnded))
        {
            VCRAudio.clip = StopSound;
            VCRAudio.Play();
        }

        duration = 0;
        seconds = 0;
        minutes = 0;
        secondsDiff = 0;
        min = "00";
        sec = "00";

        DisplayText(TextOnDisplay.StandBy, true);
        TV.SetOsdScreen(VCRTV.OSD.Zero);

        isStarted = false;
        isPlaying = false;
        isRewinding = false;
        isTapeEnded = false;
    }
    #endregion

    int FindIndex(string CID)
    {
        for (int i = 0; i < VideoTapes.Count; i++)
        {
            if (VideoTapes[i].ItemTag == CID)
            {
                return i;
            }
        }

        return -1;
    }

    public void OnPauseEvent(bool isPaused)
    {
        if (isPaused)
        {
            if (VCRAudio.isPlaying)
            {
                VCRAudio.Pause();
            }
        }
        else
        {
            VCRAudio.UnPause();
        }

        if (isStarted && isPlaying && !isStopped && canEject && !isEjecting && !isRewinding)
        {
            if (isPaused)
            {
                TV.PauseVideo();
            }
            else
            {
                TV.PlayVideo(tape.Clip);
            }
        }
    }

    void ResetPlayer()
    {
        isPlaying = false;
        isEjecting = false;
        isStarted = false;
        isRewinding = false;
        isTapeEnded = false;
    }

    public Dictionary<string, object> OnSave()
    {
        return new Dictionary<string, object>()
        {
            { "vhs_tape", tapeID.GetValueOrDefault(-1) },
            { "vhs_tape_itag", tapeCID  },
            { "vhs_tape_desc", tapeDescription },
            { "vhs_tape_texpath", tapeTexPath },
            { "player_isOn", isOn },
            { "player_canEject", canEject },
            { "player_canInsert", canInsert },
            { "player_isEjecting", isEjecting },
            { "player_isRewinding", isRewinding },
            { "player_isTapeEnded", isTapeEnded },
            { "player_isStarted", isStarted },
            { "player_duration", duration },
            { "player_secdiff", secondsDiff },
            { "player_minutes", minutes },
            { "player_seconds", seconds },
            { "vcr_tv",
                new Dictionary<string, object>()
                {
                    { "tv_isOn", TV.isOn },
                    { "tv_lastframe", TV.videoPlayer.frame }
                }
            }
        };
    }

    public void OnLoad(JToken token)
    {
        tapeID = (int)token["vhs_tape"];
        tapeCID = (string)token["vhs_tape_itag"];
        tapeDescription = (string)token["vhs_tape_desc"];
        tapeTexPath = (string)token["vhs_tape_texpath"];

        if(tapeID != -1)
        {
            tape = VideoTapes[FindIndex(tapeCID)];
            clipDuration = tape.Clip.length;

            if (tape.VideoTapeTex != null)
            {
                VideoTapeObj.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", tape.VideoTapeTex);
            }
            else
            {
                VideoTapeObj.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", DefaultTapeTex);
            }
        }

        VideoTapeObj.inventoryID = tapeID.Value;

        if (!string.IsNullOrEmpty(tapeDescription) && !string.IsNullOrEmpty(tapeCID) && !string.IsNullOrEmpty(tapeTexPath))
        {
            List<ItemHashtable> hashtables = new List<ItemHashtable>()
            {
                new ItemHashtable(Inventory.ITEM_VALUE, tapeDescription),
                new ItemHashtable(Inventory.ITEM_TAG, tapeCID),
                new ItemHashtable(Inventory.ITEM_PATH, tapeTexPath)
            };
            VideoTapeObj.itemHashtables = hashtables;
            VideoTapeObj.CreateCustomData(hashtables);
        }

        bool loadOn = (bool)token["player_isOn"];
        bool loadEject = (bool)token["player_canEject"];
        bool loadInsert = (bool)token["player_canInsert"];

        bool loadStarted = (bool)token["player_isStarted"];
        bool loadEjecting = (bool)token["player_isEjecting"];
        bool loadRewinding = (bool)token["player_isRewinding"];
        bool loadTapeEnded = (bool)token["player_isTapeEnded"];

        int loadDuration = (int)token["player_duration"];
        int loadSecDiff = (int)token["player_secdiff"];
        int loadMin = (int)token["player_minutes"];
        int loadSec = (int)token["player_seconds"];

        bool tvLoadIsOn = (bool)token["vcr_tv"]["tv_isOn"];
        long tvLoadFrame = (long)token["vcr_tv"]["tv_lastframe"];

        if (loadOn)
        {
            PowerOnOff();
        }

        if (!loadInsert)
        {
            if (loadEjecting || !loadEject)
            {
                Debug.Log("Eject Load");

                InsertTrigger.enabled = false;
                VideoTapeObj.gameObject.SetActive(true);
                VideoTapeObj.GetComponent<Collider>().enabled = true;
                VideoTapeObj.enabled = true;

                DisplayText(TextOnDisplay.StandBy, true);
                TV.SetVideoSource(false, true);

                ResetPlayer();
                canInsert = true;
                canEject = false;
            }
            else if (loadRewinding)
            {
                Debug.Log("Rewind Load");

                duration = 0;
                seconds = 0;
                minutes = 0;
                min = "00";
                sec = "00";

                InsertTrigger.enabled = false;
                DisplayText(TextOnDisplay.StandBy, true);
                TV.SetOsdScreen(VCRTV.OSD.Zero);
                TV.SetVideoSource(true, false);

                ResetPlayer();
                canInsert = false;
                canEject = true;
            }
            else if (loadStarted)
            {
                Debug.Log("Play Load AT: " + tvLoadFrame);

                duration = loadDuration;
                secondsDiff = loadSecDiff;
                minutes = loadMin;
                seconds = loadSec;

                min = minutes.ToString("D2");
                sec = seconds.ToString("D2");
                txt_time = string.Format("{0}:{1}", min, sec);

                Debug.Log(min + " " + sec);

                InsertTrigger.enabled = false;
                TV.SetVideoSource(true, false);

                if (!loadTapeEnded)
                {
                    Debug.Log("Load Not Ended");

                    DisplayText(TextOnDisplay.Stop, false);
                    TV.SetOsdScreen(VCRTV.OSD.Pause);
                    TV.PauseVideoAT(tvLoadFrame, tape.Clip, tvLoadIsOn);
                    isStopped = true;
                }
                else
                {
                    Debug.Log("Load Ended");

                    if (PlayStopRewindTrigger) { PlayStopRewindTrigger.falseTitle = "Rewind"; }
                    TV.SetOsdScreen(VCRTV.OSD.Stop);
                    DisplayText(TextOnDisplay.Stop, false);
                    isTapeEnded = true;
                }

                isPlaying = false;
                canInsert = false;
                canEject = true;
            }
            else if (!loadStarted)
            {
                Debug.Log("Load Not Started");

                InsertTrigger.enabled = false;
                DisplayText(TextOnDisplay.StandBy, true);
                TV.SetVideoSource(true, true);

                ResetPlayer();
                canInsert = false;
                canEject = true;
            }
        }

        if (tvLoadIsOn && !TV.isOn)
        {
            TV.PowerOnOff(false);
        }
    }

    [Serializable]
    public class VideoTape
    {
        public string ItemTag;
        public Texture2D VideoTapeTex;
        public VideoClip Clip;
    }
}
