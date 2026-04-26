// Assets/Scripts/Loadout/LoadoutApplier.cs
using UnityEngine;
using System.Collections.Generic;

public class LoadoutApplier : MonoBehaviour
{
    [Header("Utility Prefabs (match UtilityDefinition order)")]
    public UtilityPrefabMapping[] mappings;

    [Header("Fallback Loadout (Used if bypassing Loadout Scene)")]
    public LoadoutData.LoadoutEntry[] fallbackLoadout;

    [System.Serializable]
    public class UtilityPrefabMapping
    {
        public UtilityDefinition definition;
        public BaseUtility utilityPrefab; 
    }

    private void Awake()
    {
        var manager = GetComponent<UtilityManager>();
        List<LoadoutData.LoadoutEntry> loadoutToApply = null;

        // 1. Determine which loadout to use
        if (LoadoutData.Instance != null && LoadoutData.Instance.ConfirmedLoadout.Count > 0)
        {
            Debug.Log("[LoadoutApplier] Applying confirmed loadout from LoadoutScene.");
            loadoutToApply = LoadoutData.Instance.ConfirmedLoadout;
        }
        else if (fallbackLoadout != null && fallbackLoadout.Length > 0)
        {
            Debug.Log("[LoadoutApplier] No ConfirmedLoadout found. Using Fallback Loadout.");
            loadoutToApply = new List<LoadoutData.LoadoutEntry>(fallbackLoadout);
        }
        else
        {
            Debug.Log("[LoadoutApplier] No loadout found at all. Leaving Inspector defaults.");
            return;
        }

        // 2. Clear existing utilities to prevent "Level Scene" defaults from staying
        manager.availableUtilities = new BaseUtility[0];
        var result = new List<BaseUtility>();

        // 3. Spawn the utilities
        foreach (var entry in loadoutToApply)
        {
            bool foundMapping = false;
            foreach (var mapping in mappings)
            {
                if (mapping.definition == entry.definition && mapping.utilityPrefab != null)
                {
                    BaseUtility instance = Instantiate(mapping.utilityPrefab, transform);
                    
                    // Set max uses
                    instance.maxUses = entry.count;
                    
                    // Set current uses
                    var field = typeof(BaseUtility).GetField("_currentUses", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (field != null) field.SetValue(instance, entry.count);

                    result.Add(instance);
                    foundMapping = true;
                    Debug.Log($"[LoadoutApplier] Spawned {instance.UtilityName} with {entry.count} uses.");
                    break;
                }
            }
            if (!foundMapping) Debug.LogWarning($"[LoadoutApplier] No prefab mapping found for {entry.definition.utilityName}!");
        }

        manager.availableUtilities = result.ToArray();
    }
}