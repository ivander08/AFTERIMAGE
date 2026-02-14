using UnityEngine;
using System.Collections;

public class EnemyMelee : EnemyBase
{
    public float attackRange = 1.5f;
    public float attackWindup = 0.3f;
    public float attackCooldown = 1.5f;

    private float _lastAttackTime = -99f;
    private bool _isAttacking = false;

    protected override void HandleBehavior()
    {
        if (_isAttacking) return;

        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist <= detectRange && CanAggro())
        {
            _agent.SetDestination(_player.position);

            if (dist <= attackRange)
            {
                if (Time.time >= _lastAttackTime + attackCooldown)
                {
                    StartCoroutine(AttackRoutine());
                }
            }
        }
    }

    IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        _agent.isStopped = true;

        transform.LookAt(new Vector3(_player.position.x, transform.position.y, _player.position.z));
        rend.material.color = Color.red;

        yield return new WaitForSeconds(attackWindup);

        if (_player != null && Vector3.Distance(transform.position, _player.position) <= attackRange + 0.5f)
        {
            if (_player.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damage);
            }
        }

        rend.material.color = _originalColor;
        
        yield return new WaitForSeconds(0.5f);
        
        _lastAttackTime = Time.time;
        _agent.isStopped = false;
        _isAttacking = false;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (!showGizmos) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}