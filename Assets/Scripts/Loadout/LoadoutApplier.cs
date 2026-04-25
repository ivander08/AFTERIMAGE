// Assets/Scripts/Loadout/LoadoutApplier.cs
using UnityEngine;

/// <summary>
/// Reads LoadoutData and configures the UtilityManager at game start.
/// Attach to the same GameObject as UtilityManager.
/// </summary>
[RequireComponent(typeof(UtilityManager))]
public class LoadoutApplier : MonoBehaviour
{
    [Header("Utility Prefabs (match UtilityDefinition order)")]
    public UtilityPrefabMapping[] mappings;

    [System.Serializable]
    public class UtilityPrefabMapping
    {
        public UtilityDefinition definition;
        public BaseUtility utilityPrefab;    // the actual MonoBehaviour prefab
    }

    private void Awake()
    {
        if (LoadoutData.Instance == null) return;

        var manager = GetComponent<UtilityManager>();
        var confirmed = LoadoutData.Instance.ConfirmedLoadout;
        var result = new System.Collections.Generic.List<BaseUtility>();

        foreach (var entry in confirmed)
        {
            foreach (var mapping in mappings)
            {
                if (mapping.definition == entry.definition && mapping.utilityPrefab != null)
                {
                    // Instantiate as child, set uses
                    BaseUtility instance = Instantiate(mapping.utilityPrefab, transform);
                    instance.maxUses = entry.count;
                    // Reset current uses to match
                    instance.GetType()
                        .GetField("_currentUses",
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Instance)
                        ?.SetValue(instance, entry.count);

                    result.Add(instance);
                    break;
                }
            }
        }

        manager.availableUtilities = result.ToArray();
    }
}