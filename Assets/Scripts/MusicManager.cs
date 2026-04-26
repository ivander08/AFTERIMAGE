// Assets/Scripts/Audio/MusicManager.cs
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }[Header("Mixer")]
    public AudioMixer mainMixer;

    [Header("Music Tracks")]
    public AudioClip menuMusic;
    public AudioClip[] levelMusicTracks;

    [Header("Settings")]
    public float fadeDuration = 1.5f;
    [Range(0f, 1f)] public float maxMusicVolume = 0.5f; // Cap the max volume

    private AudioSource _audioSource;
    private string _currentTrackType = ""; // "Menu" or "Level"
    private string _lastSceneName = "";
    private Coroutine _musicRoutine;

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

        // Assign to the Music mixer group automatically
        AudioMixerGroup[] groups = mainMixer.FindMatchingGroups("Music");
        if (groups.Length > 0) _audioSource.outputAudioMixerGroup = groups[0];

        SceneManager.sceneLoaded += OnSceneLoaded;

        LoadSavedVolumes();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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

        // Fade out whatever is playing
        yield return StartCoroutine(FadeOutRoutine());

        // Play Menu Music
        if (menuMusic != null)
        {
            _audioSource.clip = menuMusic;
            _audioSource.Play();
            yield return StartCoroutine(FadeInRoutine());
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

        // Pick a random track
        if (levelMusicTracks != null && levelMusicTracks.Length > 0)
        {
            AudioClip randomTrack = levelMusicTracks[Random.Range(0, levelMusicTracks.Length)];
            _audioSource.clip = randomTrack;
            _audioSource.Play();
            
            // Fade it in smoothly
            yield return StartCoroutine(FadeInRoutine());
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        float startVol = _audioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Unscaled so it fades even if game is paused
            _audioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
            yield return null;
        }
        _audioSource.volume = 0f;
    }

    private IEnumerator FadeInRoutine()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _audioSource.volume = Mathf.Lerp(0f, maxMusicVolume, elapsed / fadeDuration);
            yield return null;
        }
        _audioSource.volume = maxMusicVolume;
    }

    // --- VOLUME CONTROL API (For your UI Sliders later) ---

    // Note: AudioMixer uses logarithmic Decibels (-80dB to 0dB), not linear 0.0 to 1.0!
    // We convert 0.0001f - 1.0f into Decibels safely.
    
    public void SetMusicVolume(float sliderValue)
    {
        float value = Mathf.Clamp(sliderValue, 0.0001f, 1f);
        mainMixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("Pref_MusicVol", value);
    }

    public void SetSFXVolume(float sliderValue)
    {
        float value = Mathf.Clamp(sliderValue, 0.0001f, 1f);
        mainMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("Pref_SFXVol", value);
    }

    private void LoadSavedVolumes()
    {
        // Load preferences, default to 0.75 (75%) if first time
        float savedMusic = PlayerPrefs.GetFloat("Pref_MusicVol", 0.75f);
        float savedSFX = PlayerPrefs.GetFloat("Pref_SFXVol", 0.75f);

        SetMusicVolume(savedMusic);
        SetSFXVolume(savedSFX);
    }
}