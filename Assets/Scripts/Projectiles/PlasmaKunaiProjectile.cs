using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PlasmaKunaiProjectile : BaseProjectile
{
    [Header("Audio")]
    public AudioClip hitSfx;
    public float hitSfxVolume = 1.5f;

    private bool _didPlayHitSfx;

    private void PlayHitSfxOnce()
    {
        if (_didPlayHitSfx || hitSfx == null) return;
        _didPlayHitSfx = true;
        AudioService.PlayClip(hitSfx, transform.position, hitSfxVolume, 1f);
    }

    public override void OnHit(Collider other)
    {
        EnemyPhalanx phalanxParent = other.GetComponentInParent<EnemyPhalanx>();
        
        if (phalanxParent != null && other.gameObject != phalanxParent.gameObject)
        {
            phalanxParent.BreakShield();
            PlayHitSfxOnce();
            if (ScoreManager.Instance != null) ScoreManager.Instance.AddUtilityScore("Plasma Kunai");
            Destroy(gameObject);
            return;
        }

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