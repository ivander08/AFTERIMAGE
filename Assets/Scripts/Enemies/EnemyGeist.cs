using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyGeist : EnemyBase
{
    public float minSolidTime = 3f;
    public float maxSolidTime = 6f;
    public float minEtherealTime = 2f;
    public float maxEtherealTime = 4f;

    public float attackRange = 1.5f;
    public float attackWindup = 0.3f;
    public float attackCooldown = 1.5f;

    private bool _isEthereal = false;
    private float _lastAttackTime = -99f;
    private bool _isAttacking = false;

    protected override void Awake()
    {
        base.Awake();
        StartCoroutine(PhaseRoutine());
    }

    protected override void HandleBehavior()
    {
        Transform target = GetTarget();
        if (target == null || _isAttacking || !CanAggro()) return;

        _agent.SetDestination(target.position);

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= attackRange && Time.time >= _lastAttackTime + attackCooldown)
        {
            StartCoroutine(AttackRoutine(target));
        }
    }

    IEnumerator AttackRoutine(Transform target)
    {
        _isAttacking = true;
        _agent.isStopped = true;

        if (target != null)
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

        rend.material.color = _originalColor;
        
        yield return new WaitForSeconds(0.5f);
        
        _lastAttackTime = Time.time;
        _agent.isStopped = false;
        _isAttacking = false;
    }

    IEnumerator PhaseRoutine()
    {
        while (!_isDead)
        {
            while (_isStunned) yield return null;

            SetEthereal(false);
            yield return new WaitForSeconds(Random.Range(minSolidTime, maxSolidTime));
            
            while (_isStunned) yield return null;

            SetEthereal(true);
            yield return new WaitForSeconds(Random.Range(minEtherealTime, maxEtherealTime));
        }
    }

    void SetEthereal(bool state)
    {
        if (_isDead) return;
        _isEthereal = state;
        rend.material.color = state ? new Color(_originalColor.r, _originalColor.g, _originalColor.b, 0.3f) : _originalColor;
    }

    public override void TakeDamage(int damageAmount)
    {
        if (_isEthereal) return;
        base.TakeDamage(damageAmount);
    }
}
