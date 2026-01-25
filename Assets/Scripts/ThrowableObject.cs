using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ThrowableObject : MonoBehaviour
{
    public float throwSpeed = 50f;
    public float detectionRange = 15f;
    public float stunDuration = 1f;
    public LayerMask enemyLayer = -1;
    public LayerMask obstacleLayer = -1;

    private bool _hasBeenThrown = false;
    private EnemyBase _targetEnemy;
    private Vector3 _throwDirection;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasBeenThrown) return;
        
        if (other.CompareTag("Player"))
        {
            ThrowAtNearestEnemy();
        }
        else if (_hasBeenThrown && other.GetComponent<EnemyBase>() != null)
        {
            HitEnemy(other.GetComponent<EnemyBase>());
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
            Debug.Log("No visible enemies found to throw at");
            return;
        }

        _targetEnemy = nearestEnemy;
        _throwDirection = (_targetEnemy.transform.position - transform.position).normalized;
        _hasBeenThrown = true;
        
        Debug.Log($"Throwing {gameObject.name} at {_targetEnemy.name}");
    }

    EnemyBase FindNearestVisibleEnemy()
    {
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, detectionRange, enemyLayer);
        
        EnemyBase nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (var enemyCollider in enemiesInRange)
        {
            EnemyBase enemy = enemyCollider.GetComponent<EnemyBase>();
            if (enemy == null) continue;

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
        Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
        float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);

        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, directionToEnemy);
        
        if (Physics.Raycast(ray, out RaycastHit hit, distanceToEnemy, obstacleLayer))
        {
            return false;
        }

        return true;
    }

    void MoveTowardTarget()
    {
        if (_targetEnemy == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 currentDirection = (_targetEnemy.transform.position - transform.position).normalized;
        transform.Translate(currentDirection * throwSpeed * Time.deltaTime, Space.World);

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
            Debug.Log($"{gameObject.name} stunned {enemy.name} for {stunDuration}s");
        }
        
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}