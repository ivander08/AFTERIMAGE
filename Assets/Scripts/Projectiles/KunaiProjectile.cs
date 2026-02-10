using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class KunaiProjectile : BaseProjectile
{
    public int damageAmount = 1;

    public override void OnHit(Collider other)
    {
        Debug.Log($"Kunai hit: {other.name} on Layer: {LayerMask.LayerToName(other.gameObject.layer)}");

        if (other.TryGetComponent(out IDamageable damageable))
        {
            damageable.TakeDamage(damageAmount);
            Destroy(gameObject);
        }
        else if (!other.isTrigger) 
        {
            Destroy(gameObject);
        }
    }
}