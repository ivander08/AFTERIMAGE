using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// In-game debug overlay with FPS counter, God Mode toggle, and Skip Level button.
/// Press F3 to toggle the debug panel visibility.
/// Attach this to any GameObject in the scene (e.g., the Player or a DDOL manager).
/// </summary>
public class DebugHUD : MonoBehaviour
{
    public static DebugHUD Instance { get; private set; }

    // No serialized KeyCode — we check for F3 via Keyboard.current in Update()

    [Header("FPS Settings")]
    public float fpsUpdateInterval = 0.25f;
    public int fpsFontSize = 16;
    public Color fpsColor = Color.green;

    [Header("Debug Panel")]
    public int panelFontSize = 14;
    public Color panelBgColor = new Color(0f, 0f, 0f, 0.75f);

    // State
    private bool _showDebugPanel = false;
    private bool _showFps = true;
    private float _fps = 0f;
    private int _framesAccumulated = 0;
    private float _timeAccumulated = 0f;

    // UI references (created at runtime)
    private GameObject _canvasObject;
    private GameObject _fpsTextObject;
    private Text _fpsText;
    private GameObject _panelObject;
    private Text _godModeLabel;
    private Toggle _godModeToggle;
    private Button _skipLevelButton;
    private Button _closeButton;

    /// <summary>
    /// Automatically creates the DebugHUD instance when any scene loads,
    /// so you don't need to manually attach it to any GameObject.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("DebugHUD");
            DontDestroyOnLoad(go);
            go.AddComponent<DebugHUD>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateDebugUI();
    }

    private void CreateDebugUI()
    {
        // --- Canvas ---
        _canvasObject = new GameObject("DebugHUDCanvas");
        _canvasObject.transform.SetParent(transform);

        Canvas canvas = _canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Very high so it renders on top of everything

        CanvasScaler scaler = _canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        _canvasObject.AddComponent<GraphicRaycaster>();

        // --- FPS Text (top-left corner) ---
        _fpsTextObject = new GameObject("FpsText");
        _fpsTextObject.transform.SetParent(_canvasObject.transform);

        RectTransform fpsRt = _fpsTextObject.AddComponent<RectTransform>();
        fpsRt.anchorMin = new Vector2(0, 1);
        fpsRt.anchorMax = new Vector2(0, 1);
        fpsRt.pivot = new Vector2(0, 1);
        fpsRt.anchoredPosition = new Vector2(10, -10);
        fpsRt.sizeDelta = new Vector2(200, 30);

        _fpsText = _fpsTextObject.AddComponent<Text>();
        _fpsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _fpsText.fontSize = fpsFontSize;
        _fpsText.color = fpsColor;
        _fpsText.alignment = TextAnchor.UpperLeft;
        _fpsText.text = "FPS: --";
        _fpsText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _fpsText.verticalOverflow = VerticalWrapMode.Overflow;

        // --- Debug Panel (center of screen) ---
        _panelObject = new GameObject("DebugPanel");
        _panelObject.transform.SetParent(_canvasObject.transform);

        RectTransform panelRt = _panelObject.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(300, 260);

        // Panel background
        Image panelBg = _panelObject.AddComponent<Image>();
        panelBg.color = panelBgColor;

        // Vertical layout for panel contents
        VerticalLayoutGroup layout = _panelObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // --- Title ---
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(_panelObject.transform);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = panelFontSize + 2;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.text = "DEBUG MENU";
        LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
        titleLayout.minHeight = 25;

        // --- FPS Toggle ---
        GameObject fpsToggleObj = CreateToggle(_panelObject.transform, "Show FPS", _showFps, (on) =>
        {
            _showFps = on;
            if (_fpsTextObject != null) _fpsTextObject.SetActive(on);
        });
        fpsToggleObj.name = "FpsToggle";

        // --- God Mode Toggle ---
        GameObject godModeToggleObj = CreateToggle(_panelObject.transform, "God Mode", false, (on) =>
        {
            PlayerHealth player = FindObjectOfType<PlayerHealth>();
            if (player != null)
            {
                player.godMode = on;
                Debug.Log($"[DebugHUD] God Mode = {(on ? "ON" : "OFF")}");
            }
            else
            {
                Debug.LogWarning("[DebugHUD] No PlayerHealth found in scene!");
            }
        });
        godModeToggleObj.name = "GodModeToggle";

        // --- Skip Level Button ---
        GameObject skipBtnObj = new GameObject("SkipLevelButton");
        skipBtnObj.transform.SetParent(_panelObject.transform);

        _skipLevelButton = skipBtnObj.AddComponent<Button>();
        Image skipBtnBg = skipBtnObj.AddComponent<Image>();
        skipBtnBg.color = new Color(0.2f, 0.4f, 0.8f, 1f);

        GameObject skipBtnTextObj = new GameObject("Text");
        skipBtnTextObj.transform.SetParent(skipBtnObj.transform);
        Text skipBtnText = skipBtnTextObj.AddComponent<Text>();
        skipBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        skipBtnText.fontSize = panelFontSize;
        skipBtnText.color = Color.white;
        skipBtnText.alignment = TextAnchor.MiddleCenter;
        skipBtnText.text = "SKIP LEVEL";
        RectTransform skipBtnTextRt = skipBtnTextObj.GetComponent<RectTransform>();
        skipBtnTextRt.anchorMin = Vector2.zero;
        skipBtnTextRt.anchorMax = Vector2.one;
        skipBtnTextRt.sizeDelta = Vector2.zero;

        LayoutElement skipLayout = skipBtnObj.AddComponent<LayoutElement>();
        skipLayout.minHeight = 35;

        _skipLevelButton.onClick.AddListener(OnSkipLevelClicked);

        // --- Skip to Boss Button (Level 6 debug) ---
        GameObject skipBossBtnObj = new GameObject("SkipToBossButton");
        skipBossBtnObj.transform.SetParent(_panelObject.transform);

        Button skipBossBtn = skipBossBtnObj.AddComponent<Button>();
        Image skipBossBg = skipBossBtnObj.AddComponent<Image>();
        skipBossBg.color = new Color(0.6f, 0.2f, 0.6f, 1f);

        GameObject skipBossTextObj = new GameObject("Text");
        skipBossTextObj.transform.SetParent(skipBossBtnObj.transform);
        Text skipBossText = skipBossTextObj.AddComponent<Text>();
        skipBossText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        skipBossText.fontSize = panelFontSize;
        skipBossText.color = Color.white;
        skipBossText.alignment = TextAnchor.MiddleCenter;
        skipBossText.text = "SKIP TO BOSS";
        RectTransform skipBossTextRt = skipBossTextObj.GetComponent<RectTransform>();
        skipBossTextRt.anchorMin = Vector2.zero;
        skipBossTextRt.anchorMax = Vector2.one;
        skipBossTextRt.sizeDelta = Vector2.zero;

        LayoutElement skipBossLayout = skipBossBtnObj.AddComponent<LayoutElement>();
        skipBossLayout.minHeight = 35;

        skipBossBtn.onClick.AddListener(OnSkipToBossClicked);

        // --- Close Button ---
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(_panelObject.transform);

        _closeButton = closeBtnObj.AddComponent<Button>();
        Image closeBtnBg = closeBtnObj.AddComponent<Image>();
        closeBtnBg.color = new Color(0.6f, 0.2f, 0.2f, 1f);

        GameObject closeBtnTextObj = new GameObject("Text");
        closeBtnTextObj.transform.SetParent(closeBtnObj.transform);
        Text closeBtnText = closeBtnTextObj.AddComponent<Text>();
        closeBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        closeBtnText.fontSize = panelFontSize;
        closeBtnText.color = Color.white;
        closeBtnText.alignment = TextAnchor.MiddleCenter;
        closeBtnText.text = "CLOSE";
        RectTransform closeBtnTextRt = closeBtnTextObj.GetComponent<RectTransform>();
        closeBtnTextRt.anchorMin = Vector2.zero;
        closeBtnTextRt.anchorMax = Vector2.one;
        closeBtnTextRt.sizeDelta = Vector2.zero;

        LayoutElement closeLayout = closeBtnObj.AddComponent<LayoutElement>();
        closeLayout.minHeight = 30;

        _closeButton.onClick.AddListener(() => _showDebugPanel = false);

        // Initially hide the panel
        _panelObject.SetActive(false);

        // Support FPS toggle via the text object visibility
        if (!_showFps) _fpsTextObject.SetActive(false);
    }

    private GameObject CreateToggle(Transform parent, string label, bool initialState, System.Action<bool> onValueChanged)
    {
        GameObject toggleObj = new GameObject("Toggle_" + label);
        toggleObj.transform.SetParent(parent);

        Toggle toggle = toggleObj.AddComponent<Toggle>();
        Image toggleBg = toggleObj.AddComponent<Image>();
        toggleBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Horizontal layout: label | toggle box
        HorizontalLayoutGroup hLayout = toggleObj.AddComponent<HorizontalLayoutGroup>();
        hLayout.padding = new RectOffset(8, 8, 2, 2);
        hLayout.spacing = 8;
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = false;
        hLayout.childForceExpandWidth = true;
        hLayout.childForceExpandHeight = false;

        LayoutElement toggleLayout = toggleObj.AddComponent<LayoutElement>();
        toggleLayout.minHeight = 30;

        // Label text
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(toggleObj.transform);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = panelFontSize;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.text = label;

        // Toggle graphic (the checkmark box)
        GameObject toggleGraphic = new GameObject("Checkmark");
        toggleGraphic.transform.SetParent(toggleObj.transform);
        Image checkImage = toggleGraphic.AddComponent<Image>();
        checkImage.color = Color.white;

        // Create a simple checkmark graphic programmatically
        GameObject checkMark = new GameObject("CheckMarkIcon");
        checkMark.transform.SetParent(toggleGraphic.transform);
        Text checkText = checkMark.AddComponent<Text>();
        checkText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        checkText.fontSize = panelFontSize;
        checkText.color = Color.green;
        checkText.alignment = TextAnchor.MiddleCenter;
        checkText.text = "✓";
        RectTransform checkRt = checkMark.GetComponent<RectTransform>();
        checkRt.anchorMin = Vector2.zero;
        checkRt.anchorMax = Vector2.one;
        checkRt.sizeDelta = Vector2.zero;

        toggle.graphic = checkImage;
        toggle.targetGraphic = toggleBg;
        toggle.isOn = initialState;
        toggle.onValueChanged.AddListener((on) => onValueChanged?.Invoke(on));

        // Set the checkmark visibility based on initial state
        if (!initialState) checkMark.SetActive(false);
        toggle.onValueChanged.AddListener((on) => checkMark.SetActive(on));

        // Set the toggle graphic size
        LayoutElement graphicLayout = toggleGraphic.AddComponent<LayoutElement>();
        graphicLayout.minWidth = 20;

        return toggleObj;
    }

    private void Update()
    {
        // Toggle debug panel with F3 (using new Input System)
        if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
        {
            _showDebugPanel = !_showDebugPanel;
            if (_panelObject != null) _panelObject.SetActive(_showDebugPanel);
        }

        // FPS calculation
        _framesAccumulated++;
        _timeAccumulated += Time.unscaledDeltaTime;

        if (_timeAccumulated >= fpsUpdateInterval)
        {
            _fps = _framesAccumulated / _timeAccumulated;
            _framesAccumulated = 0;
            _timeAccumulated = 0f;

            if (_fpsText != null)
            {
                _fpsText.text = $"FPS: {_fps:F1}";
            }
        }

        // Keep FPS text color from green -> yellow -> red based on performance
        if (_fpsText != null)
        {
            if (_fps >= 55f) _fpsText.color = Color.green;
            else if (_fps >= 30f) _fpsText.color = Color.yellow;
            else _fpsText.color = Color.red;
        }
    }

    private void OnSkipLevelClicked()
    {
        Debug.Log("[DebugHUD] Skipping to next level...");

        string currentScene = SceneManager.GetActiveScene().name;
        GameProgressManager progress = GameProgressManager.Instance;

        if (progress != null)
        {
            // Unlock the audio service first (in case it's locked by level complete)
            AudioService.SetLock(false);

            // Resume time if paused
            if (PausePanelController.IsPaused)
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;
            }

            progress.CompleteCurrentLevel(currentScene);
        }
        else
        {
            Debug.LogWarning("[DebugHUD] No GameProgressManager found. Reloading current scene as fallback.");
            SceneManager.LoadScene(currentScene);
        }
    }

    private void OnSkipToBossClicked()
    {
        if (EchoArenaController.Instance == null)
        {
            Debug.LogWarning("[DebugHUD] EchoArenaController not found — not on Level 6 boss room.");
            return;
        }

        _showDebugPanel = false;
        if (_panelObject != null) _panelObject.SetActive(false);

        EchoArenaController.Instance.SkipToBossEncounter();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
