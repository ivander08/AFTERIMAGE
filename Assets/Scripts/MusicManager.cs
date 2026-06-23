using System.Collections;
using UnityEngine;

/// <summary>
/// Manages background music playback and track switching.
/// </summary>
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }[Header("Mixer")]
    public AudioMixer mainMixer;

    [Header("Music Tracks")]
    public AudioClip menuMusic;
    public AudioClip level0Music;
    public AudioClip[] levelMusicTracks;
    public AudioClip elevatorMusic;

    [Header("Settings")]
    public float fadeDuration = 1.5f;
    [Range(0f, 1f)] public float maxMusicVolume = 0.5f; // Cap the max volume for regular levels
    [Range(0f, 1f)] public float level0MusicVolume = 0.5f; // Separate volume for Level0
    [Range(0f, 1f)] public float mainMenuMusicVolume = 0.5f;

    private AudioSource _audioSource;
    private string _currentTrackType = ""; // "Menu" or "Level"
    private string _lastSceneName = "";
    private Coroutine _musicRoutine;
    private bool _isPlayingLevel0Music = false; // Track if current music is Level0
    private AudioClip _savedLevelTrack;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("MusicManager (Auto-created)");
            go.AddComponent<MusicManager>();
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
        DontDestroyOnLoad(gameObject);

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.loop = true;
        _audioSource.playOnAwake = false;

        if (mainMixer != null)
        {
            AudioMixerGroup[] groups = mainMixer.FindMatchingGroups("Music");
            if (groups.Length > 0) _audioSource.outputAudioMixerGroup = groups[0];
        }

        SceneManager.sceneLoaded += OnSceneLoaded;

        // REMOVE LoadSavedVolumes() from here
    }

    // ADD THIS: Load volumes in Start() so the AudioMixer is ready
    private void Start()
    {
        LoadSavedVolumes();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void SetMasterVolume(float sliderValue)
    {
        float value = Mathf.Clamp(sliderValue, 0.0001f, 1f);
        if (mainMixer != null)
            mainMixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("Pref_MasterVol", value);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. If we died and reloaded the exact same level, DO NOTHING. Keep the music bumping!
        if (scene.name == _lastSceneName) return;
        _lastSceneName = scene.name;

        // 2. Menu vs Level routing
        if (scene.name == "MainMenu" || scene.name == "LoadoutScene")
        {
            if (_currentTrackType != "Menu")
            {
                if (_musicRoutine != null) StopCoroutine(_musicRoutine);
                _musicRoutine = StartCoroutine(PlayMenuMusicRoutine());
            }
        }
        else if (scene.name.StartsWith("Level")) // Level0, Level1, etc.
        {
            if (_musicRoutine != null) StopCoroutine(_musicRoutine);
            _musicRoutine = StartCoroutine(PlayLevelMusicRoutine());
        }
    }

    // --- PLAYBACK ROUTINES ---

    private IEnumerator PlayMenuMusicRoutine()
    {
        _currentTrackType = "Menu";
        yield return StartCoroutine(FadeOutRoutine());

        if (menuMusic != null)
        {
            _audioSource.clip = menuMusic;
            _audioSource.Play();
            // Fade in to the menu-specific volume
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _audioSource.volume = Mathf.Lerp(0f, mainMenuMusicVolume, elapsed / fadeDuration);
                yield return null;
            }
            _audioSource.volume = mainMenuMusicVolume;
        }
    }

    private IEnumerator PlayLevelMusicRoutine()
    {
        _currentTrackType = "Level";

        // Fade out menu music immediately
        yield return StartCoroutine(FadeOutRoutine());

        // WAIT for PreGamePanel to finish typing the location/date
        while (PreGamePanel.IsPlaying)
        {
            yield return null;
        }

        // Check if this is Level0 and we have Level0 music
        bool isLevel0 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Level0";
        AudioClip trackToPlay = null;
        
        if (isLevel0 && level0Music != null)
        {
            trackToPlay = level0Music;
            _isPlayingLevel0Music = true;
        }
        else if (levelMusicTracks != null && levelMusicTracks.Length > 0)
        {
            // Pick a random track for non-Level0 scenes
            trackToPlay = levelMusicTracks[Random.Range(0, levelMusicTracks.Length)];
            _isPlayingLevel0Music = false;
        }

        if (trackToPlay != null)
        {
            _audioSource.clip = trackToPlay;
            _audioSource.Play();
            
            // Fade it in smoothly
            yield return StartCoroutine(FadeInRoutine());
        }
    }

    private IEnumerator FadeOutRoutine(float durationOverride = -1f)
    {
        float dur = durationOverride > 0f ? durationOverride : fadeDuration;
        float startVol = _audioSource.volume;
        float elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            _audioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / dur);
            yield return null;
        }
        _audioSource.volume = 0f;
    }

    private IEnumerator FadeInRoutine(float durationOverride = -1f)
    {
        float dur = durationOverride > 0f ? durationOverride : fadeDuration;
        float targetVolume = _isPlayingLevel0Music ? level0MusicVolume : maxMusicVolume;
        float elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            _audioSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / dur);
            yield return null;
        }
        _audioSource.volume = targetVolume;
    }

    // --- VOLUME CONTROL API (For your UI Sliders later) ---

    // Note: AudioMixer uses logarithmic Decibels (-80dB to 0dB), not linear 0.0 to 1.0!
    // We convert 0.0001f - 1.0f into Decibels safely.
    
    public void SetMusicVolume(float sliderValue)
    {
        float value = Mathf.Clamp(sliderValue, 0.0001f, 1f);
        if (mainMixer != null)
            mainMixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("Pref_MusicVol", value);
    }

    public void SetSFXVolume(float sliderValue)
    {
        float value = Mathf.Clamp(sliderValue, 0.0001f, 1f);
        if (mainMixer != null)
            mainMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("Pref_SFXVol", value);
    }

    private void LoadSavedVolumes()
    {
        // MODIFIED: Changed default fallback from 0.5f to 1.0f
        float savedMaster = PlayerPrefs.GetFloat("Pref_MasterVol", 1f);
        float savedMusic  = PlayerPrefs.GetFloat("Pref_MusicVol",  1f);
        float savedSFX    = PlayerPrefs.GetFloat("Pref_SFXVol",    1f);

        SetMasterVolume(savedMaster);
        SetMusicVolume(savedMusic);
        SetSFXVolume(savedSFX);
    }

    // --- LEVEL COMPLETION API ---

    public void FadeOutMusic()
    {
        if (_musicRoutine != null) StopCoroutine(_musicRoutine);
        _musicRoutine = StartCoroutine(FadeOutRoutine());
    }

    public void PlayEasterEggMusic()
    {
        if (elevatorMusic == null) return;
        _savedLevelTrack = _audioSource.clip;
        if (_musicRoutine != null) StopCoroutine(_musicRoutine);
        _musicRoutine = StartCoroutine(PlayEasterEggRoutine());
    }

    public void ResumeLevelMusic()
    {
        if (_savedLevelTrack == null) return;
        if (_musicRoutine != null) StopCoroutine(_musicRoutine);
        _musicRoutine = StartCoroutine(ResumeLevelRoutine());
    }

    private IEnumerator PlayEasterEggRoutine()
    {
        yield return StartCoroutine(FadeOutRoutine(0.2f));
        _audioSource.clip = elevatorMusic;
        _audioSource.Play();
        yield return StartCoroutine(FadeInRoutine(0.2f));
    }

    private IEnumerator ResumeLevelRoutine()
    {
        yield return StartCoroutine(FadeOutRoutine(0.2f));
        _audioSource.clip = _savedLevelTrack;
        _audioSource.Play();
        yield return StartCoroutine(FadeInRoutine(0.2f));
    }
}