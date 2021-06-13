using UnityEngine;

/// <summary>
/// Contains all required methods for ItemSwitcher script.
/// </summary>
public abstract class SwitcherBehaviour : MonoBehaviour
{
    /// <summary>
    /// Will be called when ItemSwitcher selects an item.
    /// </summary>
    public abstract void OnSwitcherSelect();

    /// <summary>
    /// Will be called when ItemSwitcher deselects an item.
    /// </summary>
    public abstract void OnSwitcherDeselect();

    /// <summary>
    /// Will be called when ItemSwitcher activates an item.
    /// </summary>
    public abstract void OnSwitcherActivate();

    /// <summary>
    /// Will be called when ItemSwitcher deactivates an item.
    /// </summary>
    public abstract void OnSwitcherDeactivate();

    /// <summary>
    /// Will be called when other script blocks the ItemSwitcher functions.
    /// </summary>
    public virtual void OnSwitcherDisable(bool enabled) { }

    /// <summary>
    /// Will be called when ItemSwitcher hits an wall.
    /// </summary>
    public virtual void OnSwitcherWallHit(bool hit) { }
}
