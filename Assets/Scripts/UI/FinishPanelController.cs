// Assets/Scripts/UI/FinishPanelController.cs
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class FinishPanelController : MonoBehaviour
{
    public static FinishPanelController Instance { get; private set; }
    public static bool IsFinished => Instance != null && (Instance._isShown || Instance._isTransitioning);

    [Header("UI References")]
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI timeText;
    public Button mainMenuButton;
    public Button continueButton;

    [Header("Scene Routing")]
    public string mainMenuSceneName = "MainMenu";
    public string nextLevelSceneName = "";

    [Header("Completion")]
    public float checkInterval = 0.5f;

    [Header("Audio")]
    public AudioClip finishPanelSfx;
    [Range(0f, 1f)] public float finishPanelSfxVolume = 0.2f;
    public AudioClip level0CompletionSfx;[Range(0f, 1f)] public float level0CompletionSfxVolume = 0.25f;[Header("Level 0 Finale")]
    public float captionStartWaitTimeout = 3f;
    public float level0ZoomDistance = 10f; 
    public float level0ZoomSpeed = 5.5f;

    [Header("Non-Level 0 Finale")]
    public RectTransform completePanel;
    public float completePanelLerpDuration = 0.75f; // How fast it slides in from -2500
    public float completeWaitDuration = 4.0f;       // "X seconds" to slowly zoom
    public CinemachineCamera finishCamera;
    public float completeZoomDistance = 14f;

    [Header("Panel Animation")]
    public float panelRevealDuration = 0.45f;
    public float panelStartScale = 1.1f;
    public float scoreTallyDuration = 1.5f;
    public AudioClip scoreTallyTickSound;

    private CanvasGroup _canvasGroup;
    private bool _isLevel0;
    private bool _isShown;
    private bool _isTransitioning;
    private float _runStartTime;
    private Vector3 _originalScale;
    
    // Dedicated audio source to prevent the score tally from stealing slots
    private AudioSource _audioSource; 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        
        _originalScale = transform.localScale;

        if (completePanel != null)
        {
            completePanel.gameObject.SetActive(false);
        }

        // Setup dedicated audio source
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;

        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
    }

    private void Start()
    {
        _isLevel0 = SceneManager.GetActiveScene().name == "Level0";
        StartCoroutine(TrackRunAndCompletionRoutine());
    }

    private IEnumerator TrackRunAndCompletionRoutine()
    {
        while (PreGamePanel.IsPlaying)
        {
            yield return null;
        }

        _runStartTime = Time.time;

        while (!_isShown && !_isTransitioning)
        {
            if (IsLevelComplete())
            {
                yield return StartCoroutine(HandleCompletionSequenceRoutine());
                yield break;
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }

    private bool IsLevelComplete()
    {
        Room[] rooms = FindObjectsOfType<Room>();
        if (rooms == null || rooms.Length == 0) return false;

        bool hasAnyEnemyInLevel = false;

        for (int i = 0; i < rooms.Length; i++)
        {
            List<EnemyBase> enemies = rooms[i].GetEnemies();
            if (enemies == null || enemies.Count == 0) continue;

            hasAnyEnemyInLevel = true;

            for (int j = 0; j < enemies.Count; j++)
            {
                EnemyBase enemy = enemies[j];
                if (enemy != null && !enemy.IsDead)
                {
                    return false;
                }
            }
        }

        return hasAnyEnemyInLevel;
    }

    private IEnumerator HandleCompletionSequenceRoutine()
    {
        _isTransitioning = true; 
        
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null) pm.isMovementLocked = true;
        
        AudioService.SetLock(true); 

        if (_isLevel0)
        {
            if (level0CompletionSfx != null)
            {
                _audioSource.clip = level0CompletionSfx;
                _audioSource.volume = level0CompletionSfxVolume;
                _audioSource.Play();
            }

            yield return StartCoroutine(Level0ZoomAndCaptionWaitRoutine());
        }
        else
        {
            yield return StartCoroutine(NonLevel0SequenceRoutine());
        }

        ShowFinishPanel();
    }

    private IEnumerator Level0ZoomAndCaptionWaitRoutine()
    {
        CinemachinePositionComposer composer = finishCamera != null ? finishCamera.GetComponent<CinemachinePositionComposer>() : null;
        
        float timeout = Mathf.Max(0.1f, captionStartWaitTimeout);
        float waited = 0f;
        while (waited < timeout && (CaptionManager.Instance == null || !CaptionManager.Instance.IsPlaying))
        {
            waited += Time.unscaledDeltaTime;
            if (composer != null)
                composer.CameraDistance = Mathf.MoveTowards(composer.CameraDistance, level0ZoomDistance, Time.unscaledDeltaTime * level0ZoomSpeed);
            yield return null;
        }

        while (CaptionManager.Instance != null && CaptionManager.Instance.IsPlaying)
        {
            if (composer != null)
                composer.CameraDistance = Mathf.MoveTowards(composer.CameraDistance, level0ZoomDistance, Time.unscaledDeltaTime * level0ZoomSpeed);
            yield return null;
        }
    }

    private IEnumerator NonLevel0SequenceRoutine()
    {
        // 1. Play the SFX immediately
        if (finishPanelSfx != null)
        {
            _audioSource.clip = finishPanelSfx;
            _audioSource.volume = finishPanelSfxVolume;
            _audioSource.Play();
        }

        // 2 & 3. Show CompletePanel and Lerp it in from -2500
        if (completePanel != null)
        {
            completePanel.gameObject.SetActive(true);
            Vector2 startPos = completePanel.anchoredPosition;
            startPos.x = -2500f;
            completePanel.anchoredPosition = startPos;
            
            StartCoroutine(LerpCompletePanelRoutine(startPos, new Vector2(0f, startPos.y)));
        }

        // 4. Zoom the camera in slowly for X seconds
        CinemachinePositionComposer composer = finishCamera != null ? finishCamera.GetComponent<CinemachinePositionComposer>() : null;
        float originalDist = composer != null ? composer.CameraDistance : 0f;
        
        float elapsed = 0f;
        while (elapsed < completeWaitDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (composer != null)
            {
                // Smoothly ease the zoom over the wait duration
                float t = Mathf.Clamp01(elapsed / completeWaitDuration);
                float eased = 1f - Mathf.Pow(1f - t, 2f); 
                composer.CameraDistance = Mathf.Lerp(originalDist, completeZoomDistance, eased);
            }
            yield return null;
        }

        // 5. Time is up, exits routine and proceeds to ShowFinishPanel()
    }

    private IEnumerator LerpCompletePanelRoutine(Vector2 startPos, Vector2 endPos)
    {
        float elapsed = 0f;
        while (elapsed < completePanelLerpDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / completePanelLerpDuration);
            float eased = 1f - Mathf.Pow(1f - t, 4f); // Strong ease-out for a snappy but smooth slide
            
            completePanel.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
            yield return null;
        }
        completePanel.anchoredPosition = endPos;
    }

    private void ShowFinishPanel()
    {
        if (_isShown) return;
        _isShown = true;

        float elapsed = Mathf.Max(0f, Time.time - _runStartTime);
        if (timeText != null) timeText.text = FormatTime(elapsed);

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        Cursor.visible = true;

        StartCoroutine(AnimatePanelRevealRoutine());
    }

    private IEnumerator AnimatePanelRevealRoutine()
    {
        RectTransform rect = transform as RectTransform;
        
        Vector3 endScale = _originalScale; 
        Vector3 startScale = endScale * Mathf.Max(0.1f, panelStartScale);
        
        float duration = Mathf.Max(0.01f, panelRevealDuration);

        if (rect != null) rect.localScale = startScale;

        // 1. Pop panel in
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            _canvasGroup.alpha = eased;
            if (rect != null) rect.localScale = Vector3.Lerp(startScale, endScale, eased);

            yield return null;
        }

        if (rect != null) rect.localScale = endScale;
        _canvasGroup.alpha = 1f;

        // 2. Cool arcade score tally
        int targetScore = ScoreManager.Instance != null ? ScoreManager.Instance.TotalScore : 0;
        int currentDisplayedScore = 0;
        elapsed = 0f;
        float nextTick = 0f;

        while (elapsed < scoreTallyDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / scoreTallyDuration;
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            
            currentDisplayedScore = Mathf.RoundToInt(Mathf.Lerp(0, targetScore, eased));
            if (finalScoreText != null) finalScoreText.text = currentDisplayedScore.ToString();

            if (scoreTallyTickSound != null && Time.unscaledTime >= nextTick)
            {
                // PlayOneShot layers the ticks over the track without stopping it
                _audioSource.PlayOneShot(scoreTallyTickSound, 0.5f);
                nextTick = Time.unscaledTime + 0.05f;
            }
            yield return null;
        }

        if (finalScoreText != null) finalScoreText.text = targetScore.ToString();

        // 3. Unlock buttons
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return $"{minutes:00}:{secs:00}";
    }

     private void OnContinueClicked()
    {
        AudioService.SetLock(false);
        PreGamePanel.SkipNextPreGame = false;

        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.CompleteCurrentLevel(SceneManager.GetActiveScene().name);
        }
        else
        {
            Debug.LogWarning("No GameProgressManager found. Falling back to Main Menu.");
            SceneTransitionManager.Instance.LoadScene(mainMenuSceneName);
        }
    }

    private string ResolveNextLevelSceneName()
    {
        if (!string.IsNullOrWhiteSpace(nextLevelSceneName)) return nextLevelSceneName;

        string activeName = SceneManager.GetActiveScene().name;
        if (!activeName.StartsWith("Level")) return string.Empty;

        string suffix = activeName.Substring("Level".Length);
        if (!int.TryParse(suffix, out int levelIndex)) return string.Empty;

        return $"Level{levelIndex + 1}";
    }

    private void OnMainMenuClicked()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName)) return;

        AudioService.SetLock(false);
        SceneTransitionManager.Instance.LoadScene(mainMenuSceneName);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
    }
}