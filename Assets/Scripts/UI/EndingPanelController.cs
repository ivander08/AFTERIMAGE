// Assets/Scripts/UI/EndingPanelController.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// Drives the Level 6 ending sequence after Echo dies:
///   1. White panel fades in
///   2. Video fades in, plays with handheld camera shake
///   3. At 7:08 (428s) aggressively flashes to white
///   4. Wait 1s → AfterImageText fades in/out → ThankYouText fades in/out
///   5. Hand off to FinishPanelController (score tally only)
/// </summary>
public class EndingPanelController : MonoBehaviour
{
    public static EndingPanelController Instance { get; private set; }

    // ── UI References ──────────────────────────────────────────────────────────
    [Header("Panel Root")]
    public CanvasGroup panelCanvasGroup;        // on EndingPanel itself

    [Header("Video")]
    public RawImage videoRawImage;              // child of EndingImage
    public VideoPlayer videoPlayer;
    public CanvasGroup videoCanvasGroup;        // on EndingImage

    [Header("Text")]
    public CanvasGroup afterImageTextGroup;     // on AfterImageText
    public CanvasGroup thankYouTextGroup;       // on ThankYouText

    // ── Timing ────────────────────────────────────────────────────────────────
    [Header("Timing")]
    [Tooltip("How long the white panel fades in before the video starts")]
    public float whiteFadeInDuration    = 1.2f;

    [Tooltip("How long the video image fades in after the white panel is visible")]
    public float videoFadeInDuration    = 1.5f;

    [Tooltip("Video timestamp (seconds) at which to trigger the aggressive white flash")]
    public float flashTimestamp         = 428f;   // 7:08

    [Tooltip("How many seconds the aggressive white-out takes")]
    public float flashOutDuration       = 0.35f;

    [Tooltip("Seconds to wait after flash before showing texts")]
    public float postFlashWait          = 1.0f;

    [Tooltip("Fade in/out duration for AfterImageText")]
    public float afterImageFadeTime     = 1.0f;

    [Tooltip("How long AfterImageText stays fully visible")]
    public float afterImageHoldTime     = 2.5f;

    [Tooltip("Fade in/out duration for ThankYouText")]
    public float thankYouFadeTime       = 1.2f;

    [Tooltip("How long ThankYouText stays fully visible")]
    public float thankYouHoldTime       = 3.0f;

    [Tooltip("Seconds after ThankYouText fades out before FinishPanel appears")]
    public float preFinishWait          = 1.0f;

    // ── Camera Shake ──────────────────────────────────────────────────────────
    [Header("Camera Shake (applied to EndingImage RectTransform)")]
    [Tooltip("Max pixel offset for the handheld drift")]
    public float shakeMagnitude         = 6f;

    [Tooltip("Speed of the Perlin noise scroll")]
    public float shakeSpeed             = 1.8f;

    [Tooltip("Extra violent shake magnitude near the flash timestamp")]
    public float climaxShakeMagnitude   = 18f;

    [Tooltip("How many seconds before flashTimestamp the climax shake kicks in")]
    public float climaxShakeWindow      = 4f;

    // ── Private ───────────────────────────────────────────────────────────────
    private RectTransform _videoRect;
    private Vector2       _videoRectOrigin;
    private bool          _flashTriggered = false;
    private bool          _sequenceStarted = false;
    private float         _noiseOffsetX;
    private float         _noiseOffsetY;
    private float         _videoStartRealTime = -1f;  // real time when Play() was called

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Start fully invisible
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha          = 0f;
            panelCanvasGroup.interactable   = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        if (videoCanvasGroup != null) videoCanvasGroup.alpha = 0f;
        if (afterImageTextGroup != null) afterImageTextGroup.alpha = 0f;
        if (thankYouTextGroup != null) thankYouTextGroup.alpha = 0f;

        // Cache video RectTransform for shake
        if (videoRawImage != null)
        {
            _videoRect       = videoRawImage.GetComponent<RectTransform>();
            _videoRectOrigin = _videoRect != null ? _videoRect.anchoredPosition : Vector2.zero;
        }

        // Random noise seeds so it looks different every run
        _noiseOffsetX = Random.Range(0f, 100f);
        _noiseOffsetY = Random.Range(0f, 100f);

        gameObject.SetActive(false);
    }

    // ── Public entry point called by FinishPanelController ───────────────────

    public void StartEndingSequence()
    {
        if (_sequenceStarted) return;
        _sequenceStarted = true;

        gameObject.SetActive(true);
        StartCoroutine(EndingRoutine());
    }

    // ── Main coroutine ────────────────────────────────────────────────────────

    private IEnumerator EndingRoutine()
    {
        // 1. Fade in white panel
        yield return StartCoroutine(FadeCanvasGroup(panelCanvasGroup, 0f, 1f, whiteFadeInDuration));

        // 2. Prepare & start video
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.Prepare();

            // Wait up to 5s for prepare
            float waited = 0f;
            while (!videoPlayer.isPrepared && waited < 5f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            videoPlayer.Play();
            _videoStartRealTime = Time.unscaledTime;
        }

        // 3. Fade in the video image
        yield return StartCoroutine(FadeCanvasGroup(videoCanvasGroup, 0f, 1f, videoFadeInDuration));

        // 4. Wait for flash timestamp (with shake running in Update)
        yield return StartCoroutine(WaitForFlashTimestamp());

        // 5. Aggressive white flash — fade ONLY the image out fast.
        //    The VideoPlayer keeps playing so its audio track continues uninterrupted.
        //    We just make the RawImage invisible; the white panel behind it stays visible.
        yield return StartCoroutine(FadeCanvasGroup(videoCanvasGroup, 1f, 0f, flashOutDuration));

        // Keep video playing for audio — do NOT call videoPlayer.Stop() here.
        // It will stop naturally when its clip ends, or we stop it after the texts.

        // 6. Reset shake position
        if (_videoRect != null) _videoRect.anchoredPosition = _videoRectOrigin;

        // 7. Wait post-flash
        yield return new WaitForSecondsRealtime(postFlashWait);

        // 8. AfterImageText
        yield return StartCoroutine(FadeCanvasGroup(afterImageTextGroup, 0f, 1f, afterImageFadeTime));
        yield return new WaitForSecondsRealtime(afterImageHoldTime);
        yield return StartCoroutine(FadeCanvasGroup(afterImageTextGroup, 1f, 0f, afterImageFadeTime));

        // 9. ThankYouText
        yield return new WaitForSecondsRealtime(0.5f);
        yield return StartCoroutine(FadeCanvasGroup(thankYouTextGroup, 0f, 1f, thankYouFadeTime));
        yield return new WaitForSecondsRealtime(thankYouHoldTime);
        yield return StartCoroutine(FadeCanvasGroup(thankYouTextGroup, 1f, 0f, thankYouFadeTime));

        // 10. Wait then hand off
        yield return new WaitForSecondsRealtime(preFinishWait);


        // Do NOT fade out or hide EndingPanel — it stays as the white background
        // behind FinishPanel which renders on top of it.

        // Trigger FinishPanel (score tally only — SFX & continue button disabled via Level6 flag)
        if (FinishPanelController.Instance != null)
            FinishPanelController.Instance.ShowForLevel6();
    }

    // ── Flash timestamp wait (runs shake each frame) ──────────────────────────

    private IEnumerator WaitForFlashTimestamp()
    {
        // If no video at all, just wait the timestamp duration in real time
        if (videoPlayer == null)
        {
            yield return new WaitForSecondsRealtime(flashTimestamp);
            if (_videoRect != null) _videoRect.anchoredPosition = _videoRectOrigin;
            yield break;
        }

        // Use real elapsed time since Play() was called.
        // This works regardless of actual video length — if the video ends early
        // (goes black) the audio in it keeps playing via the AudioSource it drives,
        // and we still trigger the flash at the correct wall-clock moment.
        while (true)
        {
            float elapsed = _videoStartRealTime >= 0f
                ? Time.unscaledTime - _videoStartRealTime
                : 0f;

            float timeToFlash = flashTimestamp - elapsed;

            // Apply shake every frame
            ApplyShake(timeToFlash);

            if (elapsed >= flashTimestamp)
            {
                _flashTriggered = true;
                break;
            }

            yield return null;
        }

        // Stop shake, reset position
        if (_videoRect != null) _videoRect.anchoredPosition = _videoRectOrigin;
    }

    // ── Handheld shake ────────────────────────────────────────────────────────

    private void ApplyShake(float secondsToFlash)
    {
        if (_videoRect == null) return;

        float t    = Time.unscaledTime;
        float mag  = shakeMagnitude;

        // Ramp up shake as we approach the flash
        if (secondsToFlash >= 0f && secondsToFlash <= climaxShakeWindow)
        {
            float blend = 1f - (secondsToFlash / climaxShakeWindow);
            mag = Mathf.Lerp(shakeMagnitude, climaxShakeMagnitude, blend);
        }

        float x = (Mathf.PerlinNoise(_noiseOffsetX + t * shakeSpeed, 0f) - 0.5f) * 2f * mag;
        float y = (Mathf.PerlinNoise(0f, _noiseOffsetY + t * shakeSpeed) - 0.5f) * 2f * mag;

        _videoRect.anchoredPosition = _videoRectOrigin + new Vector2(x, y);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        cg.alpha = from;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        cg.alpha = to;
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnVideoPrepared;

        // Assign render texture to RawImage
        if (videoRawImage != null && vp.texture != null)
            videoRawImage.texture = vp.texture;
    }
}