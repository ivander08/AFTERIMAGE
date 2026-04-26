// Assets/Scripts/Audio/FootstepAudio.cs
using UnityEngine;

public class FootstepAudio : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip[] footstepClips;

    [Header("Settings")]
    public float stepInterval = 0.4f;
    public float volume = 0.6f;
    public float pitchMin = 0.9f;
    public float pitchMax = 1.15f;
    public bool is3D = true;

    [Header("3D Falloff")]
    public float minDistance = 1f;   // full volume within this range
    public float maxDistance = 8f;   // silent beyond this range

    private Animator _animator;
    private float _timer;
    private bool _wasWalking;
    private EnemyBase _enemy;
    private PlayerHealth _playerHealth;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _enemy = GetComponentInParent<EnemyBase>();
        _playerHealth = GetComponentInParent<PlayerHealth>();
    }

    private void Update()
    {
        if (_enemy != null && _enemy.IsDead) { ResetState(); return; }
        if (_playerHealth != null && _playerHealth.isDead) { ResetState(); return; }

        bool isWalking = _animator != null && _animator.GetBool("isWalking");

        if (!isWalking) { ResetState(); return; }

        if (!_wasWalking)
        {
            _wasWalking = true;
            _timer = stepInterval;
        }

        _timer += Time.deltaTime;
        if (_timer >= stepInterval)
        {
            _timer = 0f;
            PlayStep();
        }
    }

    private void ResetState()
    {
        _timer = 0f;
        _wasWalking = false;
    }

    private void PlayStep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        if (is3D)
        {
            // Skip playing entirely if player is too far — saves audio pool sources
            // for enemies that are actually nearby
            if (!IsPlayerInRange()) return;

            AudioService.PlayRandom(
                footstepClips,
                transform.position,
                volume,
                pitchMin,
                pitchMax,
                spatialBlend: 1f,
                minDistance: minDistance,
                maxDistance: maxDistance
            );
        }
        else
        {
            AudioService.PlayRandom2D(footstepClips, volume, pitchMin, pitchMax);
        }
    }

    private bool IsPlayerInRange()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.transform.position) <= maxDistance;
    }
}