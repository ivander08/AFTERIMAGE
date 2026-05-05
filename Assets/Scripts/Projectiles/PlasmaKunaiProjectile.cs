using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PlasmaKunaiProjectile : BaseProjectile
{
    [Header("VFX")]
    public GameObject hitVfx;
    [Header("Audio")]
    public AudioClip hitSfx;
    public float hitSfxVolume = 1.5f;

    private bool _didPlayHitSfx;

    private void PlayHitSfxOnce()
    {
        if (_didPlayHitSfx || hitSfx == null) return;
        _didPlayHitSfx = true;
        AudioService.PlayClip(hitSfx, transform.position, hitSfxVolume, 1f, minDistance: 16f);
    }

    public override void OnHit(Collider other)
    {
        if (hitVfx != null)
        {
            Instantiate(hitVfx, transform.position, Quaternion.LookRotation(_lastHitNormal));
        }

        // Just blindly apply damage! The EnemyPhalanx's new TakeDamage override
        // will automatically absorb it if the player threw it from the front.
        if (other.TryGetComponent(out IDamageable damageable))
        {
            damageable.TakeDamage(1);
            PlayHitSfxOnce();
            if (ScoreManager.Instance != null) ScoreManager.Instance.AddUtilityScore("Plasma Kunai");
            Destroy(gameObject);
        }
        else if (!other.isTrigger)
        {
            PlayHitSfxOnce();
            Destroy(gameObject);
        }
    }
}