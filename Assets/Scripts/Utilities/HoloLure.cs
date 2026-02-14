using UnityEngine;
using System.Collections;

public class HoloLure : MonoBehaviour, IDamageable
{
    public float lifetime = 3f;
    public float aggroRadius = 20f;
    public LayerMask enemyLayer;

    private void Start()
    {
        StartCoroutine(LifetimeRoutine());
        AttractEnemies();
    }

    private void OnDestroy()
    {
        ReleaseEnemies();
    }

    void AttractEnemies()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, aggroRadius, enemyLayer);
        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out EnemyBase enemy))
            {
                enemy.SetTargetOverride(transform);
            }
        }
    }

    void ReleaseEnemies()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, aggroRadius, enemyLayer);
        foreach (var col in colliders)
        {
            if (col != null && col.TryGetComponent(out EnemyBase enemy))
            {
                enemy.SetTargetOverride(null);
            }
        }
    }

    IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }

    public void TakeDamage(int damage)
    {
        Destroy(gameObject);
    }
}
