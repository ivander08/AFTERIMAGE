// Assets/Scripts/Audio/AudioService.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum AudioPriority { UI = 0, World = 1 }

[DisallowMultipleComponent]
public class AudioService : MonoBehaviour
{
    // Reserved sources for UI/player sounds — enemies can NEVER steal these
    private const int ReservedPoolSize = 12;
    // World sources for enemies, ambient hits, throwables, etc.
    private const int DefaultWorldPoolSize = 48;

    private static AudioService _instance;

    [Header("Routing")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Pool sizes")]
    [SerializeField, Min(1)] private int worldPoolSize = DefaultWorldPoolSize;

    [Header("Default 3D Settings")]
    [SerializeField, Range(0f, 1f)] private float defaultSpatialBlend = 1f;
    [SerializeField, Min(0f)]       private float defaultMinDistance   = 1f;
    [SerializeField, Min(0.01f)]    private float defaultMaxDistance   = 20f;

    // Two separate pools — reserved never lends to world
    private readonly List<AudioSource> _reservedSources = new();
    private readonly List<AudioSource> _worldSources    = new();

    private int _nextReservedIndex;
    private int _nextWorldIndex;

    private static bool _isLocked = false;
    public static void SetLock(bool locked) => _isLocked = locked;

    public static AudioService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioService>();
                if (_instance == null)
                {
                    var go = new GameObject(nameof(AudioService));
                    _instance = go.AddComponent<AudioService>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        BuildPool(_reservedSources, ReservedPoolSize,    "Reserved");
        BuildPool(_worldSources,    worldPoolSize,       "World");
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>2D sound. Always uses the reserved pool (UI, player feedback).</summary>
    public static void PlayClip2D(AudioClip clip, float volume = 1f, float pitch = 1f)
        => Instance.Play(clip, Vector3.zero, volume, pitch, 0f, 0f, 0f, AudioPriority.UI);

    /// <summary>3D world sound. Uses the world pool (enemies, props).</summary>
    public static void PlayClip(AudioClip clip, Vector3 pos,
        float volume = 1f, float pitch = 1f,
        float spatialBlend = -1f, float minDistance = -1f, float maxDistance = -1f,
        AudioPriority priority = AudioPriority.World)
        => Instance.Play(clip, pos, volume, pitch, spatialBlend, minDistance, maxDistance, priority);

    /// <summary>Random 3D world sound.</summary>
    public static void PlayRandom(AudioClip[] clips, Vector3 pos,
        float volume = 1f, float pitchMin = 0.95f, float pitchMax = 1.05f,
        float spatialBlend = -1f, float minDistance = -1f, float maxDistance = -1f,
        AudioPriority priority = AudioPriority.World)
        => Instance.PlayRnd(clips, pos, volume, pitchMin, pitchMax, spatialBlend, minDistance, maxDistance, priority);

    /// <summary>Random 2D sound. Always uses the reserved pool.</summary>
    public static void PlayRandom2D(AudioClip[] clips,
        float volume = 1f, float pitchMin = 0.95f, float pitchMax = 1.05f)
        => Instance.PlayRnd(clips, Vector3.zero, volume, pitchMin, pitchMax, 0f, 0f, 0f, AudioPriority.UI);

    public static void StopAllSFX()
    {
        if (_instance == null) return;
        foreach (var s in _instance._reservedSources) s?.Stop();
        foreach (var s in _instance._worldSources)    s?.Stop();
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    // Assets/Scripts/Audio/AudioService.cs
    private void Play(AudioClip clip, Vector3 pos, float volume, float pitch,
        float spatialBlend, float minDist, float maxDist, AudioPriority priority)
    {
        if (clip == null) return;
        if (_isLocked && priority == AudioPriority.World) return;

        AudioSource src = GetSource(priority);
        if (src == null) return;

        src.Stop();
        src.transform.position = pos;
        src.clip         = clip;
        src.volume       = Mathf.Max(0f, volume);
        src.pitch        = Mathf.Max(0.01f, pitch);
        src.spatialBlend = spatialBlend >= 0f ? spatialBlend : defaultSpatialBlend;
        src.minDistance  = minDist >= 0f      ? minDist      : defaultMinDistance;
        src.maxDistance  = maxDist >= 0f      ? maxDist      : defaultMaxDistance;
        src.outputAudioMixerGroup = sfxMixerGroup;
        
        // ADDED: UI sounds bypass the pause menu silence
        src.ignoreListenerPause = (priority == AudioPriority.UI); 
        
        src.Play();
    }

    private void PlayRnd(AudioClip[] clips, Vector3 pos, float volume,
        float pitchMin, float pitchMax,
        float spatialBlend, float minDist, float maxDist, AudioPriority priority)
    {
        if (clips == null || clips.Length == 0) return;
        var clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;
        float pitch = Random.Range(Mathf.Min(pitchMin, pitchMax), Mathf.Max(pitchMin, pitchMax));
        Play(clip, pos, volume, pitch, spatialBlend, minDist, maxDist, priority);
    }

    private AudioSource GetSource(AudioPriority priority)
    {
        // UI/player sounds always go to the reserved pool
        if (priority == AudioPriority.UI)
            return PickSource(_reservedSources, ref _nextReservedIndex, canSteal: true);

        // World sounds use the world pool; on overflow they steal from the world pool only
        return PickSource(_worldSources, ref _nextWorldIndex, canSteal: true);
    }

    private AudioSource PickSource(List<AudioSource> pool, ref int nextIndex, bool canSteal)
    {
        if (pool.Count == 0) return null;

        // Prefer an idle source
        for (int i = 0; i < pool.Count; i++)
        {
            int idx = (nextIndex + i) % pool.Count;
            if (pool[idx] != null && !pool[idx].isPlaying)
            {
                nextIndex = (idx + 1) % pool.Count;
                return pool[idx];
            }
        }

        // All busy — steal the oldest (next in round-robin) within this pool only
        if (!canSteal) return null;
        var fallback = pool[nextIndex];
        nextIndex = (nextIndex + 1) % pool.Count;
        return fallback;
    }

    private void BuildPool(List<AudioSource> pool, int count, string label)
    {
        for (int i = 0; i < count; i++)
        {
            var go  = new GameObject($"SfxSource_{label}_{i}");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake  = false;
            src.loop         = false;
            src.spatialBlend = defaultSpatialBlend;
            src.minDistance  = defaultMinDistance;
            src.maxDistance  = defaultMaxDistance;
            src.rolloffMode  = AudioRolloffMode.Logarithmic;
            src.outputAudioMixerGroup = sfxMixerGroup;
            pool.Add(src);
        }
    }

    private void OnValidate()
    {
        if (worldPoolSize < 1) worldPoolSize = 1;
    }
}