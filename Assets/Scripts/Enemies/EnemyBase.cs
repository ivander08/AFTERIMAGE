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
    public Renderer rend;
    [SerializeField] public bool showGizmos = false;
    protected Color _originalColor;
    protected NavMeshAgent _agent;
    protected Rigidbody _rb;
    protected Transform _defaultTarget;
    protected Transform _currentTarget;
    protected bool _isDead = false;
    protected bool _isStunned = false;
    protected bool _isKnockedBack = false;
    protected Room _myRoom;

    public bool IsDead => _isDead;
    public event Action OnDeath;

    protected virtual void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();
        
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _defaultTarget = p.transform;
        
        _currentTarget = _defaultTarget;

        if (rend == null) rend = GetComponent<Renderer>();
        if (rend != null) _originalColor = rend.material.color;
        
        _rb.isKinematic = true;
    }

    public void AssignRoom(Room room)
    {
        _myRoom = room;
    }

    protected bool CanAggro()
    {
        return _myRoom != null && RoomManager.Instance.CurrentRoom == _myRoom;
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
        if (_isDead || _isStunned || _isKnockedBack || _currentTarget == null) return;
        HandleBehavior();
    }

    protected abstract void HandleBehavior();

    public virtual void TakeDamage(int damage)
    {
        if (_isDead) return;
        health -= damage;
        StartCoroutine(FlashWhite());
        if (health <= 0) Die();
    }

    protected virtual void Die()
    {
        _isDead = true;
        ResetPhysicsState(); 
        _agent.enabled = false; 
        StopAllCoroutines();
        if (rend) rend.material.color = Color.black;
        GetComponent<Collider>().enabled = false;
        OnDeath?.Invoke();
        Destroy(gameObject, 1f);
    }

    public void SetHighlight(bool active)
    {
        if (_isDead || rend == null) return;
        if (rend.material.color == _originalColor || rend.material.color == Color.yellow)
        {
            rend.material.color = active ? Color.yellow : _originalColor;
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }

    IEnumerator FlashWhite()
    {
        if (rend != null)
        {
            Color prev = rend.material.color;
            rend.material.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            rend.material.color = prev;
        }
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

        if (rend != null && !_isDead) rend.material.color = _originalColor;
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

        if (rend != null) rend.material.color = Color.blue;

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
        
        if (rend != null) rend.material.color = Color.blue;
        
        yield return new WaitForSeconds(duration);
        
        if (!_isDead)
        {
            _isStunned = false;
            if (_agent.isOnNavMesh) _agent.isStopped = false;
            if (rend != null) rend.material.color = _originalColor;
        }
    }
}