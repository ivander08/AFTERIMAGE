using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class KunaiProjectile : BaseProjectile
{
    public int damageAmount = 1;

    public override void OnHit(Collider other)
    {
        Debug.Log($"Kunai hit: {other.name} on Layer: {LayerMask.LayerToName(other.gameObject.layer)}");

        IDamageable damageable = other.GetComponent<IDamageable>();
        
        if (damageable != null)
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