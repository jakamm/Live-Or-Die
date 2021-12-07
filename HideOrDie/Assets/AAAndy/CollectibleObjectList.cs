using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "CollectibleList")]
public class CollectibleObjectList : ScriptableObject
{
    public enum e_ClipType { Clip, Letter };
    public int JITB_Total;
    public int Clip_Total { get => ClipList.Count; }
    public int Letter_Total { get => LetterList.Count; }
    [SerializeField]
    List<VideoClip> ClipList;
    [SerializeField]
    List<VideoClip> LetterList;
    [TextArea]
    [SerializeField]
    List<string> LetterContentList;

    public VideoClip GetClip(e_ClipType clipType, int index)
    {
        switch (clipType)
        {
            case e_ClipType.Clip:
                return ClipList[index];
            case e_ClipType.Letter:
                return LetterList[index];
            default:
                return null;
                break;
        }
    }
    public string GetLetterContent(int index) => LetterContentList[index];
}
