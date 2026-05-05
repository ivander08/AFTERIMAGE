using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class DebugHUD : MonoBehaviour
{
    public static DebugHUD Instance { get; private set; }

    public bool GodModeEnabled { get; private set; }

    static readonly Color BgPanel = new Color(0.06f, 0.06f, 0.08f, 0.96f);
    static readonly Color TextPri = new Color(0.92f, 0.92f, 0.95f, 1f);
    static readonly Color FpsGood = new Color(0.22f, 0.85f, 0.45f, 1f);
    static readonly Color FpsWarn = new Color(0.95f, 0.72f, 0.18f, 1f);
    static readonly Color FpsBad  = new Color(0.88f, 0.25f, 0.22f, 1f);

    bool  _panelOpen;
    bool  _showFps = true;
    float _fps;
    int   _frames;
    float _elapsed;

    GameObject   _canvas;
    GameObject   _fpsRoot;
    Text         _fpsLabel;
    GameObject   _panel;
    CanvasGroup  _panelCg;
    Text         _commandsText;

    static Font _font;

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

    static void LoadFont()
    {
        if (_font != null) return;
        foreach (var name in new[] { "Consolas", "Courier New", "Courier" })
        {
            var f = Font.CreateDynamicFontFromOSFont(name, 14);
            if (f != null) { _font = f; return; }
        }
        _font = Resources.GetBuiltinResource<Font>("Arial.ttf") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void BuildUI()
    {
        _canvas = new GameObject("DebugCanvas");
        _canvas.transform.SetParent(transform);
        var c = _canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 999;
        var cs = _canvas.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);

        BuildFpsCounter();
        BuildPanel();
    }

    void BuildFpsCounter()
    {
        var root = MakeRect("FPS", _canvas.transform);
        _fpsRoot = root.gameObject;
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0, 1);
        root.anchoredPosition = new Vector2(10, -10);
        root.sizeDelta = new Vector2(90, 22);

        var bg = root.gameObject.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.07f, 0.80f);

        _fpsLabel = MakeText(root, "FPS --", 13, TextAnchor.MiddleCenter, TextPri);
        _fpsLabel.rectTransform.anchorMin = Vector2.zero;
        _fpsLabel.rectTransform.anchorMax = Vector2.one;
        _fpsLabel.rectTransform.offsetMin = _fpsLabel.rectTransform.offsetMax = Vector2.zero;
    }

    void BuildPanel()
    {
        var rt = MakeRect("DebugPanel", _canvas.transform);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(10, -36);  
        rt.sizeDelta = new Vector2(240, 120);           
        _panel = rt.gameObject;

        var bg = _panel.AddComponent<Image>();
        bg.color = BgPanel;

        _panelCg = _panel.AddComponent<CanvasGroup>();
        _panelCg.alpha = 0f;

        var header = MakeText(rt, "DEBUG (F3 to close)", 11, TextAnchor.UpperLeft, new Color(0.55f, 0.55f, 0.60f, 1f));
        header.rectTransform.anchoredPosition = new Vector2(10, -10);
        header.rectTransform.sizeDelta = new Vector2(220, 20);

        _commandsText = MakeText(rt, "", 13, TextAnchor.UpperLeft, TextPri);
        _commandsText.supportRichText = true;
        _commandsText.rectTransform.anchoredPosition = new Vector2(10, -35);
        _commandsText.rectTransform.sizeDelta = new Vector2(220, 100);

        RefreshCommandText();
        _panel.SetActive(false);
    }

    static RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1); 
        rt.pivot = new Vector2(0, 1);
        return rt;
    }

    Text MakeText(RectTransform parent, string content, int size, TextAnchor align, Color color)
    {
        var go = new GameObject("T");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = _font;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.text = content;
        return t;
    }

    void RefreshCommandText()
    {
        _commandsText.text = 
            $"[1] Toggle FPS: {(_showFps ? "<color=#38d973>ON</color>" : "<color=#e04038>OFF</color>")}\n" +
            $"[2] God Mode: {(GodModeEnabled ? "<color=#38d973>ON</color>" : "<color=#e04038>OFF</color>")}\n" +
            "[3] Kill Enemies (Skip)\n" +
            "[4] Skip to Boss";
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
        {
            SetPanel(!_panelOpen);
        }

        if (_panelOpen && Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                _showFps = !_showFps;
                _fpsRoot.SetActive(_showFps);
                RefreshCommandText();
            }
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                GodModeEnabled = !GodModeEnabled;
                RefreshCommandText();
            }
            if (Keyboard.current.digit3Key.wasPressedThisFrame) OnSkipLevel();
            if (Keyboard.current.digit4Key.wasPressedThisFrame) OnSkipBoss();
        }

        if (_showFps)
        {
            _frames++;
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed >= 0.25f)
            {
                _fps = _frames / _elapsed;
                _frames = 0;
                _elapsed = 0f;
                _fpsLabel.text = $"FPS {_fps:F0}";
                _fpsLabel.color = _fps >= 55f ? FpsGood : _fps >= 30f ? FpsWarn : FpsBad;
            }
        }
    }

    void SetPanel(bool open)
    {
        _panelOpen = open;
        if (open)
        {
            _panel.SetActive(true);
            _panelCg.alpha = 0f;
            StartCoroutine(FadeTo(1f));
        }
        else
        {
            StartCoroutine(FadeTo(0f, deactivateAfter: true));
        }
    }

    System.Collections.IEnumerator FadeTo(float target, bool deactivateAfter = false)
    {
        float start = _panelCg.alpha;
        float elapsed = 0f;
        while (elapsed < 0.12f)
        {
            elapsed += Time.unscaledDeltaTime;
            _panelCg.alpha = Mathf.Lerp(start, target, elapsed / 0.12f);
            yield return null;
        }
        _panelCg.alpha = target;
        if (deactivateAfter) _panel.SetActive(false);
    }

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
        SetPanel(false);
    }

    void OnSkipBoss()
    {
        if (EchoArenaController.Instance != null)
        {
            SetPanel(false);
            EchoArenaController.Instance.SkipToBossEncounter();
        }
    }
}
