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

    public float realizationTime = 0.05f;
    public Transform[] patrolPoints;
    public float minPatrolWait = 1f;
    public float maxPatrolWait = 3f;

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
    
    // Katana system for melee enemies
    [SerializeField] protected GameObject katanaHip;
    [SerializeField] protected GameObject katanaHand;

    public bool IsDead => _isDead;
    public event Action OnDeath;

    public GameObject[] bloodDecalPrefabs;

    protected bool IsPlayerDead => _playerHealth != null && _playerHealth.isDead;
    protected bool ShouldAbortAttack(Transform target) => target == null || IsPlayerDead;

    private int _patrolIndex = 0;
    private bool _isPatrolWaiting = false;
    private float _patrolWaitEndTime = 0f;
    private bool _patrolForward = true;
    private float _aggroStartTime = float.MaxValue;
    private bool _hasRealized = false;

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

    /// <summary>
    /// Sets the katana visibility for melee enemies.
    /// Melee enemies should call SetKatanaVisible(false) in their Awake()
    /// and SetKatanaVisible(true) during attacks.
    /// </summary>
    protected void SetKatanaVisible(bool active)
    {
        if (katanaHip != null) katanaHip.SetActive(!active);
        if (katanaHand != null) katanaHand.SetActive(active);
    }

    public void AssignRoom(Room room)
    {
        _myRoom = room;
    }

    public void NotifyPlayerEnteredRoom()
    {
        _aggroStartTime = Time.time + realizationTime;
        _hasRealized = false;
        
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
        }
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

        if (CanAggro())
        {
            if (Time.time < _aggroStartTime)
            {
                if (_agent.isOnNavMesh) _agent.isStopped = true;
                return;
            }

            if (!_hasRealized)
            {
                _hasRealized = true;
                if (_agent.isOnNavMesh) _agent.isStopped = false;
            }

            HandleBehavior();
        }
        else
        {
            HandlePatrol();
        }
    }

    private void HandlePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        if (_agent == null || !_agent.isOnNavMesh) return;

        if (_isPatrolWaiting)
        {
            SetWalkingAnimation(false);

            if (Time.time >= _patrolWaitEndTime)
            {
                _isPatrolWaiting = false;
                SetNextPatrolPoint();
            }
        }
        else
        {
            SetWalkingAnimation(true);

            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                _isPatrolWaiting = true;
                _patrolWaitEndTime = Time.time + UnityEngine.Random.Range(minPatrolWait, maxPatrolWait);
            }
        }
    }

    private void SetNextPatrolPoint()
    {
        if (patrolPoints.Length <= 1) return;

        if (_patrolForward)
        {
            _patrolIndex++;
            if (_patrolIndex >= patrolPoints.Length)
            {
                _patrolIndex = patrolPoints.Length - 2;
                _patrolForward = false;
            }
        }
        else
        {
            _patrolIndex--;
            if (_patrolIndex < 0)
            {
                _patrolIndex = 1;
                _patrolForward = true;
            }
        }

        if (patrolPoints[_patrolIndex] != null)
        {
            _agent.isStopped = false;
            _agent.SetDestination(patrolPoints[_patrolIndex].position);
            SetWalkingAnimation(true);
        }
    }

    protected void SetWalkingAnimation(bool isWalking)
    {
        if (_animator != null)
        {
            _animator.SetBool("isWalking", isWalking);
        }
    }

    protected abstract void HandleBehavior();

    public virtual void TakeDamage(int damage)
    {
        if (isInvulnerable) return;
        if (_isDead) return;
        health -= damage;

        AudioService.PlayRandom(hitSounds, transform.position, 3f, 0.95f, 1.05f);

        CameraShakeService.Shake(0.8f); 

        if (health <= 0) Die();
    }

    protected virtual void Die()
    {
        _isDead = true;

        SetHighlight(false);
        SetKatanaVisible(false);
        SetWalkingAnimation(false);

        AudioService.PlayRandom(deathSounds, transform.position, 2f, 0.95f, 1.05f);

        if (deathVFXPrefab != null)
        {
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddKillScore(scoreValue, gameObject.name);
        }

        SpawnBloodPool();
        
        ResetPhysicsState(); 
        _agent.enabled = false; 
        StopAllCoroutines();
        GetComponent<Collider>().enabled = false;

        if (_animator != null)
        {
            _animator.ResetTrigger("dashTrigger");
            _animator.SetInteger("deathIndex", UnityEngine.Random.Range(0, 3));
            _animator.SetTrigger("deathTrigger");
        }

        OnDeath?.Invoke();
    }

    private void SpawnBloodPool()
    {
        if (bloodDecalPrefabs == null || bloodDecalPrefabs.Length == 0) return;
        int randomIndex = UnityEngine.Random.Range(0, bloodDecalPrefabs.Length);
        GameObject prefab = bloodDecalPrefabs[randomIndex];
        Vector3 spawnPos = transform.position + Vector3.up * 0.1f; 
        GameObject decal = Instantiate(prefab, spawnPos, prefab.transform.rotation);
        decal.transform.Rotate(Vector3.forward, UnityEngine.Random.Range(0f, 360f), Space.Self);
    }

    public void SetHighlight(bool active)
    {
        if (_isDead && active) return; 

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

        if (frozen)
        {
            SetWalkingAnimation(false);
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
        if (_isDead) return;
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
        SetWalkingAnimation(false);
        
        yield return new WaitForSeconds(duration);
        
        if (!_isDead)
        {
            _isStunned = false;
            if (_agent.isOnNavMesh) _agent.isStopped = false;
        }
    }
}