using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "CollectibleList")]
public class CollectibleObjectList : ScriptableObject
{
    public List<VideoClip> ClipList;
    public List<VideoClip> LetterList;
    [TextArea]
    public List<string> LetterContentList;
}
