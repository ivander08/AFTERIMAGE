using UnityEngine;
using System.Collections;

public class EnemyGrunt : EnemyBase
{
    public float attackRange = 1.5f;
    public float attackWindup = 0.3f;
    public float attackCooldown = 1.5f;

    private float _lastAttackTime = -99f;
    private bool _isAttacking = false;

    protected override void HandleBehavior()
    {
        if (_isAttacking) return;
        if (!CanAggro()) return;

        Transform target = GetTarget();
        float dist = Vector3.Distance(transform.position, target.position);

        if (dist <= detectRange)
        {
            _agent.SetDestination(target.position);

            if (dist <= attackRange && Time.time >= _lastAttackTime + attackCooldown)
            {
                StartCoroutine(AttackRoutine(target));
            }
        }
    }

    IEnumerator AttackRoutine(Transform target)
    {
        _isAttacking = true;
        _agent.isStopped = true;

        transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
        rend.material.color = Color.red;

        yield return new WaitForSeconds(attackWindup);

        if (target != null && Vector3.Distance(transform.position, target.position) <= attackRange + 0.5f)
        {
            if (target.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damage);
            }
        }

        rend.material.color = Color.white; 
        yield return new WaitForSeconds(0.5f);
        
        _lastAttackTime = Time.time;
        _agent.isStopped = false;
        _isAttacking = false;
    }
}
