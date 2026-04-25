using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class EnemyGeist : EnemyBase
{
    public float minSolidTime = 0.5f;
    public float maxSolidTime = 1.5f;
    public float minEtherealTime = 0.5f;
    public float maxEtherealTime = 1.5f;

    public float attackRange = 1.5f;
    public float attackWindup = 0.1f;
    public float attackCooldown = 0.5f;

    private bool _isEthereal = false;
    private float _lastAttackTime = -99f;
    private bool _isAttacking = false;
    private Renderer[] _renderers;
    private Dictionary<Material, Color> _originalEmission = new Dictionary<Material, Color>();

    protected override void Awake()
    {
        base.Awake();
        _renderers = GetComponentsInChildren<Renderer>();
        
        // Cache original emission colors
        foreach (var r in _renderers)
        {
            foreach (var mat in r.materials)
            {
                if (mat.HasProperty("_EmissionColor"))
                    _originalEmission[mat] = mat.GetColor("_EmissionColor");
            }
        }
        
        SetKatanaVisible(false);
        StartCoroutine(PhaseRoutine());
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
        Transform target = GetTarget();
        if (target == null || _isAttacking || !CanAggro()) return;

        _agent.SetDestination(target.position);

        float dist = Vector3.Distance(transform.position, target.position);
        
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

    IEnumerator AttackRoutine(Transform target)
    {
        _isAttacking = true;
        _agent.isStopped = true;

        if (target != null)
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

        float alpha = state ? 0.3f : 1f;
        foreach (var r in _renderers)
        {
            foreach (var mat in r.materials)
            {
                if (!mat.HasProperty("_Color")) continue;
                Color c = mat.color;
                c.a = alpha;
                mat.color = c;

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", state ? Color.black : 
                        (_originalEmission.TryGetValue(mat, out Color orig) ? orig : Color.black));
                }
            }
        }
    }

    public override void TakeDamage(int damageAmount)
    {
        if (_isEthereal) return;
        base.TakeDamage(damageAmount);
    }
}
