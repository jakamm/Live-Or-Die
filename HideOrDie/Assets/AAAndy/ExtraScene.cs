using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;
using UnityEngine.UI;

public class ExtraScene : MonoBehaviour
{
    public CollectibleObjectList objectList;
    public bool IsExtraOn;
    [Space(10)]
    public UnityEvent OnExtraOnStart;
    public UnityEvent OnExtraOnEnd;
    public UnityEvent OnExtraOffStart;
    public UnityEvent OnExtraOffEnd;

    [Space(10)]
    public GameObject staticClip;
    public GameObject buttonPrefab;
    public Transform clipList;
    public Transform letterList;
    public GameObject clipVideoPlayer;
    public GameObject letterVideoPlayer;
    [Space(10)]
    public GameObject letterTextGO;
    public Text letterText;
    public AudioSource changeChannelSound;
    public GameObject showTextBTN;

    CollectibleManager cm;

    
    
    VideoPlayer cvPlayer;
    AudioSource audioSource;

    VideoPlayer lvPlayer;

    GameObject selectedButton;
    
    bool isTextOn;
    // Start is called before the first frame update
    void Start()
    {
        InitializeClipAndLetterList();
        cvPlayer = clipVideoPlayer.GetComponent<VideoPlayer>();
        audioSource = clipVideoPlayer.GetComponent<AudioSource>();
        lvPlayer = letterVideoPlayer.GetComponent<VideoPlayer>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ReturnToMainMenu()
    {
        OnExtraOffStart?.Invoke();
    }

    public void OpenExtra()
    {
        OnExtraOnStart?.Invoke();
    }

    void playTheClip(int index)
    {
        VideoClip clip = objectList.GetClip(CollectibleObjectList.e_ClipType.Clip, index);
        clipVideoPlayer.SetActive(true);
        letterVideoPlayer.SetActive(false);
        staticClip.SetActive(false);

        cvPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        cvPlayer.EnableAudioTrack(0, true);
        cvPlayer.SetTargetAudioSource(0, audioSource);
        cvPlayer.controlledAudioTrackCount = 1;
        cvPlayer.clip = clip;

        //vPlayer.Prepare();
        changeChannelSound.Play();     
        audioSource.Play();
        cvPlayer.Play();

        //Close Letter and Button
        letterTextGO.SetActive(false);
        showTextBTN.SetActive(false);
    }

    void playTheLetter(int index)
    {
        VideoClip clip = objectList.GetClip(CollectibleObjectList.e_ClipType.Letter, index);

        clipVideoPlayer.SetActive(false);
        letterVideoPlayer.SetActive(true);
        staticClip.SetActive(false);

        changeChannelSound.Play();
        lvPlayer.clip = clip;
        lvPlayer.Play();

        letterText.text = objectList.GetLetterContent(index);
        letterTextGO.SetActive(false);
        showTextBTN.SetActive(true);
    }

    void InitializeClipAndLetterList()
    {
        if (!cm) cm = FindObjectOfType<CollectibleManager>();
        int totalClip = objectList.Clip_Total;

        for (int i = 0; i < totalClip; i++)
        {
            GameObject temp = Instantiate(buttonPrefab, clipList);
            temp.name = i.ToString();
            temp.GetComponentInChildren<Text>().text = (i + 1).ToString();
            temp.GetComponent<Button>().onClick.AddListener(() => SwappingButton(temp));
            temp.GetComponent<Button>().onClick.AddListener(() => playTheClip(int.Parse(temp.name)));
            
            bool ifInteracted = cm.GetIfDestroyed(CollectibleObject.ObjectType.Clip, i);
            temp.GetComponent<Button>().interactable = ifInteracted;
        }

        int totalLetter = objectList.Letter_Total;

        for (int i = 0; i < totalLetter; i++)
        {
            GameObject temp = Instantiate(buttonPrefab, letterList);
            temp.name = i.ToString();
            temp.GetComponentInChildren<Text>().text = (i + 1).ToString();
            temp.GetComponent<Button>().onClick.AddListener(() => SwappingButton(temp));
            temp.GetComponent<Button>().onClick.AddListener(() => playTheLetter(int.Parse(temp.name)));
            bool ifInteracted = cm.GetIfDestroyed(CollectibleObject.ObjectType.Letter, i);
            temp.GetComponent<Button>().interactable = ifInteracted;
        }
    }

    void SwappingButton(GameObject newBtn)
    {
        if(selectedButton != null)
        {
            selectedButton.GetComponent<Image>().enabled = false;
        }
        selectedButton = newBtn;
        selectedButton.GetComponent<Image>().enabled = true;
    }

    public void ClearSelectedButton()
    {
        if(selectedButton!=null)
        {
            selectedButton.GetComponent<Image>().enabled = false;
            selectedButton = null;
        }
    }
}
