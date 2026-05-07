// Assets/Scripts/UI/FlashbackPanelController.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CanvasGroup))]
public class FlashbackPanelController : MonoBehaviour
{
    public static bool IsPlaying { get; private set; }
    public static bool SkipNextFlashback = false;

    [Header("Settings")]
    public bool skipForDebug = false;
    public LevelLoadoutConfig levelConfig;

    [Header("UI References")]
    public Image flashbackImage;
    public TextMeshProUGUI flashbackText;
    public GameObject skipPromptObj;[Header("Audio")]
    public AudioClip typeSound;
    public float audioFadeInDuration = 1.0f;
    private AudioSource _audioSource;
    private CanvasGroup _canvasGroup;
    private CanvasGroup _skipPromptGroup;

    [Header("Timings")][Tooltip("How long to stay pure black before the audio starts")]
    public float initialBlackScreen = 2.0f;[Tooltip("When the image starts glitching in (relative to audio start)")]
    public float glitchStartTime = 1.8f;
    [Tooltip("When the image stabilizes (relative to audio start)")]
    public float glitchEndTime = 4f;[Tooltip("When the image cuts to black (relative to audio start)")]
    public float cutToBlackTime = 7.5f;[Header("Text Timings")]
    public float typeSpeed = 0.05f;
    public float textReadTime = 2.0f;
    public float delayBeforePreGame = 2.0f;[Header("Visual Settings")]
    [Tooltip("How much the image scales up by the end of the cutscene")]
    public float zoomEndScale = 1.25f; 
    public float maxGlitchIntensity = 0.8f;
    public float normalGlitchIntensity = 0.1f;
    
    public float ambientGlitchFluctuation = 0.3f;
    public float ambientGlitchSpeed = 5.0f;

    [Header("Camera Realism")][Tooltip("How far the image slowly drifts to simulate a handheld camera")]
    public float cameraDriftAmount = 10f;
    [Tooltip("How fast the handheld camera drifts")]
    public float cameraDriftSpeed = 5f;
    [Tooltip("How violently the image shakes/snaps during the heavy glitch phase")]
    public float glitchJitterAmount = 50f;

    // Shader Property IDs
    private int _glitchIntensityId = Shader.PropertyToID("_GlitchIntensity");
    
    // Store original position to calculate drift relative to center
    private Vector2 _originalImagePosition;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        
        if (flashbackImage != null) 
        {
            _originalImagePosition = flashbackImage.rectTransform.anchoredPosition;
            flashbackImage.gameObject.SetActive(false);
        }
        
        if (flashbackText != null) flashbackText.text = "";
        
        // Auto-setup CanvasGroup for the skip prompt so we can fade it
        if (skipPromptObj != null)
        {
            _skipPromptGroup = skipPromptObj.GetComponent<CanvasGroup>();
            if (_skipPromptGroup == null) _skipPromptGroup = skipPromptObj.AddComponent<CanvasGroup>();
            
            _skipPromptGroup.alpha = 0f; // Start invisible
            skipPromptObj.SetActive(true);
        }

        // Lock the game state
        IsPlaying = true;
    }

    private IEnumerator Start()
    {
        if (skipForDebug || SkipNextFlashback || levelConfig == null || !levelConfig.hasFlashback)
        {
            SkipNextFlashback = false;
            StartCoroutine(EndFlashbackRoutine(0f)); // Instant skip
            yield break;
        }

        if (flashbackImage != null && flashbackImage.material != null)
        {
            flashbackImage.material = new Material(flashbackImage.material);
        }

        if (flashbackImage != null) flashbackImage.sprite = levelConfig.flashbackImage;

        StartCoroutine(SkipListenerRoutine());
        yield return StartCoroutine(RunCutsceneSequence());
    }

    private IEnumerator RunCutsceneSequence()
    {
        // 1. Initial Pure Black Screen / Silence
        yield return new WaitForSeconds(initialBlackScreen);

        // 2. Fade in the Skip Prompt and Audio
        if (_skipPromptGroup != null) StartCoroutine(FadeGroupRoutine(_skipPromptGroup, 1f, 1.0f));

        if (levelConfig.flashbackAudio != null)
        {
            _audioSource.clip = levelConfig.flashbackAudio;
            _audioSource.volume = 0f;
            _audioSource.Play();
            StartCoroutine(AudioFadeRoutine(1f, audioFadeInDuration));
        }

        float timer = 0f;
        Vector3 startScale = Vector3.one;
        Vector3 endScale = new Vector3(zoomEndScale, zoomEndScale, 1f);

        // 3. Image Sequence
        while (timer < cutToBlackTime)
        {
            timer += Time.deltaTime;
            
            float currentGlitch = normalGlitchIntensity;
            Vector2 jitterOffset = Vector2.zero;

            // Phase A: Black screen, audio playing
            if (timer < glitchStartTime)
            {
                if (flashbackImage != null && flashbackImage.gameObject.activeSelf)
                    flashbackImage.gameObject.SetActive(false);
            }
            // Phase B: Glitch into view (NO ZOOMING YET)
            else if (timer >= glitchStartTime && timer < glitchEndTime)
            {
                if (flashbackImage != null && !flashbackImage.gameObject.activeSelf)
                    flashbackImage.gameObject.SetActive(true);

                float phaseT = (timer - glitchStartTime) / (glitchEndTime - glitchStartTime);
                
                // Ease out the glitch intensity so it snaps in violently but settles nicely
                float easeOutT = 1f - Mathf.Pow(1f - phaseT, 3f);
                currentGlitch = Mathf.Lerp(maxGlitchIntensity, normalGlitchIntensity, easeOutT);
                
                // Violent positional jitter during the heavy glitch
                if (currentGlitch > normalGlitchIntensity * 2f && Random.value > 0.25f)
                {
                    jitterOffset = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * (glitchJitterAmount * currentGlitch);
                }

                if (flashbackImage != null && flashbackImage.material != null)
                {
                    flashbackImage.material.SetFloat(_glitchIntensityId, currentGlitch);
                }

                // Lock scale at 1.0 here
                if (flashbackImage != null) 
                    flashbackImage.rectTransform.localScale = startScale;
            }
            // Phase C: Stable image, Start Zooming + Ambient Fluctuation
            else if (timer >= glitchEndTime && timer < cutToBlackTime)
            {
                if (flashbackImage != null && flashbackImage.material != null)
                {
                    float ambientGlitch = normalGlitchIntensity + (Mathf.PerlinNoise(timer * ambientGlitchSpeed, 0f) * ambientGlitchFluctuation);
                    flashbackImage.material.SetFloat(_glitchIntensityId, ambientGlitch);
                }

                // Zoom Math (with cinematic SmoothStep easing)
                float rawZoomT = (timer - glitchEndTime) / (cutToBlackTime - glitchEndTime); 
                float cinematicZoomT = Mathf.SmoothStep(0f, 1f, rawZoomT);
                
                if (flashbackImage != null) 
                    flashbackImage.rectTransform.localScale = Vector3.Lerp(startScale, endScale, cinematicZoomT);
            }

            // Apply Camera Drift (Perlin Noise) + Glitch Jitter to the image position
            if (flashbackImage != null && flashbackImage.gameObject.activeSelf)
            {
                // Drift gives it a continuous slow "breathing" or "handheld" feel
                float driftX = (Mathf.PerlinNoise(timer * cameraDriftSpeed, 0f) - 0.5f) * 2f;
                float driftY = (Mathf.PerlinNoise(0f, timer * cameraDriftSpeed) - 0.5f) * 2f;
                Vector2 driftOffset = new Vector2(driftX, driftY) * cameraDriftAmount;

                flashbackImage.rectTransform.anchoredPosition = _originalImagePosition + driftOffset + jitterOffset;
            }

            yield return null;
        }

        // 4. Cut to black
        if (flashbackImage != null) flashbackImage.gameObject.SetActive(false);
        
        // 5. Wait for audio to finish
        while (_audioSource.isPlaying)
        {
            yield return null;
        }

        // 6. Text Sequence
        if (!string.IsNullOrEmpty(levelConfig.flashbackTextAfter))
        {
            yield return StartCoroutine(TypeTextRoutine(levelConfig.flashbackTextAfter));
            yield return new WaitForSeconds(textReadTime);
            yield return StartCoroutine(UntypeTextRoutine(levelConfig.flashbackTextAfter));
            
            // Fade out the skip prompt early as we transition out
            if (_skipPromptGroup != null) StartCoroutine(FadeGroupRoutine(_skipPromptGroup, 0f, 0.5f));

            yield return new WaitForSeconds(delayBeforePreGame);
        }

        // Natural End
        StartCoroutine(EndFlashbackRoutine(0.25f));
    }

    private IEnumerator SkipListenerRoutine()
    {
        while (IsPlaying)
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                StopAllCoroutines(); 
                StartCoroutine(EndFlashbackRoutine(0.25f));
                yield break;
            }
            yield return null;
        }
    }

    private IEnumerator EndFlashbackRoutine(float fadeDuration)
    {
        if (_audioSource != null && _audioSource.isPlaying) StartCoroutine(AudioFadeRoutine(0f, fadeDuration));
        
        if (fadeDuration > 0f)
        {
            yield return StartCoroutine(FadeGroupRoutine(_canvasGroup, 0f, fadeDuration));
        }

        if (_audioSource != null) _audioSource.Stop();
        if (skipPromptObj != null) skipPromptObj.SetActive(false);
        
        IsPlaying = false;
        gameObject.SetActive(false); 
    }

    private IEnumerator FadeGroupRoutine(CanvasGroup cg, float targetAlpha, float duration)
    {
        if (cg == null) yield break;
        float startAlpha = cg.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        cg.alpha = targetAlpha;
    }

    private IEnumerator AudioFadeRoutine(float targetVolume, float duration)
    {
        float startVol = _audioSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(startVol, targetVolume, elapsed / duration);
            yield return null;
        }
        _audioSource.volume = targetVolume;
    }

    private IEnumerator TypeTextRoutine(string text)
    {
        if (flashbackText == null) yield break;
        flashbackText.text = "";
        
        foreach (char c in text)
        {
            flashbackText.text += c;
            if (typeSound != null && !char.IsWhiteSpace(c))
            {
                _audioSource.PlayOneShot(typeSound, 0.3f);
            }
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    private IEnumerator UntypeTextRoutine(string text)
    {
        if (flashbackText == null) yield break;
        
        string currentText = text;
        while (currentText.Length > 0)
        {
            currentText = currentText.Substring(0, currentText.Length - 1);
            flashbackText.text = currentText;

            if (typeSound != null && currentText.Length > 0 && !char.IsWhiteSpace(currentText[currentText.Length - 1]))
            {
                _audioSource.PlayOneShot(typeSound, 0.3f);
            }
            
            yield return new WaitForSeconds(typeSpeed * 0.75f);
        }
    }
}