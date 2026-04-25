// Assets/Scripts/ThrowableObject.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ThrowableObject : MonoBehaviour
{
    public float throwSpeed = 50f;
    public float detectionRange = 15f;
    public float stunDuration = 1f;
    public LayerMask enemyLayer = -1;
    public LayerMask obstacleLayer = -1;

    public float playerPickupRadius = 1.2f;

    private bool _hasBeenThrown = false;
    private EnemyBase _targetEnemy;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        gameObject.layer = LayerMask.NameToLayer("Throwable");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasBeenThrown)
        {
            // Only care about enemies after thrown
            if (other.TryGetComponent(out EnemyBase enemy))
            {
                HitEnemy(enemy);
            }
            return;
        }

        // Before thrown: pickup on player touch
        if (other.CompareTag("Player"))
        {
            ThrowAtNearestEnemy();
        }
    }

    private void Update()
    {
        if (_hasBeenThrown && _targetEnemy != null)
        {
            MoveTowardTarget();
        }
    }

    void ThrowAtNearestEnemy()
    {
        EnemyBase nearestEnemy = FindNearestVisibleEnemy();

        if (nearestEnemy == null)
        {
            Debug.Log("[ThrowableObject] No visible enemies found.");
            return;
        }

        _targetEnemy = nearestEnemy;
        _hasBeenThrown = true;

        Debug.Log($"[ThrowableObject] Throwing {gameObject.name} at {_targetEnemy.name}");
    }

    EnemyBase FindNearestVisibleEnemy()
    {
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, detectionRange, enemyLayer);

        EnemyBase nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (var col in enemiesInRange)
        {
            if (!col.TryGetComponent(out EnemyBase enemy)) continue;
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < nearestDistance && IsEnemyVisible(enemy))
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }

    bool IsEnemyVisible(EnemyBase enemy)
    {
        Vector3 dir = (enemy.transform.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, enemy.transform.position);
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, dir);

        // obstacleLayer should NOT include "Throwable" layer
        return !Physics.Raycast(ray, out _, dist, obstacleLayer);
    }

    void MoveTowardTarget()
    {
        if (_targetEnemy == null || _targetEnemy.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = (_targetEnemy.transform.position - transform.position).normalized;
        transform.Translate(dir * throwSpeed * Time.deltaTime, Space.World);

        if (Vector3.Distance(transform.position, _targetEnemy.transform.position) < 0.5f)
        {
            HitEnemy(_targetEnemy);
        }
    }

    void HitEnemy(EnemyBase enemy)
    {
        if (enemy != null)
        {
            enemy.Stun(stunDuration);
            if (ScoreManager.Instance != null) ScoreManager.Instance.AddThrowableBonus();
            CameraShakeService.Shake(0.5f);
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}