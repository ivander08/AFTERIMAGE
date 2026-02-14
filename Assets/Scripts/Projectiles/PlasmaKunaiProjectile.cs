using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PlasmaKunaiProjectile : BaseProjectile
{
    public override void OnHit(Collider other)
    {
        Debug.Log($"Plasma Kunai hit: {other.name} on Layer: {LayerMask.LayerToName(other.gameObject.layer)}");

        if (other.TryGetComponent(out IDamageable damageable))
        {
            damageable.TakeDamage(1);
            Destroy(gameObject);
        }
        else if (!other.isTrigger) 
        {
            Destroy(gameObject);
        }
    }
}