using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PlasmaKunaiProjectile : BaseProjectile
{
    public override void OnHit(Collider other)
    {
        EnemyPhalanx phalanxParent = other.GetComponentInParent<EnemyPhalanx>();
        
        if (phalanxParent != null && other.gameObject != phalanxParent.gameObject)
        {
            phalanxParent.BreakShield();
            if (ScoreManager.Instance != null) ScoreManager.Instance.AddUtilityScore("Plasma Kunai");
            Destroy(gameObject);
            return;
        }

        if (other.TryGetComponent(out IDamageable damageable))
        {
            damageable.TakeDamage(1);
            if (ScoreManager.Instance != null) ScoreManager.Instance.AddUtilityScore("Plasma Kunai");
            Destroy(gameObject);
        }
        else if (!other.isTrigger) 
        {
            Destroy(gameObject);
        }
    }
}