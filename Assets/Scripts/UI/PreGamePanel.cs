// Assets/Scripts/UI/PreGamePanel.cs
using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class PreGamePanel : MonoBehaviour
{
    // Global flag for other scripts to check
    public static bool IsPlaying { get; private set; }[Header("Debug")]
    public bool skipForDebug = false;
    public static bool SkipNextPreGame = false;

    [Header("Level Data")]
    public LevelLoadoutConfig levelConfig;

    [Header("UI References")]
    public TextMeshProUGUI dateTextUI;
    public TextMeshProUGUI locationTextUI;
    public TextMeshProUGUI missionTextUI;
    public GameObject reticleObject;

    [Header("Timing Settings")]
    public float typeSpeed = 0.05f;
    public float delayBetweenLines = 0.5f;
    public float displayDuration = 2.5f;
    public float fadeDuration = 1.0f;

    [Header("Audio")]
    public AudioClip typeSound;

    private CanvasGroup _canvasGroup;
    private AudioSource _audioSource;

    private void Awake()
    {
        // 1. Lock this instantly so RoomManager won't start captions early
        IsPlaying = true; 

        _canvasGroup = GetComponent<CanvasGroup>();
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;

        dateTextUI.text = "";
        locationTextUI.text = "";
        missionTextUI.text = "";
    }

    private IEnumerator Start()
    {
        if (skipForDebug || SkipNextPreGame)
        {
            SkipNextPreGame = false; 
            IsPlaying = false;
            
            if (reticleObject != null) reticleObject.SetActive(true);
            gameObject.SetActive(false);
            yield break;
        }

        // (Removed IsPlaying = true from here, it's now in Awake)
        
        if (reticleObject != null) reticleObject.SetActive(false);

        string dText = levelConfig != null ? levelConfig.dateText : "UNKNOWN DATE";
        string lText = levelConfig != null ? levelConfig.locationText : "UNKNOWN LOCATION";
        string mText = levelConfig != null ? levelConfig.missionText : "NO MISSION ASSIGNED";

        yield return StartCoroutine(TypeText(dateTextUI, dText));
        yield return new WaitForSeconds(delayBetweenLines);

        yield return StartCoroutine(TypeText(locationTextUI, lText));
        yield return new WaitForSeconds(delayBetweenLines);

        yield return StartCoroutine(TypeText(missionTextUI, mText));
        yield return new WaitForSeconds(displayDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        IsPlaying = false;
        if (reticleObject != null) reticleObject.SetActive(true);
        gameObject.SetActive(false);
    }

    private IEnumerator TypeText(TextMeshProUGUI uiElement, string text)
    {
        uiElement.text = "";
        foreach (char c in text)
        {
            uiElement.text += c;
            if (typeSound != null && !char.IsWhiteSpace(c))
            {
                _audioSource.PlayOneShot(typeSound, 0.3f);
            }
            yield return new WaitForSeconds(typeSpeed);
        }
    }
}