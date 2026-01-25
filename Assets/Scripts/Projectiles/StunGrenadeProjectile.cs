using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class StunGrenadeProjectile : BaseProjectile
{
    public float explosionRadius = 4f;
    public float stunDuration = 3f;
    public float maxRange = 8f;
    public LayerMask enemyLayer;

    private Vector3 _startPosition;
    private bool _hasExploded = false;

    protected override void Awake()
    {
        base.Awake();
        _startPosition = transform.position;
    }

    protected override void Update()
    {
        if (_hasExploded) return;

        base.Update();

        float distanceTraveled = Vector3.Distance(_startPosition, transform.position);
        if (distanceTraveled >= maxRange)
        {
            Explode();
        }
    }

    public override void OnHit(Collider other)
    {
        if (_hasExploded) return;
        Explode();
    }

    void Explode()
    {
        _hasExploded = true;
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, enemyLayer);
        
        foreach (var hitCollider in hitColliders)
        {
            EnemyBase enemy = hitCollider.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.Stun(stunDuration);
            }
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}