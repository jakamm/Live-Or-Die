using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ThunderWire.Game.Options;

public class ToggleUI : OptionBehaviour, ISelectHandler, IDeselectHandler
{
    private AdvancedMenuUI menuUI;

    public bool isOn = false;

    [Header("Objects")]
    public Text ToggleText;

    [Header("Text")]
    public string EnabledText = "Enabled";
    public string DisabledText = "Disabled";

    private bool isChanged = false;
    private bool isSelected = false;

    private void Awake()
    {
        menuUI = AdvancedMenuUI.Instance;
    }

    private void Update()
    {
        if (isSelected)
        {
            if (!isChanged && menuUI.Navigation.x != 0)
            {
                ChangeToggle();
                isChanged = true;
            }
            else if (menuUI.Navigation.x == 0)
            {
                isChanged = false;
            }
        }

        if (isOn)
        {
            ToggleText.text = EnabledText;
        }
        else
        {
            ToggleText.text = DisabledText;
        }
    }

    public void ChangeToggle()
    {
        if (!isOn)
        {
            ToggleText.text = EnabledText;
            isOn = true;
        }
        else
        {
            ToggleText.text = DisabledText;
            isOn = false;
        }

        OnChanged();
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

    public override object GetValue()
    {
        return isOn;
    }

    public override void SetValue(string value)
    {
        isOn = bool.Parse(value);
    }

    void OnChanged()
    {
        OptionsController.Instance.OnOptionChanged(this);
    }
}
