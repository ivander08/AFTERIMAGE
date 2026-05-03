using UnityEngine;

/// <summary>
/// Projectile thrown by the Echo boss. Hits the player or obstacles,
/// but passes through other enemies.
/// Overrides trigger detection since BaseProjectile ignores "Player" tag.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class EnemyPlasmaKunai : BaseProjectile
{
    [Header("VFX")]
    public GameObject hitVfx;

    [Header("Audio")]
    public AudioClip hitSfx;

    [Header("Damage")]
    public int kunaiDamage = 1;

    private bool _didHit;

    /// <summary>
    /// Override trigger detection to handle Player hits
    /// (BaseProjectile skips the Player tag).
    /// </summary>
    protected override void OnTriggerEnter(Collider other)
    {
        // BaseProjectile.OnTriggerEnter skips "Player" — we let everything through
        // and let OnHit decide what to do.
        // We skip "Enemy" so the kunai passes through other enemies.
        if (other.CompareTag("Enemy")) return;

        _lastHitNormal = -transform.forward;
        OnHit(other);
    }

    public override void OnHit(Collider other)
    {
        // --- Player hit: deal damage ---
        if (other.CompareTag("Player"))
        {
            if (_didHit) return;
            _didHit = true;

            if (other.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(kunaiDamage);
            }

            SpawnVfxAndSfx();
            Destroy(gameObject);
            return;
        }

        // --- Ignore enemies and triggers (pass through harmlessly) ---
        if (other.CompareTag("Enemy") || other.isTrigger)
            return;

        // --- Obstacle hit: non-trigger, non-Enemy ---
        if (_didHit) return;
        _didHit = true;

        SpawnVfxAndSfx();
        Destroy(gameObject);
    }

    private void SpawnVfxAndSfx()
    {
        if (hitVfx != null)
        {
            Instantiate(hitVfx, transform.position, Quaternion.LookRotation(_lastHitNormal));
        }

        if (hitSfx != null)
        {
            AudioService.PlayClip(hitSfx, transform.position, volume: 1.5f, pitch: 1f, minDistance: 2f);
        }
    }
}
