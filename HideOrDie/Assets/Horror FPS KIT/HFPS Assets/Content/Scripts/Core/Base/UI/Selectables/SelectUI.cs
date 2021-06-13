using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ThunderWire.Game.Options;

public class SelectUI : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    private AdvancedMenuUI menuUI;

    public string[] Items;
    public Text itemsText;

    [HideInInspector]
    public int index;

    private bool isSelected;
    private bool isChanged;

    void Awake()
    {
        menuUI = AdvancedMenuUI.Instance;
    }

    void Start()
    {
        itemsText.text = Items[index];
    }

    void Update()
    {
        if (isSelected)
        {
            if (menuUI.Navigation.x != 0)
            {
                if (!isChanged)
                {
                    if (menuUI.Navigation.x > 0.1)
                    {
                        index = index < Items.Length - 1 ? index + 1 : 0;
                    }
                    else if (menuUI.Navigation.x < -0.1)
                    {
                        index = index > 0 ? index - 1 : Items.Length - 1;
                    }

                    itemsText.text = Items[index];
                }

                isChanged = true;
            }
            else
            {
                isChanged = false;
            }
        }
    }

    public void SetValue(int value)
    {
        if (value < Items.Length - 1)
        {
            itemsText.text = Items[value];
            index = value;
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
    }

    void OnDisable()
    {
        isSelected = false;
    }
}
