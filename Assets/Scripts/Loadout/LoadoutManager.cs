// Assets/Scripts/Loadout/LoadoutManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoadoutManager : MonoBehaviour
{
    [Header("Level Config")]
    public LevelLoadoutConfig levelConfig;

    [Header("Available Utilities This Level")]
    public UtilityDefinition[] availableUtilities;

    [Header("Settings")]
    public int totalSlots = 6;

    [Header("Preview Panel")]
    public Image previewIcon;
    public TextMeshProUGUI previewName;
    public TextMeshProUGUI slotsLeftText;
    public Button arrowLeft;
    public Button arrowRight;

    [Header("Tray Slots (assign TraySlot_0 through TraySlot_5)")]
    public TraySlotReference[] traySlots = new TraySlotReference[6];

    [Header("Scene To Load")]
    public string gameSceneName = "Level1";

    [System.Serializable]
    public class TraySlotReference
    {
        public Button button;
        public Image utilityIcon;
        public TextMeshProUGUI utilityText;

        [HideInInspector] public int utilityIndex = -1; // -1 = empty
    }

    private int _previewIndex = 0;
    private int[] _chosen; // count of each utility placed in tray

    // -------------------------------------------------------------------------

    private void Awake()
    {
        // OVERRIDE the inspector properties if we transitioned via the Progress Manager
        if (GameProgressManager.Instance != null && GameProgressManager.Instance.CurrentPendingConfig != null)
        {
            levelConfig = GameProgressManager.Instance.CurrentPendingConfig;
            gameSceneName = levelConfig.levelSceneName;
        }

        availableUtilities = levelConfig.availableUtilities;
        _chosen = new int[availableUtilities.Length];
    }

    private void Start()
    {
        // Wire up arrow buttons
        arrowLeft.onClick.AddListener(OnArrowLeft);
        arrowRight.onClick.AddListener(OnArrowRight);

        // Wire up tray slot buttons
        for (int i = 0; i < traySlots.Length; i++)
        {
            int captured = i;
            if (traySlots[i].button != null)
                traySlots[i].button.onClick.AddListener(() => OnTraySlotClicked(captured));
        }

        // Clear all tray slots visually
        for (int i = 0; i < traySlots.Length; i++)
            ClearSlotVisual(i);

        RefreshPreview();
        RefreshSlotsLeft();
    }

    // ── Arrow navigation ──────────────────────────────────────────────────────

    private void OnArrowLeft()
    {
        _previewIndex = (_previewIndex - 1 + availableUtilities.Length) % availableUtilities.Length;
        RefreshPreview();
    }

    private void OnArrowRight()
    {
        _previewIndex = (_previewIndex + 1) % availableUtilities.Length;
        RefreshPreview();
    }

    // ── Tray slot clicked ─────────────────────────────────────────────────────

    private void OnTraySlotClicked(int slotIndex)
    {
        TraySlotReference slot = traySlots[slotIndex];
        UtilityDefinition def  = availableUtilities[_previewIndex];

        if (slot.utilityIndex == _previewIndex)
        {
            // Clicking an already-filled slot with the same utility → remove it
            _chosen[_previewIndex]--;
            ClearSlotVisual(slotIndex);
        }
        else if (slot.utilityIndex == -1)
        {
            // Empty slot → try to place current utility
            if (SlotsUsed() + def.slotCost <= totalSlots)
            {
                _chosen[_previewIndex]++;
                FillSlotVisual(slotIndex, _previewIndex);
            }
            else
            {
                Debug.Log("[Loadout] Not enough slots!");
            }
        }
        else
        {
            // Slot has a different utility → swap it out
            _chosen[slot.utilityIndex]--;
            ClearSlotVisual(slotIndex);

            if (SlotsUsed() + def.slotCost <= totalSlots)
            {
                _chosen[_previewIndex]++;
                FillSlotVisual(slotIndex, _previewIndex);
            }
        }

        RefreshSlotsLeft();
        RefreshTrayHighlights();
    }

    // ── Start Game ────────────────────────────────────────────────────────────

    public void OnStartGame()
    {
        var entries = new List<LoadoutData.LoadoutEntry>();

        foreach(var entry in entries) 
            Debug.Log($"[LoadoutManager] Exporting: {entry.definition.utilityName} x{entry.count}");

        for (int i = 0; i < availableUtilities.Length; i++)
        {
            if (_chosen[i] > 0)
            {
                entries.Add(new LoadoutData.LoadoutEntry
                {
                    definition = availableUtilities[i],
                    count      = _chosen[i]
                });
            }
        }

        if (LoadoutData.Instance == null)
            new GameObject("LoadoutData").AddComponent<LoadoutData>();

        LoadoutData.Instance.SetLoadout(entries);
        SceneTransitionManager.Instance.LoadScene(gameSceneName);
    }

    // ── Visual helpers ────────────────────────────────────────────────────────

    private void RefreshPreview()
    {
        UtilityDefinition def = availableUtilities[_previewIndex];

        if (previewIcon != null)
        {
            previewIcon.sprite   = def.icon;
            previewIcon.material = def.iconMaterial;
            previewIcon.enabled  = def.icon != null;
        }

        if (previewName != null)
            previewName.text = def.utilityName;

        RefreshTrayHighlights();
    }

    private void RefreshSlotsLeft()
    {
        if (slotsLeftText != null)
            slotsLeftText.text = $"Slots Left: {totalSlots - SlotsUsed()}";
    }

    private void FillSlotVisual(int slotIndex, int utilityIndex)
    {
        TraySlotReference slot = traySlots[slotIndex];
        UtilityDefinition def  = availableUtilities[utilityIndex];

        slot.utilityIndex = utilityIndex;

        if (slot.utilityIcon != null)
        {
            slot.utilityIcon.sprite   = def.icon;
            slot.utilityIcon.material = def.iconMaterial;
            slot.utilityIcon.enabled  = true;
        }

        if (slot.utilityText != null)
            slot.utilityText.text = def.utilityName;
    }

    private void ClearSlotVisual(int slotIndex)
    {
        TraySlotReference slot = traySlots[slotIndex];
        slot.utilityIndex = -1;

        if (slot.utilityIcon != null)
        {
            slot.utilityIcon.sprite   = null;
            slot.utilityIcon.material = null;
            slot.utilityIcon.enabled  = false;
        }

        if (slot.utilityText != null)
            slot.utilityText.text = "";
    }

    /// <summary>
    /// Highlights tray slots that contain the currently previewed utility.
    /// Uses the slot's Image color as a simple highlight.
    /// Swap this for material changes if you prefer.
    /// </summary>
    private void RefreshTrayHighlights()
    {
        for (int i = 0; i < traySlots.Length; i++)
        {
            bool isActive = traySlots[i].utilityIndex == _previewIndex;
            Image slotBg = traySlots[i].button?.GetComponent<Image>();
            if (slotBg != null)
                slotBg.color = isActive ? new Color(0.2f, 0.8f, 0.8f, 1f)  // teal highlight
                                        : Color.white;
        }
    }

    private int SlotsUsed()
    {
        int used = 0;
        for (int i = 0; i < availableUtilities.Length; i++)
            used += _chosen[i] * availableUtilities[i].slotCost;
        return used;
    }
}