using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    public int health = 1;
    public float detectRange = 15f;
    public int damage = 1;
    public bool isInvulnerable = false;
    public int scoreValue = 100;
    [SerializeField] public bool showGizmos = false;
    public AudioClip[] deathSounds;
    public AudioClip[] hitSounds;
    public GameObject deathVFXPrefab;
    protected NavMeshAgent _agent;
    protected Rigidbody _rb;
    protected Animator _animator;
    protected Transform _defaultTarget;
    protected Transform _currentTarget;
    protected bool _isDead = false;
    protected bool _isStunned = false;
    protected bool _isKnockedBack = false;
    protected Room _myRoom;
    protected PlayerHealth _playerHealth;

    public bool IsDead => _isDead;
    public event Action OnDeath;

    protected bool IsPlayerDead => _playerHealth != null && _playerHealth.isDead;
    protected bool ShouldAbortAttack(Transform target) => target == null || IsPlayerDead;

    protected virtual void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponentInChildren<Animator>();
        
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _defaultTarget = p.transform;
        if (p != null) _playerHealth = p.GetComponent<PlayerHealth>();
        
        _currentTarget = _defaultTarget;
        _rb.isKinematic = true;
    }

    public void AssignRoom(Room room)
    {
        _myRoom = room;
    }

    protected bool CanAggro()
    {
        return _myRoom != null && RoomManager.Instance.CurrentRoom == _myRoom && !IsPlayerDead;
    }

    public Transform GetTarget()
    {
        return _currentTarget != null ? _currentTarget : _defaultTarget;
    }

    public void SetTargetOverride(Transform newTarget)
    {
        _currentTarget = newTarget != null ? newTarget : _defaultTarget;
    }

    protected virtual void Update()
    {
        if (_isDead || _isStunned || _isKnockedBack || _currentTarget == null || IsPlayerDead) return;
        HandleBehavior();
    }

    protected abstract void HandleBehavior();

    public virtual void TakeDamage(int damage)
    {
        if (isInvulnerable) return;
        if (_isDead) return;
        health -= damage;

        AudioService.PlayRandom(hitSounds, transform.position, 2f, 0.95f, 1.05f);

        if (health <= 0) Die();
    }

    protected virtual void Die()
    {
        _isDead = true;

        AudioService.PlayRandom(deathSounds, transform.position, 0.9f, 0.95f, 1.05f);

        if (deathVFXPrefab != null)
        {
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddKillScore(scoreValue, gameObject.name);
        }
        
        
        ResetPhysicsState(); 
        _agent.enabled = false; 
        StopAllCoroutines();
        GetComponent<Collider>().enabled = false;

        if (_animator != null)
        {
            _animator.SetInteger("deathIndex", UnityEngine.Random.Range(0, 3));
            _animator.SetTrigger("deathTrigger");
        }

        OnDeath?.Invoke();
        // Destroy(gameObject, 3f); 
    }

    public void SetHighlight(bool active)
    {
        if (_isDead) return;
        EnemyDetectionUI ui = GetComponentInChildren<EnemyDetectionUI>();
        if (ui != null)
        {
            ui.SetHighlighted(active);
        }
    }

    public void SetFrozen(bool frozen)
    {
        _isStunned = frozen;
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = frozen;
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }

    public void Stun(float duration)
    {
        if (_isKnockedBack) return; 
        
        StopAllCoroutines();
        ResetPhysicsState(); 
        StartCoroutine(StunRoutine(duration));
    }

    public void Knockback(Vector3 dir, float force, float duration)
    {
        if (_isDead) return;
        
        StopAllCoroutines(); 
        ResetPhysicsState(); 
        
        StartCoroutine(KnockbackRoutine(dir, force, duration));
    }

    private void ResetPhysicsState()
    {
        _isKnockedBack = false;
        _isStunned = false;

        if (_rb != null)
        {
            if (!_rb.isKinematic)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            _rb.constraints = RigidbodyConstraints.None;
            _rb.linearDamping = 0f;
            _rb.isKinematic = true;
        }

        if (_agent != null && gameObject.activeSelf)
        {
            _agent.enabled = true;
            if (!_isDead) _agent.isStopped = false;
        }
    }

    IEnumerator KnockbackRoutine(Vector3 dir, float force, float duration)
    {
        _isKnockedBack = true;
        _isStunned = true;

        if (_agent.isOnNavMesh) _agent.isStopped = true;
        _agent.enabled = false;
        
        _rb.isKinematic = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rb.linearDamping = 10f;

        Vector3 knockbackForce = dir.normalized * force;
        _rb.AddForce(knockbackForce, ForceMode.Impulse);

        yield return new WaitForSeconds(duration);

        if (!_isDead)
        {
            ResetPhysicsState();
        }
    }

    IEnumerator StunRoutine(float duration)
    {
        _isStunned = true;
        if (_agent.isOnNavMesh) _agent.isStopped = true;
        
        yield return new WaitForSeconds(duration);
        
        if (!_isDead)
        {
            _isStunned = false;
            if (_agent.isOnNavMesh) _agent.isStopped = false;
        }
    }
}