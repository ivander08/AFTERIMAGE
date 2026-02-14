using UnityEngine;

public class EnemyPhalanx : EnemyBase
{
    public GameObject shield;
    public float repulsorForceReduction = 0.7f;
    public float attackRange = 2f;
    // public int shieldKnockbackForce = 30;
    public float attackCooldown = 1.5f;
    public float attackWindup = 0.3f;

    private GameObject _shieldInstance;
    private bool _shieldActive = true;
    private float _lastAttackTime = -99f;
    private bool _isAttacking = false;

    protected override void Awake()
    {
        base.Awake();
        AssignShield();
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

    protected override void HandleBehavior()
    {
        if (_isAttacking) return;
        if (!CanAggro()) return;

        Transform target = GetTarget();
        if (target == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, target.position);
        
        _agent.SetDestination(target.position);

        if (distanceToPlayer <= attackRange && Time.time >= _lastAttackTime + attackCooldown)
        {
            StartCoroutine(AttackRoutine(target));
        }
    }

    private System.Collections.IEnumerator AttackRoutine(Transform target)
    {
        _isAttacking = true;
        _agent.isStopped = true;

        transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
        rend.material.color = Color.red;
        Debug.Log($"[EnemyPhalanx] {name} attacking player!");

        yield return new WaitForSeconds(attackWindup);

        if (target != null && Vector3.Distance(transform.position, target.position) <= attackRange + 0.5f)
        {
            if (target.TryGetComponent(out IDamageable damageable))
            {
                Debug.Log($"[EnemyPhalanx] Damage dealt to player!");
                damageable.TakeDamage(damage);
            }
        }

        rend.material.color = _originalColor;
        yield return new WaitForSeconds(0.5f);

        _lastAttackTime = Time.time;
        _agent.isStopped = false;
        _isAttacking = false;
    }

    public void BreakShield()
    {
        if (!_shieldActive) return;

        Debug.Log($"[EnemyPhalanx] Shield broken");
        _shieldActive = false;
        if (_shieldInstance != null)
        {
            _shieldInstance.SetActive(false);
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
}
