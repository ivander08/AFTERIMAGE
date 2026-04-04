using UnityEngine;
using System.Collections;

public class EnemyGrunt : EnemyBase
{
    public float attackRange = 1.5f;
    public float attackWindup = 0.1f;
    public float attackCooldown = 0.5f;

    [SerializeField] private GameObject katanaHip;
    [SerializeField] private GameObject katanaHand;

    private float _lastAttackTime = -99f;
    private bool _isAttacking = false;

    protected override void Awake()
    {
        base.Awake();
        SetKatanaVisible(false);
    }

    private void SetKatanaVisible(bool active)
    {
        if (katanaHip != null)
        {
            katanaHip.SetActive(!active);
        }

        if (katanaHand != null)
        {
            katanaHand.SetActive(active);
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

        SetKatanaVisible(true);

        transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));

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
