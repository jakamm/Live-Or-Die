using UnityEngine.UI;
using ThunderWire.Game.Options;

public class DropdownUI : OptionBehaviour
{
    public Dropdown dropdown;

    void Awake()
    {
        dropdown.onValueChanged.AddListener(delegate { OnChanged(dropdown); });
    }

    public override void SetValue(string value)
    {
        dropdown.value = int.Parse(value);
        dropdown.RefreshShownValue();
    }

    public override object GetValue()
    {
        return dropdown.value;
    }

    public void OnChanged(Dropdown drop)
    {
        OptionsController.Instance.OnOptionChanged(this);
    }
}
