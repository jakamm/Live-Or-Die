using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType { Normal, Heal, ItemPart, Weapon, Bullets }
public enum ItemAction { None, Increase, Decrease, ItemValue }

public class InventoryScriptable : ScriptableObject {

	public List<ItemMapper> ItemDatabase = new List<ItemMapper> ();

	[Serializable]
	public class ItemMapper {

		public string Title;
        [ReadOnly] public int ID;
        [Multiline] public string Description;
        public ItemType itemType;
        public ItemAction useActionType = ItemAction.None;
        public Sprite itemSprite;
        public GameObject dropObject;
        public GameObject packDropObject;

        [Serializable]
        public class Booleans
        {
            public bool isStackable;
            public bool isUsable;
            public bool isCombinable;
            public bool isDroppable;
            public bool isRemovable;
            public bool canInspect;
            public bool canBindShortcut;
            public bool CombineGetItem;
            public bool CombineNoRemove;
            public bool CombineGetSwItem;
            public bool UseItemSwitcher;
            public bool ShowContainerDesc;
            public bool doActionUse;
            public bool doActionCombine;
        }
        public Booleans itemToggles = new Booleans();

        [Serializable]
        public class Sounds
        {
            public AudioClip useSound;
            public AudioClip combineSound;
            [Range(0,1f)]
            public float soundVolume = 1f;
        }
        public Sounds itemSounds = new Sounds();

        [Serializable]
        public class Settings
        {
            public int maxItemCount;
            public int useSwitcherID = -1;
            public int healAmount;
            public Vector3 inspectRotation;
        }

		public Settings itemSettings = new Settings();

        [Serializable]
        public class CustomActionSettings
        {
            public int triggerValue;
            public int triggerAddItem;
            public string addItemValue;
            public bool actionRemove;
            public bool actionAddItem;
            public bool actionRestrictUse;
            public bool actionRestrictCombine;
        }

        public CustomActionSettings useActionSettings = new CustomActionSettings();

        [Serializable]
        public class CombineSettings
        {
            public int combineWithID;
            public int resultCombineID;
            public int combineSwitcherID;
        }

        public CombineSettings[] combineSettings;
    }

    public void Reseed()
    {
        foreach (ItemMapper x in ItemDatabase)
        {
            x.ID = ItemDatabase.IndexOf(x);
        }
    }
}
