using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TipsManager : MonoBehaviour {

    public string[] Tips;

    [Space(5)]
    public Text TipsText;

    [Header("Settings")]
    public string TipPrefix;
    public float TipTime;

    private List<int> tipsCache = new List<int>();

    void OnEnable()
    {
        InvokeRepeating("ChangeTip", 0, TipTime);
    }

    public void ResetInvoke()
    {
        InvokeRepeating("ChangeTip", 0, TipTime);
    }

    void Update()
    {
        if (!TipsText.gameObject.activeSelf)
        {
            CancelInvoke();
        }
    }

    private void ChangeTip()
    {
        if (string.IsNullOrEmpty(TipPrefix))
        {
            TipsText.text = Tips[GetTipNumber()];
        }
        else
        {
            TipsText.text = TipPrefix + ": " + Tips[GetTipNumber()];
        }
    }

    private int GetTipNumber()
    {
        int tip;

        if (tipsCache.Count != Tips.Length)
        {
            while (!tipsCache.Contains(tip = Random.Range(0, Tips.Length)))
            {
                tipsCache.Add(tip);
                return tip;
            }
        }
        else
        {
            tipsCache.Clear();
        }

        return 0;
    }
}
