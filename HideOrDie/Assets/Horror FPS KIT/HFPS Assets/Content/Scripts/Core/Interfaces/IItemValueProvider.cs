interface IItemValueProvider
{
    string OnGetValue();
    void OnSetValue(string value);
}

interface IItemSelect
{
    void OnItemSelect(int ID, CustomItemData data);
}