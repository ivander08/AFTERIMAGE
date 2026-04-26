// Assets/Scripts/Audio/AmbientAudioController.cs
using System.Collections;
using UnityEngine;

public class AmbientAudioController : MonoBehaviour
{
    public static AmbientAudioController Instance { get; private set; }

    [Header("Level Startup")]
    public AudioClip startingAmbient;[Range(0f, 1f)] public float targetVolume = 0.5f;
    public float initialFadeInDuration = 2.5f;

    private AudioSource _sourceA;
    private AudioSource _sourceB;
    private bool _isUsingSourceA = true;
    private Coroutine _fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Create two invisible audio sources for crossfading
        _sourceA = gameObject.AddComponent<AudioSource>();
        _sourceB = gameObject.AddComponent<AudioSource>();

        _sourceA.loop = true;
        _sourceB.loop = true;
        _sourceA.playOnAwake = false;
        _sourceB.playOnAwake = false;
        _sourceA.volume = 0f;
        _sourceB.volume = 0f;
    }

    private void Start()
    {
        if (startingAmbient != null)
        {
            CrossfadeTo(startingAmbient, initialFadeInDuration);
        }
    }

    public void CrossfadeTo(AudioClip newClip, float duration)
    {
        if (newClip == null) return;

        AudioSource activeSource = _isUsingSourceA ? _sourceA : _sourceB;
        AudioSource fadingInSource = _isUsingSourceA ? _sourceB : _sourceA;

        // Prevent restarting the exact same clip if it's already playing
        if (activeSource.clip == newClip && activeSource.isPlaying) return;

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);

        fadingInSource.clip = newClip;
        fadingInSource.volume = 0f;
        fadingInSource.Play();

        _fadeRoutine = StartCoroutine(CrossfadeRoutine(activeSource, fadingInSource, duration));
        
        // Swap which source is considered "active"
        _isUsingSourceA = !_isUsingSourceA;
    }

    private IEnumerator CrossfadeRoutine(AudioSource fadingOut, AudioSource fadingIn, float duration)
    {
        float startOutVol = fadingOut.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            fadingOut.volume = Mathf.Lerp(startOutVol, 0f, t);
            fadingIn.volume = Mathf.Lerp(0f, targetVolume, t);

            yield return null;
        }

        fadingOut.volume = 0f;
        fadingIn.volume = targetVolume;
        fadingOut.Stop();
    }

     public void FadeToSilence(float duration)
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeToSilenceRoutine(duration));
    }

    private IEnumerator FadeToSilenceRoutine(float duration)
    {
        float startVolA = _sourceA.volume;
        float startVolB = _sourceB.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            _sourceA.volume = Mathf.Lerp(startVolA, 0f, t);
            _sourceB.volume = Mathf.Lerp(startVolB, 0f, t);
            yield return null;
        }

        _sourceA.Stop();
        _sourceB.Stop();
        _sourceA.volume = 0f;
        _sourceB.volume = 0f;
    }
}