using UnityEngine;
using System.Collections;

/// <summary>
/// Shield-bearing melee enemy. The shield must be broken
/// before the Phalanx can be damaged normally.
/// </summary>

public class EnemyPhalanx : EnemyBase
{
    public GameObject shield;
    [SerializeField] private GameObject shieldCollider;
    public float repulsorForceReduction = 0.7f;
    public float attackRange = 2f;
    public float attackCooldown = 0.5f;
    public float attackWindup = 0.1f;
    
    [SerializeField] private GameObject shieldBreakVFX;
    [SerializeField] private AudioClip shieldBreakSFX;

    private GameObject _shieldInstance;
    private bool _shieldActive = true;
    private float _lastAttackTime = -99f;
    private bool _isAttacking = false;

    protected override void Awake()
    {
        base.Awake();
        AssignShield();
        SetKatanaVisible(false);
    }

    private void AssignShield()
    {
        if (shield != null)
        {
            _shieldInstance = shield;
        }
        else
        {
            Transform shieldChild = transform.Find("Shield");
            if (shieldChild != null)
            {
                _shieldInstance = shieldChild.gameObject;
            }
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

        float distanceToPlayer = Vector3.Distance(transform.position, target.position);
        
        _agent.SetDestination(target.position);

        // Update walking animation
        if (_animator != null)
        {
            _animator.SetBool("isWalking", distanceToPlayer > attackRange);
        }

        if (distanceToPlayer <= attackRange && Time.time >= _lastAttackTime + attackCooldown)
        {
            StartCoroutine(AttackRoutine(target));
        }
    }

    private IEnumerator AttackRoutine(Transform target)
    {
        _isAttacking = true;
        _agent.isStopped = true;

        transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));

        // Fire attack animation and draw sword
        SetKatanaVisible(true);
        if (_animator != null) _animator.SetTrigger("dashTrigger");
        StartCoroutine(WaitForAttackAnimationEnd());

        // Play slash sound
        if (slashSound != null)
            AudioService.PlayClip(slashSound, transform.position, volume: 1.5f, pitch: Random.Range(0.95f, 1.05f));

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

    public void BreakShield()
    {
        if (!_shieldActive) return;

        _shieldActive = false;
        
        if (_shieldInstance != null)
            _shieldInstance.SetActive(false);
        
        if (shieldCollider != null)
            shieldCollider.SetActive(false);

        // Spawn VFX at shield position
        if (shieldBreakVFX != null)
        {
            Vector3 spawnPos = _shieldInstance != null ? _shieldInstance.transform.position : transform.position;
            Instantiate(shieldBreakVFX, spawnPos, Quaternion.identity);
        }

        // Play shield break sound
        if (shieldBreakSFX != null)
        {
            AudioService.PlayClip2D(shieldBreakSFX, 0.5f, 1f);
        }
    }

    public bool HasShield() => _shieldActive;

    public float GetAdjustedRepulsorForce(float baseForce)
    {
        // Reduce repulsor force if shield is active
        if (_shieldActive)
        {
            return baseForce * repulsorForceReduction;
        }
        return baseForce;
    }

    protected override void Die()
    {
        BreakShield();
        base.Die();
    }

}