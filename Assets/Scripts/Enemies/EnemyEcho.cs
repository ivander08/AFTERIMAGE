using UnityEngine;
using System.Collections;

/// <summary>
/// The "Echo" boss — a perfect clone of the player that tests reaction speed.
/// Uses an Option-B AI loop: threat detection (dodge incoming attacks) with high priority,
/// then proactive offense (dash attacks / thrown kunai).
/// Features a Clash mechanic when both Echo and the player dash into each other,
/// and dodges away instantly when hit instead of being stunned.
/// </summary>
public class EnemyEcho : EnemyBase
{
    #region Inspector Fields — Dash Attack
    [Header("Dash Attack")]
    public float dashSpeed = 40f;
    public float dashRange = 5f;
    public float dashWindup = 0.15f;
    public float dashCooldown = 0.5f;
    #endregion//

    #region Inspector Fields — Dodge
    [Header("Dodge")]
    public float dodgeCooldown = 1.5f;
    public float dodgeDistance = 4f;
    public float dodgeSpeed = 30f;
    #endregion

    #region Inspector Fields — Arsenal
    [Header("Arsenal")]
    public GameObject enemyKunaiPrefab;
    public Transform throwPoint;
    #endregion

    #region Inspector Fields — Audio
    [Header("Audio")]
    public AudioClip dashSound;
    public AudioClip throwSfx;
    #endregion

    #region Inspector Fields — VFX & Hit Reaction
    [Header("VFX & Hit Reaction")]
    public GameObject clashVfxPrefab;
    public AudioClip clashSfx;
    public GameObject hitVfxPrefab;
    public float hitDodgeDistance = 8f;
    public float hitStunDuration = 1f;
    #endregion

    #region Inspector Fields — Detection
    [Header("Detection")]
    public LayerMask playerProjectileLayer;
    #endregion

    #region Events
    /// <summary>
    /// Fires whenever Echo's HP changes. Int parameter = current HP.
    /// Used by EchoArenaController to update boss HP UI.
    /// </summary>
    public event System.Action<int> OnHpChanged;
    #endregion

    #region Private State
    private PlayerDash _playerDash;
    private Coroutine _currentRoutine;
    private bool _isPerformingAction = false;
    private bool _isDashing = false;
    private float _lastDodgeTime = -99f;
    private float _lastDashTime = -99f;
    private int _kunaiCharges;
    private bool _dealtDashDamage = false;

    public float dashChance = 0.8f;
    public float kunaiMinDistance = 8f;

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();

        // Katana is hidden by default; shown only during dash attacks
        SetKatanaVisible(false);

        // Cache PlayerDash reference for clash & threat detection
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerDash = player.GetComponent<PlayerDash>();
        }

        // Kunai charges: exactly 1 throw per HP segment.
        // Replenished when Echo takes damage (loses an HP bar).
        _kunaiCharges = 1;
    }

    #endregion

    #region AI Loop — "Option B"

    protected override void HandleBehavior()
    {
        if (!CanAggro()) return;

        Transform target = GetTarget();
        if (target == null) return;

        // ── Step 1: Threat Detection (Highest Priority) ──
        if (CanDodge() && DetectThreat(target))
        {
            Debug.Log("[Echo] Threat detected! Dodging & countering.");
            InterruptCurrentAction();
            _currentRoutine = StartCoroutine(DodgeAndCounterRoutine(target));
            return;
        }

        // ── Step 2: Proactive Offense ──
        if (!_isPerformingAction)
        {
            Debug.Log("[Echo] No threat, starting action loop.");
            _currentRoutine = StartCoroutine(ActionLoopRoutine(target));
        }
    }

    /// <summary>
    /// Checks if the dodge cooldown has passed and Echo isn't currently dashing.
    /// </summary>
    private bool CanDodge()
    {
        return Time.time >= _lastDodgeTime + dodgeCooldown && !_isDashing;
    }

    /// <summary>
    /// Detects incoming threats: player dashing nearby OR player projectiles.
    /// </summary>
    private bool DetectThreat(Transform target)
    {
        float distToPlayer = Vector3.Distance(transform.position, target.position);

        // Threat 1: Player is dashing within 7 units
        if (_playerDash != null && _playerDash.IsPlayerDashing() && distToPlayer < 7f)
            return true;

        // Threat 2: Player projectiles detected within detectRange
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, detectRange, playerProjectileLayer);
        foreach (var col in nearbyColliders)
        {
            if (col.TryGetComponent(out BaseProjectile _))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Stops the currently running coroutine and resets state flags.
    /// </summary>
    private void InterruptCurrentAction()
    {
        if (_currentRoutine != null)
        {
            StopCoroutine(_currentRoutine);
            _currentRoutine = null;
        }
        _isPerformingAction = false;
        _isDashing = false;

        // Restore agent state if it was disabled by an action
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
        }
    }

    #endregion

    #region Action Loop

    /// <summary>
    /// Main proactive loop: faces the player, then rolls for an action.
    /// 80% chance of a dash attack, 20% chance of throwing a kunai.
    /// Respects dash cooldown and HP-gated kunai charges.
    /// </summary>
    private IEnumerator ActionLoopRoutine(Transform target)
    {
        _isPerformingAction = true;

        FaceTarget(target);

        bool canDash = Time.time >= _lastDashTime + dashCooldown;
        float distToPlayer = Vector3.Distance(transform.position, target.position);
        bool canThrow = _kunaiCharges > 0 && distToPlayer >= kunaiMinDistance;

        if (canDash && canThrow)
        {
            if (Random.value < 0.8f)
            {
                Debug.Log("[Echo] Action: Dash Attack (80% roll)");
                yield return DashAttackRoutine(target);
            }
            else
            {
                Debug.Log("[Echo] Action: Throw Kunai (20% roll)");
                yield return ThrowKunaiRoutine(target);
            }
        }
        else if (canDash)
        {
            Debug.Log("[Echo] Action: Dash Attack (only option)");
            yield return DashAttackRoutine(target);
        }
        else if (canThrow)
        {
            Debug.Log("[Echo] Action: Throw Kunai (only option)");
            yield return ThrowKunaiRoutine(target);
        }
        else
        {
            Debug.Log("[Echo] Action: Nothing available (dash on cooldown, no kunai charges)");
        }

        _isPerformingAction = false;
    }

    #endregion

    #region Dodge & Counter

    /// <summary>
    /// Dodges backward away from the player, then immediately throws a kunai to punish.
    /// Plays dodge SFX.
    /// </summary>
    private IEnumerator DodgeAndCounterRoutine(Transform target)
    {
        Debug.Log("[Echo] Dodge & Counter started.");
        _isPerformingAction = true;
        _lastDodgeTime = Time.time;

        if (dashSound != null)
        {
            AudioService.PlayClip(dashSound, transform.position, volume: 1.5f, pitch: 1f, minDistance: 10f);
        }

        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        // Calculate backward direction (away from player)
        Vector3 dodgeDir = (transform.position - target.position).normalized;
        dodgeDir.y = 0;
        if (dodgeDir.sqrMagnitude < 0.01f) dodgeDir = -target.forward;

        // Face the player while moving backward
        FaceTarget(target);

        // Slide backward over dodgeDistance
        float distanceTraveled = 0f;
        while (distanceTraveled < dodgeDistance)
        {
            float step = dodgeSpeed * Time.deltaTime;
            float remaining = dodgeDistance - distanceTraveled;
            if (step > remaining) step = remaining;

            transform.position += dodgeDir * step;
            distanceTraveled += step;
            yield return null;
        }

        // ── Counter-attack: throw a kunai immediately ──
        yield return ThrowKunaiRoutine(target);

        _isPerformingAction = false;
    }

    #endregion

    #region Dash Attack

    private IEnumerator DashAttackRoutine(Transform target)
    {
        Debug.Log("[Echo] Dash Attack started.");
        _lastDashTime = Time.time;

        // Windup
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        SetKatanaVisible(true);
        _dealtDashDamage = false;

        // Face target
        Vector3 dashDir = (target.position - transform.position).normalized;
        dashDir.y = 0;
        if (dashDir.sqrMagnitude < 0.01f) dashDir = transform.forward;
        transform.forward = dashDir;

        // Windup animation
        if (_animator != null)
        {
            _animator.SetBool("isWalking", false);
        }

        yield return new WaitForSeconds(dashWindup);

        if (ShouldAbortAttack(target))
        {
            SetKatanaVisible(false);
            yield break;
        }

        // Play dash SFX + slash SFX
        if (dashSound != null)
        {
            AudioService.PlayClip(dashSound, transform.position, volume: 1.5f, pitch: 1f, minDistance: 10f);
        }
        if (slashSound != null)
        {
            AudioService.PlayClip(slashSound, transform.position, volume: 1.5f, pitch: Random.Range(0.95f, 1.05f), minDistance: 6f);
        }

        // Begin dash
        _isDashing = true;
        if (_animator != null)
        {
            _animator.SetTrigger("dashTrigger");
        }

        // Recalculate direction after windup (player may have moved)
        dashDir = (target.position - transform.position).normalized;
        dashDir.y = 0;
        if (dashDir.sqrMagnitude < 0.01f) dashDir = transform.forward;
        transform.forward = dashDir;

        // Dash forward
        float distanceTraveled = 0f;
        while (distanceTraveled < dashRange)
        {
            float step = dashSpeed * Time.deltaTime;
            float remaining = dashRange - distanceTraveled;
            if (step > remaining) step = remaining;

            transform.position += dashDir * step;
            distanceTraveled += step;

            // Check for player collision during dash
            if (!_dealtDashDamage)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, 1.2f);
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Player") && hit.TryGetComponent(out IDamageable damageable))
                    {
                        Debug.Log("[Echo] Dash hit the player!");
                        damageable.TakeDamage(damage);
                        _dealtDashDamage = true;
                        break;
                    }
                }
            }

            yield return null;

            // Abort if player died during the dash
            if (IsPlayerDead) break;
        }

        // Cleanup
        _isDashing = false;
        SetKatanaVisible(false);
    }

    #endregion

    #region Throw Kunai

    /// <summary>
    /// Instantiates an EnemyPlasmaKunai projectile aimed at the player.
    /// </summary>
    private IEnumerator ThrowKunaiRoutine(Transform target)
    {
        if (_kunaiCharges <= 0) yield break;
        if (enemyKunaiPrefab == null || throwPoint == null) yield break;

        _kunaiCharges--;
        Debug.Log($"[Echo] Threw a kunai! {_kunaiCharges} charges remaining.");

        FaceTarget(target);

        // Calculate direction to player
        Vector3 throwDir = (target.position - throwPoint.position).normalized;
        throwDir.y = 0;
        if (throwDir.sqrMagnitude < 0.01f) throwDir = transform.forward;

        // Throw SFX
        if (throwSfx != null)
        {
            AudioService.PlayClip(throwSfx, transform.position, volume: 1.5f, pitch: Random.Range(0.95f, 1.05f), minDistance: 6f);
        }

        // Spawn the projectile
        GameObject kunai = Instantiate(
            enemyKunaiPrefab,
            throwPoint.position,
            Quaternion.LookRotation(throwDir)
        );

        // Small delay so the kunai throw feels distinct
        yield return new WaitForSeconds(0.3f);
    }

    #endregion

    #region TakeDamage — Clash & Dodge-Away

    public override void TakeDamage(int damage)
    {
        if (isInvulnerable) return;
        if (_isDead) return;

        // ── CLASH CHECK ──
        if (_isDashing && _playerDash != null && _playerDash.IsPlayerDashing())
        {
            Debug.Log("[Echo] CLASH! Both dashing — zero damage.");
            HandleClash();
            return;
        }

        // ── NORMAL DAMAGE ──
        health -= damage;
        Debug.Log($"[Echo] Took {damage} damage — HP: {health}");
        OnHpChanged?.Invoke(health);

        AudioService.PlayRandom(hitSounds, transform.position, 3f, 0.95f, 1.05f, minDistance: 10f);
        AudioService.PlayRandom(deathSounds, transform.position, 3f, 0.95f, 1.05f, minDistance: 10f);
        CameraShakeService.Shake(0.4f);

        if (hitVfxPrefab != null)
        {
            Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);
        }

        // Blood decal on hit (uses base class bloodDecalPrefabs array)
        if (bloodDecalPrefabs != null && bloodDecalPrefabs.Length > 0)
        {
            int randomIndex = Random.Range(0, bloodDecalPrefabs.Length);
            GameObject prefab = bloodDecalPrefabs[randomIndex];
            Vector3 bloodPos = transform.position + Vector3.up * 0.1f;
            GameObject decal = Instantiate(prefab, bloodPos, prefab.transform.rotation);
            decal.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);
        }

        // Replenish kunai charge on HP loss
        _kunaiCharges = 1;
        Debug.Log($"[Echo] Kunai charge replenished. Charges: {_kunaiCharges}");

        if (health <= 0)
        {
            Debug.Log("[Echo] HEALTH <= 0 — DYING.");
            Die();
            return;
        }

        // ── DODGE AWAY + STUN ──
        Debug.Log("[Echo] Hit reaction: dodge 8u + stun 1s.");
        StartCoroutine(DodgeAwayOnHit());
    }

    /// <summary>
    /// Clash: Both Echo and the player dashed into each other.
    /// Both get knocked back ~4 units, creating a rhythmic clash chain.
    /// Echo takes zero damage and can dash again immediately.
    /// </summary>
    private void HandleClash()
    {
        Debug.Log("[Echo] CLASH! Both knocked back.");

        Transform target = GetTarget();

        // VFX at midpoint
        if (target != null && clashVfxPrefab != null)
        {
            Vector3 midpoint = (transform.position + target.position) * 0.5f;
            Instantiate(clashVfxPrefab, midpoint, Quaternion.identity);
        }

        // Clash SFX
        if (clashSfx != null)
        {
            AudioService.PlayClip(clashSfx, transform.position, volume: 2f, pitch: 1f, minDistance: 8f);
        }

        // ── BOTH KNOCKED BACK 4 units ──
        if (target != null)
        {
            Vector3 clashDir = (target.position - transform.position).normalized;
            clashDir.y = 0;
            float knockDist = 1f;

            // Knock Echo backward
            transform.position -= clashDir * knockDist;

            // Knock player backward using CharacterController (safe)
            CharacterController playerCC = target.GetComponent<CharacterController>();
            if (playerCC != null)
            {
                playerCC.Move(clashDir * knockDist);
            }
            else
            {
                // Fallback: direct transform move
                target.position += clashDir * knockDist;
            }
        }

        // Reset so Echo can dash again immediately
        _lastDodgeTime = -99f;
        _isDashing = false;
        SetKatanaVisible(false);
    }

    private IEnumerator DodgeAwayOnHit()
    {
        Debug.Log("[Echo] DodgeAwayOnHit: interrupting + dodging 8u.");

        Transform target = GetTarget();
        if (target == null) yield break;

        // Interrupt current action
        if (_currentRoutine != null)
        {
            StopCoroutine(_currentRoutine);
            _currentRoutine = null;
        }
        // Mark as performing action so HandleBehavior doesn't start anything during dodge+stun
        _isPerformingAction = true;
        _isDashing = false;
        SetKatanaVisible(false);

        // Play dodge SFX
        if (dashSound != null)
        {
            AudioService.PlayClip(dashSound, transform.position, volume: 1.5f, pitch: 1f, minDistance: 10f);
        }

        // Calculate direction away from player
        Vector3 dodgeDir = (transform.position - target.position).normalized;
        dodgeDir.y = 0;
        if (dodgeDir.sqrMagnitude < 0.01f) dodgeDir = -target.forward;

        // Face the player
        FaceTarget(target);

        // Dodge back by hitDodgeDistance (8 units)
        float distanceTraveled = 0f;
        float speed = dodgeSpeed * 1.2f;
        while (distanceTraveled < hitDodgeDistance)
        {
            float step = speed * Time.deltaTime;
            float remaining = hitDodgeDistance - distanceTraveled;
            if (step > remaining) step = remaining;

            transform.position += dodgeDir * step;
            distanceTraveled += step;
            yield return null;
        }

        // ── Stun for hitStunDuration seconds ──
        _isStunned = true;
        SetWalkingAnimation(false);
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
        }

        yield return new WaitForSeconds(hitStunDuration);

        Debug.Log("[Echo] Stun ended — resuming action loop.");
        _isStunned = false;
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
        }

        // Resume pressure
        _currentRoutine = StartCoroutine(ActionLoopRoutine(GetTarget()));
    }

    /// <summary>
    /// Called by EchoArenaController on retry. Resets HP, state, and coroutines.
    /// </summary>
    public void ResetForRetry()
    {
        if (_currentRoutine != null)
        {
            StopCoroutine(_currentRoutine);
            _currentRoutine = null;
        }
        StopAllCoroutines();

        health = 3; // Boss always has 3 HP
        _isDead = false;
        _isPerformingAction = false;
        _isDashing = false;
        _isStunned = false;
        _isKnockedBack = false;
        _lastDodgeTime = -99f;
        _lastDashTime = -99f;
        _kunaiCharges = 1;
        _dealtDashDamage = false;

        SetKatanaVisible(false);
        SetWalkingAnimation(false);

        if (_agent != null)
        {
            _agent.enabled = true;
            _agent.isStopped = false;
        }
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector3.zero;
        }
        GetComponent<Collider>().enabled = true;

        OnHpChanged?.Invoke(health);
    }

    #endregion

    #region Death

    protected override void Die()
    {
        // Clean up Echo-specific state before base death sequence
        if (_currentRoutine != null)
        {
            StopCoroutine(_currentRoutine);
            _currentRoutine = null;
        }
        _isPerformingAction = false;
        _isDashing = false;

        // Let the base class handle death VFX, SFX, score, animation
        base.Die();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Rotates Echo to face the target (y-axis only).
    /// </summary>
    private void FaceTarget(Transform target)
    {
        if (target == null) return;
        Vector3 lookTarget = new Vector3(target.position.x, transform.position.y, target.position.z);
        transform.LookAt(lookTarget);
    }

    #endregion

    #region Gizmos

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (!showGizmos) return;

        // Dash range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, dashRange);

        // Dodge distance
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, dodgeDistance);

        // Detection range for projectiles
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }

    #endregion
}
