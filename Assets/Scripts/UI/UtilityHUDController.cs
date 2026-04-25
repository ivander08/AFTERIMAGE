// Assets/Scripts/UI/UtilityHUDController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UtilityHUDController : MonoBehaviour
{
    [System.Serializable]
    public class UtilitySlotUI
    {
        public Image utilityIcon;
        public TextMeshProUGUI countText;
    }

    [System.Serializable]
    public class UtilityVisuals
    {
        public Sprite icon;
        public Material material;
    }

    [Header("Slot UI (0 = selected, 1 & 2 = unselected)")]
    public UtilitySlotUI[] slots = new UtilitySlotUI[3];

    [Header("Per-Utility Visuals (match order of UtilityManager.availableUtilities)")]
    public UtilityVisuals[] utilityVisuals;

    [Header("Dependencies")]
    public UtilityManager utilityManager;

    private int _lastSelectedIndex = -1;

    private void Update()
    {
        if (utilityManager == null || utilityManager.availableUtilities == null) return;

        int selectedIndex = GetCurrentIndex();
        bool dirty = selectedIndex != _lastSelectedIndex;

        // Also refresh if any use count changed
        if (!dirty)
        {
            foreach (var u in utilityManager.availableUtilities)
                if (u != null && u.CurrentUses != GetCachedUses(u)) { dirty = true; break; }
        }

        if (!dirty) return;
        _lastSelectedIndex = selectedIndex;
        Refresh(selectedIndex);
    }

    private void Refresh(int selectedIndex)
    {
        BaseUtility[] utilities = utilityManager.availableUtilities;
        int total = utilities.Length;

        // Build display order: selected first, then the rest in order
        int[] displayOrder = new int[3]; // which utility index goes in which slot
        int slotFill = 0;

        // Slot 0 = selected utility
        displayOrder[0] = selectedIndex;
        slotFill = 1;

        // Slots 1 & 2 = the others, in array order
        for (int i = 0; i < total && slotFill < 3; i++)
        {
            if (i != selectedIndex)
                displayOrder[slotFill++] = i;
        }

        // Fill remaining slots with -1 (empty)
        for (; slotFill < 3; slotFill++)
            displayOrder[slotFill] = -1;

        // Apply to UI
        for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
        {
            UtilitySlotUI slot = slots[slotIndex];
            int utilIndex = displayOrder[slotIndex];

            bool hasUtility = utilIndex >= 0 && utilIndex < total && utilities[utilIndex] != null;

            if (slot.utilityIcon != null)
            {
                slot.utilityIcon.enabled = hasUtility;
                if (hasUtility && utilityVisuals != null && utilIndex < utilityVisuals.Length)
                {
                    slot.utilityIcon.sprite   = utilityVisuals[utilIndex].icon;
                    slot.utilityIcon.material = utilityVisuals[utilIndex].material;
                }
            }

            if (slot.countText != null)
            {
                if (hasUtility)
                {
                    int uses = utilities[utilIndex].CurrentUses;
                    slot.countText.text    = uses.ToString();
                    slot.countText.enabled = uses > 0;
                }
                else
                {
                    slot.countText.enabled = false;
                }
            }
        }
    }

    private int GetCurrentIndex()
    {
        BaseUtility current = utilityManager.GetCurrentUtility();
        for (int i = 0; i < utilityManager.availableUtilities.Length; i++)
            if (utilityManager.availableUtilities[i] == current) return i;
        return 0;
    }

    // Simple per-frame use-count cache to detect changes
    private System.Collections.Generic.Dictionary<BaseUtility, int> _usesCache = new();
    private int GetCachedUses(BaseUtility u)
    {
        if (_usesCache.TryGetValue(u, out int cached)) return cached;
        _usesCache[u] = u.CurrentUses;
        return u.CurrentUses;
    }
}