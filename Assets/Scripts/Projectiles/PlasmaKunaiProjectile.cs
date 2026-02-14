using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PlasmaKunaiProjectile : BaseProjectile
{
    public override void OnHit(Collider other)
    {
        Debug.Log($"Plasma Kunai hit: {other.name}");
        
        EnemyPhalanx phalanxParent = other.GetComponentInParent<EnemyPhalanx>();
        
        if (phalanxParent != null && other.gameObject != phalanxParent.gameObject)
        {
            Debug.Log($"[PlasmaKunai] Hit shield!");
            phalanxParent.BreakShield();
            Destroy(gameObject);
            return;
        }

        if (other.TryGetComponent(out IDamageable damageable))
        {
            Debug.Log($"[PlasmaKunai] Damage dealt");
            damageable.TakeDamage(1);
            Destroy(gameObject);
        }
        else if (!other.isTrigger) 
        {
            Destroy(gameObject);
        }
    }
}