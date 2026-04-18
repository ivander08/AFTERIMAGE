using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class AudioService : MonoBehaviour
{
    private const int DefaultPoolSize = 8;
    private static AudioService _instance;

    [Header("Routing")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Pool")]
    [SerializeField, Min(1)] private int poolSize = DefaultPoolSize;

    [Header("Default 3D Settings")]
    [SerializeField, Range(0f, 1f)] private float defaultSpatialBlend = 1f;
    [SerializeField, Min(0f)] private float defaultMinDistance = 1f;
    [SerializeField, Min(0.01f)] private float defaultMaxDistance = 20f;

    private readonly List<AudioSource> _sources = new List<AudioSource>();
    private int _nextSourceIndex;

    public static AudioService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioService>();

                if (_instance == null)
                {
                    GameObject serviceObject = new GameObject(nameof(AudioService));
                    _instance = serviceObject.AddComponent<AudioService>();
                }
            }

            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        EnsurePool();
    }

    private void OnValidate()
    {
        if (poolSize < 1)
        {
            poolSize = 1;
        }

        ApplyDefaultsToSources();
    }

    public static void PlayClip(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = -1f, float minDistance = -1f, float maxDistance = -1f)
    {
        Instance.PlayClipInternal(clip, position, volume, pitch, spatialBlend, minDistance, maxDistance);
    }

    public static void PlayClip2D(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        Instance.PlayClipInternal(clip, Vector3.zero, volume, pitch, 0f, 0f, 0f);
    }

    public static void PlayRandom(AudioClip[] clips, Vector3 position, float volume = 1f, float pitchMin = 0.95f, float pitchMax = 1.05f, float spatialBlend = -1f, float minDistance = -1f, float maxDistance = -1f)
    {
        Instance.PlayRandomInternal(clips, position, volume, pitchMin, pitchMax, spatialBlend, minDistance, maxDistance);
    }

    public static void PlayRandom2D(AudioClip[] clips, float volume = 1f, float pitchMin = 0.95f, float pitchMax = 1.05f)
    {
        Instance.PlayRandomInternal(clips, Vector3.zero, volume, pitchMin, pitchMax, 0f, 0f, 0f);
    }

    private void EnsurePool()
    {
        if (_sources.Count > 0)
        {
            ApplyDefaultsToSources();
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            CreateSource(i);
        }
    }

    private void CreateSource(int index)
    {
        GameObject sourceObject = new GameObject($"SfxSource_{index}");
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = defaultSpatialBlend;
        source.minDistance = defaultMinDistance;
        source.maxDistance = defaultMaxDistance;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.outputAudioMixerGroup = sfxMixerGroup;

        _sources.Add(source);
    }

    private void ApplyDefaultsToSources()
    {
        for (int i = 0; i < _sources.Count; i++)
        {
            AudioSource source = _sources[i];

            if (source == null)
            {
                continue;
            }

            source.spatialBlend = defaultSpatialBlend;
            source.minDistance = defaultMinDistance;
            source.maxDistance = defaultMaxDistance;
            source.outputAudioMixerGroup = sfxMixerGroup;
        }
    }

    private AudioSource GetSource()
    {
        EnsurePool();

        for (int i = 0; i < _sources.Count; i++)
        {
            int index = (_nextSourceIndex + i) % _sources.Count;
            AudioSource source = _sources[index];

            if (source != null && !source.isPlaying)
            {
                _nextSourceIndex = (index + 1) % _sources.Count;
                return source;
            }
        }

        AudioSource fallbackSource = _sources[_nextSourceIndex];
        _nextSourceIndex = (_nextSourceIndex + 1) % _sources.Count;
        return fallbackSource;
    }

    private void PlayClipInternal(AudioClip clip, Vector3 position, float volume, float pitch, float spatialBlend, float minDistance, float maxDistance)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource source = GetSource();
        if (source == null)
        {
            return;
        }

        source.Stop();
        source.transform.position = position;
        source.clip = clip;
        source.volume = Mathf.Max(0f, volume);
        source.pitch = Mathf.Max(0.01f, pitch);
        source.spatialBlend = spatialBlend >= 0f ? spatialBlend : defaultSpatialBlend;
        source.minDistance = minDistance >= 0f ? minDistance : defaultMinDistance;
        source.maxDistance = maxDistance >= 0f ? maxDistance : defaultMaxDistance;
        source.outputAudioMixerGroup = sfxMixerGroup;
        source.Play();
    }

    private void PlayRandomInternal(AudioClip[] clips, Vector3 position, float volume, float pitchMin, float pitchMax, float spatialBlend, float minDistance, float maxDistance)
    {
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null)
        {
            return;
        }

        float minPitch = Mathf.Min(pitchMin, pitchMax);
        float maxPitch = Mathf.Max(pitchMin, pitchMax);
        float pitch = Random.Range(minPitch, maxPitch);

        PlayClipInternal(clip, position, volume, pitch, spatialBlend, minDistance, maxDistance);
    }
}