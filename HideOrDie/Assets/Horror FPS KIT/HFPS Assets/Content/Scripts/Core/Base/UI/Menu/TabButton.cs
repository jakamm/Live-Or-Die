using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using ThunderWire.Game.Options;

public class TabButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler {

    public AdvancedMenuUI.OptionTab tab = AdvancedMenuUI.OptionTab.General;
    [Space(7)]
    public bool holdColor;

    [Header("Graphic")]
    public Image ButtonImage;
    public Text ButtonText;

    [Header("Button Colors")]
    public Color NormalColor = Color.white;
    public Color HoverColor = Color.white;
    public Color PressedColor = Color.white;
    public Color HoldColor = Color.white;

    [Header("Text Colors")]
    public bool useTextColor;
    public Color TextNormalColor = Color.white;
    public Color TextHoverColor = Color.white;
    public Color TextPressedColor = Color.white;
    public Color TextHoldColor = Color.white;

    void OnEnable()
    {
        if (transform.childCount > 0 && transform.GetChild(0).GetComponent<Text>() && useTextColor)
        {
            ButtonText = transform.GetChild(0).GetComponent<Text>();
            ButtonText.color = TextNormalColor;
        }

        if (holdColor)
        {
            ButtonImage.color = HoldColor;
            if(useTextColor) ButtonText.color = TextHoldColor;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (holdColor)
        {
            return;
        }

        ButtonImage.color = PressedColor;
        if (useTextColor) ButtonText.color = TextPressedColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        holdColor = true;

        AdvancedMenuUI.Instance.SelectTab((int)tab);

        ButtonImage.color = HoldColor;
        if (useTextColor) ButtonText.color = TextHoldColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (holdColor)
        {
            return;
        }

        ButtonImage.color = HoverColor;
        if (useTextColor) ButtonText.color = TextHoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (holdColor)
        {
            return;
        }

        ButtonImage.color = NormalColor;
        if (useTextColor) ButtonText.color = TextNormalColor;
    }

    public void Select()
    {
        holdColor = true;
        ButtonImage.color = HoldColor;
        if (useTextColor) ButtonText.color = TextHoldColor;
    }

    public void Unhold()
    {
        holdColor = false;
        ButtonImage.color = NormalColor;
        if (useTextColor) ButtonText.color = TextNormalColor;
    }
}
