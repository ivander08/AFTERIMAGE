using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// In-game debug overlay. Press F3 to toggle.
/// Auto-initializes via RuntimeInitializeOnLoadMethod — no manual setup needed.
/// </summary>
public class DebugHUD : MonoBehaviour
{
    public static DebugHUD Instance { get; private set; }

    // ── Colors ────────────────────────────────────────────────────────────────
    static readonly Color BgPanel    = new Color(0.06f, 0.06f, 0.08f, 0.96f);
    static readonly Color BgRow      = new Color(1f, 1f, 1f, 0.04f);
    static readonly Color BgBtn      = new Color(1f, 1f, 1f, 0.08f);
    static readonly Color BgBtnDanger= new Color(0.8f, 0.2f, 0.2f, 0.55f);
    static readonly Color Accent     = new Color(0.18f, 0.72f, 0.95f, 1f);
    static readonly Color TextPri    = new Color(0.92f, 0.92f, 0.95f, 1f);
    static readonly Color TextMuted  = new Color(0.55f, 0.55f, 0.60f, 1f);
    static readonly Color TrackOff   = new Color(0.25f, 0.25f, 0.30f, 1f);

    // ── FPS thresholds ────────────────────────────────────────────────────────
    static readonly Color FpsGood = new Color(0.22f, 0.85f, 0.45f, 1f);
    static readonly Color FpsWarn = new Color(0.95f, 0.72f, 0.18f, 1f);
    static readonly Color FpsBad  = new Color(0.88f, 0.25f, 0.22f, 1f);

    // ── State ─────────────────────────────────────────────────────────────────
    bool  _panelOpen;
    float _fps;
    int   _frames;
    float _elapsed;

    // ── UI refs ───────────────────────────────────────────────────────────────
    GameObject   _canvas;
    Text         _fpsLabel;
    GameObject   _panel;
    CanvasGroup  _panelCg;

    // ── Font ──────────────────────────────────────────────────────────────────
    static Font _font;

    // ─────────────────────────────────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Instance != null) return;
        var go = new GameObject("DebugHUD");
        DontDestroyOnLoad(go);
        go.AddComponent<DebugHUD>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadFont();
        BuildUI();
    }

    // ── Font resolution ───────────────────────────────────────────────────────
    static void LoadFont()
    {
        if (_font != null) return;
        foreach (var name in new[] { "Consolas", "Courier New", "Courier" })
        {
            var f = Font.CreateDynamicFontFromOSFont(name, 14);
            if (f != null) { _font = f; return; }
        }
        _font = Resources.GetBuiltinResource<Font>("Arial.ttf")
             ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    // ── Build all UI ──────────────────────────────────────────────────────────
    void BuildUI()
    {
        // Canvas
        _canvas = new GameObject("DebugCanvas");
        _canvas.transform.SetParent(transform);
        var c = _canvas.AddComponent<Canvas>();
        c.renderMode   = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 999;
        var cs = _canvas.AddComponent<CanvasScaler>();
        cs.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;
        _canvas.AddComponent<GraphicRaycaster>();

        BuildFpsCounter();
        BuildPanel();
    }

    // ── FPS counter (top-left, minimal) ───────────────────────────────────────
    void BuildFpsCounter()
    {
        var root = MakeRect("FPS", _canvas.transform);
        Anchor(root, 0, 1, 0, 1);
        root.pivot = new Vector2(0, 1);
        root.anchoredPosition = new Vector2(10, -10);
        root.sizeDelta = new Vector2(90, 22);

        var bg = root.gameObject.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.07f, 0.80f);

        _fpsLabel = MakeText(root, "FPS --", 13, TextAnchor.MiddleCenter, TextPri);
        FullStretch(_fpsLabel.rectTransform);
    }

    // ── Debug panel ───────────────────────────────────────────────────────────
    void BuildPanel()
    {
        // Root
        var rt = MakeRect("DebugPanel", _canvas.transform);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(10, -36);  // sits below FPS counter
        rt.sizeDelta = new Vector2(240, 0);           // height set by content below
        _panel = rt.gameObject;

        var bg = _panel.AddComponent<Image>();
        bg.color = BgPanel;

        _panelCg = _panel.AddComponent<CanvasGroup>();
        _panelCg.alpha = 0f;

        // ── Content (manual stacking, no LayoutGroup fighting you) ────────────
        float y = -10f;
        const float rowH   = 30f;
        const float btnH   = 28f;
        const float gap    = 4f;
        const float indent = 12f;

        // Header label
        var header = MakeText(rt, "DEBUG", 11, TextAnchor.MiddleLeft, TextMuted);
        PlaceChild(header.rectTransform, indent, y, 220, 20);
        y -= 24f;

        // Divider
        MakeDivider(rt, y + 8, indent);
        y -= 4f;

        // Toggles
        MakeToggleRow(rt, ref y, "Show FPS",  rowH, gap, indent, true,  isOn =>
        {
            // fpsLabel always exists; just alpha the whole FPS root
        });

        MakeToggleRow(rt, ref y, "God Mode", rowH, gap, indent, false, isOn =>
        {
            var ph = FindObjectOfType<PlayerHealth>();
            if (ph) ph.godMode = isOn;
            else Debug.LogWarning("[DebugHUD] PlayerHealth not found.");
        });

        // Spacer
        y -= 6f;
        MakeDivider(rt, y, indent);
        y -= 8f;

        // Buttons
        MakeButton(rt, ref y, "Skip Level",        btnH, gap, indent, BgBtn,       OnSkipLevel);
        MakeButton(rt, ref y, "Skip to Boss (L6)", btnH, gap, indent, BgBtn,       OnSkipBoss);
        MakeButton(rt, ref y, "Close",             btnH, gap, indent, BgBtnDanger, OnClose);

        y -= 10f;

        // Final height
        rt.sizeDelta = new Vector2(240, -y);

        _panel.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Row helpers
    // ─────────────────────────────────────────────────────────────────────────

    void MakeToggleRow(RectTransform parent, ref float y,
                       string label, float h, float gap, float indent,
                       bool initial, System.Action<bool> cb)
    {
        float panelW = parent.sizeDelta.x;
        float rowW   = panelW - indent * 2;

        // Row background
        var rowRt = MakeRect("Row_" + label, parent);
        PlaceChild(rowRt, indent, y, rowW, h);
        var rowBg = rowRt.gameObject.AddComponent<Image>();
        rowBg.color = BgRow;

        // Label
        var lbl = MakeText(rowRt, label, 13, TextAnchor.MiddleLeft, TextPri);
        PlaceChild(lbl.rectTransform, 10, 0, rowW - 60, h);

        // Switch track — y is negative because pivot is top-left (y goes downward)
        const float trackW = 32f, trackH = 16f;
        var trackRt = MakeRect("Track", rowRt);
        PlaceChild(trackRt, rowW - 10 - trackW, -((h - trackH) * 0.5f), trackW, trackH);
        var trackImg = trackRt.gameObject.AddComponent<Image>();
        trackImg.color = initial ? Accent : TrackOff;

        // Thumb — centered vertically inside track using middle-left anchor
        const float thumbSz = 12f;
        var thumbGo = new GameObject("Thumb");
        thumbGo.transform.SetParent(trackRt, false);
        var thumbRt = thumbGo.AddComponent<RectTransform>();
        thumbRt.anchorMin = new Vector2(0, 0.5f);
        thumbRt.anchorMax = new Vector2(0, 0.5f);
        thumbRt.pivot     = new Vector2(0.5f, 0.5f);
        thumbRt.sizeDelta = new Vector2(thumbSz, thumbSz);
        thumbRt.anchoredPosition = new Vector2(initial ? trackW - thumbSz * 0.5f - 2 : thumbSz * 0.5f + 2, 0);
        var thumbImg = thumbGo.AddComponent<Image>();
        thumbImg.color = Color.white;

        // Toggle component on the row object
        var toggle = rowRt.gameObject.AddComponent<Toggle>();
        toggle.isOn          = initial;
        toggle.targetGraphic = rowBg;
        toggle.graphic       = thumbImg; // Unity needs this to detect the toggle region
        toggle.onValueChanged.AddListener(isOn =>
        {
            trackImg.color = isOn ? Accent : TrackOff;
            thumbRt.anchoredPosition = new Vector2(isOn ? trackW - thumbSz * 0.5f - 2 : thumbSz * 0.5f + 2, 0);
            cb?.Invoke(isOn);
        });

        y -= h + gap;
    }

    void MakeButton(RectTransform parent, ref float y,
                    string label, float h, float gap, float indent,
                    Color bgColor, System.Action onClick)
    {
        float panelW = parent.sizeDelta.x;
        float btnW   = panelW - indent * 2;

        var btnRt = MakeRect("Btn_" + label, parent);
        PlaceChild(btnRt, indent, y, btnW, h);

        var bgImg = btnRt.gameObject.AddComponent<Image>();
        bgImg.color = bgColor;

        var btn = btnRt.gameObject.AddComponent<Button>();
        btn.targetGraphic = bgImg;
        var cb = btn.colors;
        cb.normalColor      = bgColor;
        cb.highlightedColor = new Color(bgColor.r + 0.12f, bgColor.g + 0.12f, bgColor.b + 0.12f, bgColor.a + 0.1f);
        cb.pressedColor     = new Color(bgColor.r - 0.08f, bgColor.g - 0.08f, bgColor.b - 0.08f, 1f);
        cb.colorMultiplier  = 1f;
        cb.fadeDuration     = 0.08f;
        btn.colors          = cb;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var txt = MakeText(btnRt, label, 12, TextAnchor.MiddleCenter, TextPri);
        FullStretch(txt.rectTransform);

        y -= h + gap;
    }

    void MakeDivider(RectTransform parent, float y, float indent)
    {
        float w = parent.sizeDelta.x - indent * 2;
        var div = MakeRect("Divider", parent);
        PlaceChild(div, indent, y, w, 1);
        var img = div.gameObject.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.08f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Primitive helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// Create a RectTransform child with top-left anchor (for manual placement).
    static RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1); // top-left
        rt.pivot = new Vector2(0, 1);
        return rt;
    }

    /// Position a child rect relative to parent's top-left corner.
    static void PlaceChild(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    /// Stretch a rect to fill its parent completely.
    static void FullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    /// Set all four anchors to the same point.
    static void Anchor(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
    }

    Text MakeText(RectTransform parent, string content, int size,
                  TextAnchor align, Color color)
    {
        var go = new GameObject("T");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font      = _font;
        t.fontSize  = size;
        t.color     = color;
        t.alignment = align;
        t.text      = content;
        t.supportRichText = false;
        return t;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Runtime
    // ─────────────────────────────────────────────────────────────────────────

    void Update()
    {
        // Toggle panel
        if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            SetPanel(!_panelOpen);

        // FPS
        _frames++;
        _elapsed += Time.unscaledDeltaTime;
        if (_elapsed >= 0.25f)
        {
            _fps     = _frames / _elapsed;
            _frames  = 0;
            _elapsed = 0f;
            UpdateFpsLabel();
        }
    }

    void UpdateFpsLabel()
    {
        if (_fpsLabel == null) return;
        _fpsLabel.text  = $"FPS {_fps:F0}";
        _fpsLabel.color = _fps >= 55f ? FpsGood : _fps >= 30f ? FpsWarn : FpsBad;
    }

    void SetPanel(bool open)
    {
        _panelOpen = open;
        if (open)
        {
            _panel.SetActive(true);
            _panelCg.alpha = 0f;
            StartCoroutine(FadeTo(1f));
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
        else
        {
            StartCoroutine(FadeTo(0f, deactivateAfter: true));
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = false;
        }
    }

    System.Collections.IEnumerator FadeTo(float target, bool deactivateAfter = false)
    {
        float start   = _panelCg.alpha;
        float elapsed = 0f;
        const float dur = 0.12f;
        while (elapsed < dur)
        {
            elapsed      += Time.unscaledDeltaTime;
            _panelCg.alpha = Mathf.Lerp(start, target, elapsed / dur);
            yield return null;
        }
        _panelCg.alpha = target;
        if (deactivateAfter) _panel.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Actions
    // ─────────────────────────────────────────────────────────────────────────

    void OnSkipLevel()
    {
        if (PausePanelController.IsPaused) { Time.timeScale = 1f; AudioListener.pause = false; }

        int killed = 0, prev = -1;
        while (killed != prev)
        {
            prev = killed;
            foreach (var e in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
                if (e != null && !e.IsDead) { e.ForceKill(); killed++; }
        }
        Debug.Log($"[DebugHUD] Killed {killed} enemies.");
        SetPanel(false);
    }

    void OnSkipBoss()
    {
        if (EchoArenaController.Instance == null)
        {
            Debug.LogWarning("[DebugHUD] EchoArenaController not found.");
            return;
        }
        SetPanel(false);
        EchoArenaController.Instance.SkipToBossEncounter();
    }

    void OnClose() => SetPanel(false);

    void OnDestroy() { if (Instance == this) Instance = null; }
}