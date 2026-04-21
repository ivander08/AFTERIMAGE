using UnityEngine;
using System.Collections;

public class EnemyShard : EnemyBase
{
    public float attackRange = 1.5f;
    public float attackWindup = 0.1f;
    public float attackCooldown = 0.5f;
    public float moveSpeed = 12f;

    private float _lastAttackTime = -99f;
    private bool _isAttacking = false;

    protected override void Awake()
    {
        base.Awake();
        SetKatanaVisible(false);
        if (_agent != null)
        {
            _agent.speed = moveSpeed;
        }
    }

    private IEnumerator WaitForAttackAnimationEnd()
    {
        yield return null;

        while (_animator != null && _animator.GetCurrentAnimatorStateInfo(0).IsName("Dash"))
        {
            yield return null;
        }

        SetKatanaVisible(false);
    }

    protected override void HandleBehavior()
    {
        if (_isAttacking) return;
        if (!CanAggro()) return;

        Transform target = GetTarget();
        if (target == null) return;
        float dist = Vector3.Distance(transform.position, target.position);

        if (dist <= detectRange)
        {
            _agent.SetDestination(target.position);

            // Update walking animation
            if (_animator != null)
            {
                _animator.SetBool("isWalking", dist > attackRange);
            }

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

        // Draw sword and trigger attack animation
        SetKatanaVisible(true);
        if (_animator != null) _animator.SetTrigger("dashTrigger");
        StartCoroutine(WaitForAttackAnimationEnd());

        yield return new WaitForSeconds(attackWindup);

        if (ShouldAbortAttack(target))
        {
            _agent.isStopped = false;
            _isAttacking = false;
            yield break;
        }

        if (Vector3.Distance(transform.position, target.position) <= attackRange + 0.5f)
        {
            if (target.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damage);
            }
        }

        yield return new WaitForSeconds(0.5f);

        _lastAttackTime = Time.time;
        _agent.isStopped = false;
        _isAttacking = false;
    }
}
