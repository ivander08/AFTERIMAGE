using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class EnemyScatter : EnemyBase
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    
    public float maxRange = 10f;
    public float attackCooldown = 2.5f;
    public float attackWindup = 0.4f;
    
    public int projectileCount = 5;
    public float spreadAngle = 30f;
    public float projectileSpeed = 50f;

    private float _lastAttackTime = -999f;
    private bool _isAttacking = false;

    protected override void Awake()
    {
        base.Awake();
        
        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    protected override void HandleBehavior()
    {
        if (!CanAggro() || _isAttacking) return;

        Transform target = GetTarget();
        if (target == null) return;

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > maxRange)
        {
            _agent.SetDestination(target.position);
        }
        else
        {
            _agent.isStopped = true;
            
            if (Time.time >= _lastAttackTime + attackCooldown)
            {
                StartCoroutine(AttackRoutine(target));
            }
        }
    }

    private IEnumerator AttackRoutine(Transform target)
    {
        _isAttacking = true;

        Vector3 directionToTarget = (target.position - transform.position).normalized;
        directionToTarget.y = 0;
        transform.rotation = Quaternion.LookRotation(directionToTarget);
        
        Vector3 lockedAimDirection = (target.position - firePoint.position).normalized;

        if (rend != null)
        {
            rend.material.color = Color.red;
        }
        
        yield return new WaitForSeconds(attackWindup);

        if (!_isDead)
        {
            FireSpreadProjectiles(lockedAimDirection);
        }

        if (rend != null)
        {
            rend.material.color = _originalColor;
        }
        
        yield return new WaitForSeconds(0.5f);

        _lastAttackTime = Time.time;
        _isAttacking = false;
    }

    private void FireSpreadProjectiles(Vector3 baseDirection)
    {
        if (projectilePrefab == null) return;
        
        float angleStep = spreadAngle / (projectileCount - 1);
        float startAngle = -spreadAngle / 2f;

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * baseDirection;

            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            
            if (projectile.TryGetComponent(out ScatterProjectile scatterProj))
            {
                scatterProj.Initialize(direction, projectileSpeed, damage);
            }
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (!showGizmos) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxRange);
    }
}
