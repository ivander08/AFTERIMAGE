using System.Collections;
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

    [Header("New Utility Announcement")]
    public RectTransform newUtilityPanel;
    public Image newUtilityImage;
    public TextMeshProUGUI newUtilityNameText;
    public TextMeshProUGUI newUtilityDescText; // Map to your ExplanationText
    public Button newUtilityOkButton;
    public AudioClip newUtilitySfx;

    [Header("Are You Sure? (no utilities equipped)")]
    public RectTransform areYouSurePanel;
    public Button areYouSureYesButton;
    public Button areYouSureNoButton;

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

        // Ensure the AreYouSure panel starts hidden
        if (areYouSurePanel != null)
            areYouSurePanel.gameObject.SetActive(false);
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
        RefreshEmptySlotLabels();
        RefreshSlotsLeft();

        // Check for new utility unlocks
        CheckForNewUtilityUnlock();
    }

    // ── Unlock Logic ──────────────────────────────────────────────────────────

    private void CheckForNewUtilityUnlock()
    {
        // REPLACED: No more PlayerPrefs. Just check if the config dictates a new unlock.
        if (levelConfig != null && levelConfig.newlyUnlockedUtility != null && newUtilityPanel != null)
        {
            ShowNewUtilityPanel(levelConfig.newlyUnlockedUtility);
        }
        else if (newUtilityPanel != null)
        {
            newUtilityPanel.gameObject.SetActive(false);
        }
    }

    private void ShowNewUtilityPanel(UtilityDefinition def)
    {
        // Populate UI
        if (newUtilityImage != null)
        {
            newUtilityImage.sprite = def.icon;
            newUtilityImage.material = def.iconMaterial;
        }
        if (newUtilityNameText != null) newUtilityNameText.text = def.utilityName;
        if (newUtilityDescText != null) newUtilityDescText.text = def.description;

        // REMOVED: PlayerPrefs.SetInt("SeenUtil_" + def.utilityName, 1);

        // Bind OK button
        if (newUtilityOkButton != null)
        {
            newUtilityOkButton.onClick.RemoveAllListeners();
            newUtilityOkButton.onClick.AddListener(HideNewUtilityPanel);
        }

        // Setup Slide-in
        newUtilityPanel.gameObject.SetActive(true);
        newUtilityPanel.anchoredPosition = new Vector2(newUtilityPanel.anchoredPosition.x, -2000f);

        if (newUtilitySfx != null) AudioService.PlayClip2D(newUtilitySfx, 0.5f);

        StartCoroutine(LerpPanel(newUtilityPanel, -2000f, 0f, false));
    }

     private void HideNewUtilityPanel()
    {
        if (newUtilityPanel != null)
            StartCoroutine(LerpPanel(newUtilityPanel, 0f, -2000f, true));
    }

    // ── Are You Sure? Panel ─────────────────────────────────────────────────────

    private void ShowAreYouSurePanel()
    {
        if (areYouSureYesButton != null)
        {
            areYouSureYesButton.onClick.RemoveAllListeners();
            areYouSureYesButton.onClick.AddListener(OnAreYouSureYes);
        }
        if (areYouSureNoButton != null)
        {
            areYouSureNoButton.onClick.RemoveAllListeners();
            areYouSureNoButton.onClick.AddListener(HideAreYouSurePanel);
        }

        areYouSurePanel.gameObject.SetActive(true);
        areYouSurePanel.anchoredPosition = new Vector2(areYouSurePanel.anchoredPosition.x, -2000f);

        // Play the same SFX as the new utility announcement
        if (newUtilitySfx != null) AudioService.PlayClip2D(newUtilitySfx, 0.5f);

        StartCoroutine(LerpPanel(areYouSurePanel, -2000f, 0f, false));
    }

    private void HideAreYouSurePanel()
    {
        if (areYouSurePanel != null)
            StartCoroutine(LerpPanel(areYouSurePanel, 0f, -2000f, true));
    }

    private void OnAreYouSureYes()
    {
        StartGameInternal();
    }

    private IEnumerator LerpPanel(RectTransform panel, float startY, float endY, bool hideAfter)
    {
        float elapsed = 0f;
        float duration = 0.5f;
        float x = panel.anchoredPosition.x;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f); // Smooth ease-out
            
            panel.anchoredPosition = new Vector2(x, Mathf.Lerp(startY, endY, eased));
            yield return null;
        }

        panel.anchoredPosition = new Vector2(x, endY);
        if (hideAfter) panel.gameObject.SetActive(false);
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
        RefreshEmptySlotLabels();
    }

    // ── Start Game ────────────────────────────────────────────────────────────

    public void OnStartGame()
    {
        // Check if any utilities are actually equipped
        bool hasAnyUtilities = false;
        for (int i = 0; i < availableUtilities.Length; i++)
        {
            if (_chosen[i] > 0)
            {
                hasAnyUtilities = true;
                break;
            }
        }

        if (!hasAnyUtilities && areYouSurePanel != null)
        {
            ShowAreYouSurePanel();
            return;
        }

        StartGameInternal();
    }

    private void StartGameInternal()
    {
        var entries = new List<LoadoutData.LoadoutEntry>();

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
        RefreshEmptySlotLabels();
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

    /// <summary>
    /// Shows "Click Here" (white) on empty slots if the currently previewed utility
    /// can fit. Shows "Unavailable" (red) if it doesn't fit.
    /// </summary>
    private readonly Color _unavailableRed = new Color(1f, 0f, 0f); // 255,0,0

    private void RefreshEmptySlotLabels()
    {
        int remainingSlots = totalSlots - SlotsUsed();
        int previewCost    = availableUtilities[_previewIndex].slotCost;
        bool canFit        = remainingSlots >= previewCost;

        for (int i = 0; i < traySlots.Length; i++)
        {
            if (traySlots[i].utilityIndex == -1 && traySlots[i].utilityText != null)
            {
                traySlots[i].utilityText.text  = canFit ? "Click Here" : "Unavailable";
                traySlots[i].utilityText.color = canFit ? Color.white : _unavailableRed;
            }
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