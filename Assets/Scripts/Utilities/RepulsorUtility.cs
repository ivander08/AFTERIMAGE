using UnityEngine;

public class RepulsorUtility : BaseUtility
{
    public float radius = 5f;
    public float pushForce = 40f;
    public float stunDuration = 1.0f;
    public LayerMask enemyLayer;

    public override string UtilityName => "Repulsor";

    protected override void ExecuteUtility(Transform origin)
    {
        Collider[] colliders = Physics.OverlapSphere(origin.position, radius, enemyLayer);
        
        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out EnemyBase enemy))
            {
                Vector3 direction = (enemy.transform.position - origin.position).normalized;
                float adjustedForce = pushForce;
                
                if (enemy is EnemyPhalanx phalanx && phalanx.HasShield())
                {
                    adjustedForce = phalanx.GetAdjustedRepulsorForce(pushForce);
                    phalanx.BreakShield();
                }
                
                enemy.Knockback(direction, adjustedForce, stunDuration);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
