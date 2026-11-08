using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all dynamic level items: traps, pickups, screemers, sound triggers, windows.
/// Handles saving/loading references for persistence.
/// </summary>
public class LevelItemsManager : MonoBehaviour
{
    public static LevelItemsManager Instance { get; private set; }

    [Header("Runtime Lists")]
    public List<GameObject> TrapObjects = new List<GameObject>();
    public List<GameObject> Items = new List<GameObject>();
    public List<GameObject> Screemers = new List<GameObject>();
    public List<GameObject> SoundTriggers = new List<GameObject>();
    public List<GameObject> WindowsObj = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Register a newly placed trap.
    /// </summary>
    public void AddTrap(GameObject trap)
    {
        if (trap != null && !TrapObjects.Contains(trap))
        {
            TrapObjects.Add(trap);
        }
    }

    /// <summary>
    /// Remove null/destroyed entries from trap list.
    /// </summary>
    public void RefreshTrapList()
    {
        for (int i = TrapObjects.Count - 1; i >= 0; i--)
        {
            if (TrapObjects[i] == null)
            {
                TrapObjects.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Remove null/inactive entries from sound triggers list.
    /// </summary>
    public void RefreshSoundTriggersList()
    {
        for (int i = SoundTriggers.Count - 1; i >= 0; i--)
        {
            if (SoundTriggers[i] == null || !SoundTriggers[i].activeSelf)
            {
                SoundTriggers.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Remove null/inactive entries from screemers list.
    /// </summary>
    public void RefreshScreemersList()
    {
        for (int i = Screemers.Count - 1; i >= 0; i--)
        {
            if (Screemers[i] == null || !Screemers[i].activeSelf)
            {
                Screemers.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Remove null/inactive entries from items list.
    /// </summary>
    public void RefreshItemList()
    {
        for (int i = Items.Count - 1; i >= 0; i--)
        {
            if (Items[i] == null || !Items[i].activeSelf)
            {
                Items.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Register a window object.
    /// </summary>
    public void RegisterWindow(GameObject window)
    {
        if (window != null && !WindowsObj.Contains(window))
        {
            WindowsObj.Add(window);
        }
    }

    /// <summary>
    /// Register a sound trigger.
    /// </summary>
    public void RegisterSoundTrigger(GameObject trigger)
    {
        if (trigger != null && !SoundTriggers.Contains(trigger))
        {
            SoundTriggers.Add(trigger);
        }
    }

    /// <summary>
    /// Register a screemer.
    /// </summary>
    public void RegisterScreemer(GameObject screemer)
    {
        if (screemer != null && !Screemers.Contains(screemer))
        {
            Screemers.Add(screemer);
        }
    }

    /// <summary>
    /// Register a pickup item.
    /// </summary>
    public void RegisterItem(GameObject item)
    {
        if (item != null && !Items.Contains(item))
        {
            Items.Add(item);
        }
    }
}