using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that persists across scenes, carrying the player's chosen loadout.
/// </summary>
public class LoadoutData : MonoBehaviour
{
    public static LoadoutData Instance { get; private set; }

    // What the player confirmed — passed into the game scene
    public List<LoadoutEntry> ConfirmedLoadout { get; private set; } = new();

    // Tracks if we actually hit "Start" in the loadout scene
    public bool HasConfirmedLoadout { get; private set; } = false;

    [System.Serializable]
    public class LoadoutEntry
    {
        public UtilityDefinition definition;
        public int count;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetLoadout(List<LoadoutEntry> entries)
    {
        ConfirmedLoadout = new List<LoadoutEntry>(entries);
        HasConfirmedLoadout = true; // We purposefully saved a loadout
    }
}