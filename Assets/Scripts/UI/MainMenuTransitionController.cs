using System.Collections;
using UnityEngine;

public class MainMenuTransitionController : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private RectTransform mainMenuContainer;
    [SerializeField] private RectTransform aboutContainer;
    [SerializeField] private Light mainMenuPointLight;

    [Header("Transition")]
    [SerializeField] private float slideDuration = 0.35f;
    [SerializeField] private AnimationCurve transitionEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float depthOffset = 100000f;
    [SerializeField] private float aboutVerticalOffset = 2000f;
    [SerializeField] private bool disableHiddenPanel = true;

    private Vector3 _mainVisiblePosition;
    private Vector3 _mainHiddenPosition;
    private Vector3 _aboutVisiblePosition;
    private Vector3 _aboutHiddenPosition;
    private float _mainMenuLightVisibleIntensity;

    private Coroutine _transitionRoutine;
    private bool _isTransitioning;
    private bool _hasCachedPositions;

    private void Awake()
    {
        if (mainMenuPointLight == null && mainMenuContainer != null)
            mainMenuPointLight = mainMenuContainer.GetComponentInChildren<Light>(true);
    }

    private void Start()
    {
        InitializeDefaultState();
    }

    public void InitializeDefaultState()
    {
        if (mainMenuContainer == null || aboutContainer == null)
            return;

        CachePositionsIfNeeded();

        mainMenuContainer.gameObject.SetActive(true);
        mainMenuContainer.localPosition = _mainVisiblePosition;

        if (mainMenuPointLight != null)
            mainMenuPointLight.intensity = _mainMenuLightVisibleIntensity;

        aboutContainer.gameObject.SetActive(true);
        aboutContainer.localPosition = _aboutHiddenPosition;

        if (disableHiddenPanel)
            aboutContainer.gameObject.SetActive(false);
    }

    public void ShowAbout()
    {
        if (_isTransitioning)
            return;

        if (mainMenuContainer == null || aboutContainer == null)
            return;

        CachePositionsIfNeeded();

        EnsurePanelEnterStart(aboutContainer, _aboutHiddenPosition);
        aboutContainer.SetAsLastSibling();

        StartTransition(
            fromPanel: mainMenuContainer,
            fromTarget: _mainHiddenPosition,
            toPanel: aboutContainer,
            toTarget: _aboutVisiblePosition,
            disableFromPanelOnComplete: disableHiddenPanel
        );
    }

    public void ShowMainMenu()
    {
        if (_isTransitioning)
            return;

        if (mainMenuContainer == null || aboutContainer == null)
            return;

        CachePositionsIfNeeded();

        EnsurePanelEnterStart(mainMenuContainer, _mainHiddenPosition);
        mainMenuContainer.SetAsLastSibling();

        StartTransition(
            fromPanel: aboutContainer,
            fromTarget: _aboutHiddenPosition,
            toPanel: mainMenuContainer,
            toTarget: _mainVisiblePosition,
            disableFromPanelOnComplete: disableHiddenPanel
        );
    }

    private void EnsurePanelEnterStart(RectTransform panel, Vector3 startPosition)
    {
        if (panel == null)
            return;

        if (!panel.gameObject.activeSelf)
        {
            panel.gameObject.SetActive(true);
            panel.localPosition = startPosition;
        }
    }

    private void StartTransition(
        RectTransform fromPanel,
        Vector3 fromTarget,
        RectTransform toPanel,
        Vector3 toTarget,
        bool disableFromPanelOnComplete)
    {
        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);

        _transitionRoutine = StartCoroutine(SlideRoutine(fromPanel, fromTarget, toPanel, toTarget, disableFromPanelOnComplete));
    }

    private IEnumerator SlideRoutine(
        RectTransform fromPanel,
        Vector3 fromTarget,
        RectTransform toPanel,
        Vector3 toTarget,
        bool disableFromPanelOnComplete)
    {
        _isTransitioning = true;

        Vector3 fromStart = fromPanel != null ? fromPanel.localPosition : Vector3.zero;
        Vector3 toStart = toPanel != null ? toPanel.localPosition : Vector3.zero;
        float lightStart = mainMenuPointLight != null ? mainMenuPointLight.intensity : 0f;
        float lightTarget = 0f;

        if (fromPanel == mainMenuContainer)
            lightTarget = 0f;
        else if (toPanel == mainMenuContainer)
            lightTarget = _mainMenuLightVisibleIntensity;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, slideDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = transitionEase != null ? Mathf.Clamp01(transitionEase.Evaluate(t)) : t;

            if (fromPanel != null)
                fromPanel.localPosition = Vector3.LerpUnclamped(fromStart, fromTarget, eased);

            if (toPanel != null)
                toPanel.localPosition = Vector3.LerpUnclamped(toStart, toTarget, eased);

            if (mainMenuPointLight != null)
                mainMenuPointLight.intensity = Mathf.LerpUnclamped(lightStart, lightTarget, eased);

            yield return null;
        }

        if (fromPanel != null)
            fromPanel.localPosition = fromTarget;

        if (toPanel != null)
            toPanel.localPosition = toTarget;

        if (mainMenuPointLight != null)
            mainMenuPointLight.intensity = lightTarget;

        if (disableFromPanelOnComplete && fromPanel != null)
            fromPanel.gameObject.SetActive(false);

        _isTransitioning = false;
        _transitionRoutine = null;
    }

    private void CachePositionsIfNeeded()
    {
        if (_hasCachedPositions)
            return;

        _mainVisiblePosition = mainMenuContainer.localPosition;
        _aboutVisiblePosition = aboutContainer.localPosition;
        _mainMenuLightVisibleIntensity = mainMenuPointLight != null ? mainMenuPointLight.intensity : 0f;

        float safeDepthOffset = Mathf.Max(1f, depthOffset);
        float safeAboutVerticalOffset = Mathf.Max(1f, aboutVerticalOffset);
        _mainHiddenPosition = _mainVisiblePosition + new Vector3(0f, 0f, -safeDepthOffset);
        _aboutHiddenPosition = _aboutVisiblePosition + new Vector3(0f, safeAboutVerticalOffset, 0f);

        _hasCachedPositions = true;
    }
}
