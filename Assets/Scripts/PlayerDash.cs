using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using Unity.Cinemachine;

/// <summary>
/// Handles player dash attacks (left-click) and dodge rolls (Ctrl).
/// Manages enemy highlights, door breaking, camera zoom, and slow-mo recovery.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerDash : MonoBehaviour
{
    #region Inspector Fields - Dash
    public float maxDashDistance = 6f;
    public float dashSpeed = 40f;
    #endregion

    #region Inspector Fields - Combat

    public float damageDelay = 0.05f;
    #endregion

    #region Inspector Fields - Camera
    public float missPenalty = 1.0f;
    public float slowMotionFactor = 0.3f;

    public LayerMask hitLayer;
    public float hitRadius = 1.0f; 
    public int damageAmount = 1;

    public CinemachineCamera cam;
    public float normalDistance = 35f;
    public float zoomDistance = 15f;
    public float zoomSpeed = 5f;

    #endregion

    #region Inspector Fields - Audio
    public AudioClip dashSound;
    #endregion

    #region Inspector Fields - Cooldown
    public AudioClip slashSound;
    public AudioClip thudSound;

    public float dodgeCooldown = 1.5f;
    #endregion

    #region Inspector Fields - VFX & Animation
    private float _lastDodgeTime = -99f;

    public TrailRenderer trail;
    public Renderer playerRenderer; 
    public Color normalColor = Color.cyan;
    public Color recoveryColor = Color.gray;

    public GameObject katanaHip;
    public GameObject katanaHand;

    public LayerMask environmentMask;
    #endregion

    #region Private State

    private bool _isDashing = false;
    private bool _isPenaltyActive = false;
    
    private Vector3 _aimPoint;
    private Vector3 _dashDirection;
    private float _dashDistance;
    
    private CharacterController _cc;
    private PlayerMovement _movement;
    private Camera _mainCam;
    private CinemachinePositionComposer _posComposer; 
    private float _targetCamDistance;
    private Animator _animator;

    int _playerLayer;
    int _enemyLayer;
    
    private readonly HashSet<EnemyBase> _highlightedEnemies = new HashSet<EnemyBase>();
    private Door _cachedDoorInPath;
    private float doorDetectionRadius = 0.5f;

    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _movement = GetComponent<PlayerMovement>();
        _mainCam = Camera.main;
        _animator = GetComponentInChildren<Animator>();

        if (trail != null) trail.emitting = false;
        if (playerRenderer == null) playerRenderer = GetComponentInChildren<Renderer>();
        SetColor(normalColor);

        _playerLayer = LayerMask.NameToLayer("Player");
        _enemyLayer = LayerMask.NameToLayer("Enemy");

        if (cam != null)
        {
            _posComposer = cam.GetComponent<CinemachinePositionComposer>();
            
            if (_posComposer != null)
            {
                _targetCamDistance = normalDistance;
                _posComposer.CameraDistance = normalDistance;
            }
        
            #endregion
        
            #region Update Loop
        }
    }

    void Update()
    {
        CalculateDashData();
        HandleCameraZoom();

        if (_movement != null && (_movement.isMovementLocked || CaptionManager.IsFrozen || TutorialUIManager.IsOpen || PreGamePanel.IsPlaying || FinishPanelController.IsFinished || PausePanelController.IsPaused)) return;

        if (Mouse.current == null || _isDashing) return;

        if (!_isPenaltyActive) UpdateEnemyHighlights();
        else ClearHighlights();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!_isPenaltyActive) StartCoroutine(PerformDash(true));
        }
        else if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
        {
            if (Time.time >= _lastDodgeTime + dodgeCooldown)
            {
                _lastDodgeTime = Time.time;
                StartCoroutine(PerformDash(false));
            }
        }
    }

    void CalculateDashData()
    {
        Ray ray = _mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane ground = new Plane(Vector3.up, transform.position);
        
        if (ground.Raycast(ray, out float enter))
        {
            _aimPoint = ray.GetPoint(enter);
            Vector3 rawDir = _aimPoint - transform.position;
            rawDir.y = 0;
            
            _dashDirection = rawDir.sqrMagnitude > 0.01f ? rawDir.normalized : transform.forward;
            _dashDistance = Mathf.Clamp(rawDir.magnitude, 0f, maxDashDistance);
        }
        else
        {
            _dashDirection = transform.forward;
            _dashDistance = maxDashDistance;
        }

        _cachedDoorInPath = GetDoorInDashPath(_dashDirection, _dashDistance);

        if (_cachedDoorInPath != null && !_cachedDoorInPath.IsLocked())
        {
            DoorDashZone zone = _cachedDoorInPath.GetComponent<DoorDashZone>();
            if (zone != null)
            {
                Vector3 landingPos = zone.GetLandingPosition(transform.position);
                Vector3 distVector = landingPos - transform.position;
                distVector.y = 0;

                _dashDirection = distVector.normalized;
                _dashDistance = distVector.magnitude;
            }
        }
        else
        {
            if (Physics.SphereCast(transform.position, _cc.radius, _dashDirection, out RaycastHit envHit, _dashDistance, environmentMask, QueryTriggerInteraction.Ignore))
            {
                _dashDistance = envHit.distance;
            }
        }
    }

    void HandleCameraZoom()
    {
        if (_posComposer == null) return;
        if (CaptionCameraController.IsDriving) return;
        if (FinishPanelController.IsFinished) return; 
        float currentDist = _posComposer.CameraDistance;
        if (Mathf.Abs(currentDist - _targetCamDistance) < 0.01f) return;
        float newDist = Mathf.Lerp(currentDist, _targetCamDistance, Time.unscaledDeltaTime * zoomSpeed);
        _posComposer.CameraDistance = newDist;
    }

    IEnumerator PerformDash(bool isAttack)
    {
        _isDashing = true;

        if (dashSound != null)
        {
            AudioService.PlayClip2D(dashSound, 0.8f, 1f);
        }

        if (isAttack && slashSound != null)
        {
            AudioService.PlayClip2D(slashSound, 0.8f, 1.05f);
        }

        if (isAttack && katanaHip != null && katanaHand != null)
        {
            katanaHip.SetActive(false);
            katanaHand.SetActive(true);
        }

        if (_animator != null) _animator.SetTrigger("dashTrigger");
        
        bool wasAlreadyLocked = _movement.isMovementLocked;

        if (!wasAlreadyLocked)
        {
            _movement.isMovementLocked = true;
        }

        ClearHighlights();
        if (trail != null) trail.emitting = true;

        Physics.IgnoreLayerCollision(_playerLayer, _enemyLayer, true);
        Vector3 dashDir = _dashDirection;
        float currentDashDistance = _dashDistance;
        
        Door doorInPath = _cachedDoorInPath;
        _cachedDoorInPath = null;
        Vector3? doorLandingPos = null;

        if (doorInPath != null && isAttack && !doorInPath.IsLocked())
        {
            // Verify the path to the door is clear before breaking it.
            // Prevents phantom breaks when the door is detected through a wall or gap.
            Vector3 playerPos = transform.position;
            playerPos.y = 0;
            Vector3 doorPos = doorInPath.transform.position;
            doorPos.y = 0;
            Vector3 dirToDoor = (doorPos - playerPos).normalized;
            float distToDoor = Vector3.Distance(playerPos, doorPos);
            bool pathBlocked = false;
            if (Physics.Raycast(playerPos, dirToDoor, out RaycastHit pathHit, distToDoor, environmentMask))
            {
                // Ignore the door itself — only block if something else is in the way
                pathBlocked = !pathHit.collider.TryGetComponent(out Door hitDoor) || hitDoor != doorInPath;
            }

            if (!pathBlocked)
            {
                Debug.Log($"[PlayerDash] Breaking door: {doorInPath.DoorName}");
                DoorDashZone zone = doorInPath.GetComponent<DoorDashZone>();
                if (zone != null)
                {
                    doorInPath.Break();
                    CameraShakeService.Shake(0.5f);
                    zone.OnPlayerDashThrough();
                    
                    Debug.Log($"[PlayerDash] After transition: currentRoom={RoomManager.Instance?.CurrentRoom?.RoomName}, isDashing={_isDashing}");

                    Vector3 landingPos = zone.GetLandingPosition(transform.position);
                    doorLandingPos = landingPos;
                    Vector3 distVector = landingPos - transform.position;

                    distVector.y = 0;
                    
                    dashDir = distVector.normalized;
                    currentDashDistance = distVector.magnitude;
                }
            }
            else
            {
                Debug.Log($"[PlayerDash] Path to door blocked — skipping break: {doorInPath.DoorName}");
            }
        }

        transform.rotation = Quaternion.LookRotation(dashDir);

        bool hitSuccess = false;
        bool hitShield = false;
        
        if (isAttack)
        {
            List<RaycastHit> targets = GetSortedTargets(dashDir, currentDashDistance);

            targets = targets.Where(hit => {
                EnemyBase e = hit.collider.GetComponentInParent<EnemyBase>() 
                            ?? hit.collider.GetComponent<EnemyBase>();
                return e == null || e.MyRoom == RoomManager.Instance.CurrentRoom;
            }).ToList();
            
            if (targets.Count > 0)
            {
                RaycastHit firstHit = targets[0];
                
                // Safely grab the Phalanx whether we hit the child shield or the main body
                EnemyPhalanx phalanx = firstHit.collider.GetComponentInParent<EnemyPhalanx>();
                if (phalanx == null) phalanx = firstHit.collider.GetComponent<EnemyPhalanx>();
                
                if (phalanx != null && phalanx.HasShield())
                {
                    // Directional Math: Vector from Player to Phalanx
                    Vector3 dirToPhalanx = (phalanx.transform.position - transform.position).normalized;
                    
                    // Compare that vector against where the Phalanx is currently facing.
                    // A dot product less than ~0.2 means the player is attacking from the front/sides.
                    // Greater than 0.2 means the player is flanking from behind.
                    float dot = Vector3.Dot(dirToPhalanx, phalanx.transform.forward);
                    
                    if (dot < 0.2f)
                    {
                        hitShield = true;
                        Debug.Log($"[PlayerDash] Frontal attack! Breaking shield and knocking back...");
                        phalanx.BreakShield();
                        currentDashDistance = firstHit.distance; // Stop dash early
                        
                        StartCoroutine(RecoveryRoutine());
                    }
                    else
                    {
                        // Flanked! Bypass the shield entirely
                        hitSuccess = true;
                        _lastDodgeTime = -99f;
                        StartCoroutine(DealSequentialDamage(targets, dashDir));
                    }
                }
                else
                {
                    hitSuccess = true;
                    _lastDodgeTime = -99f;
                    StartCoroutine(DealSequentialDamage(targets, dashDir));
                }
            }
        }

        float traveled = 0f;
        while (traveled < currentDashDistance)
        {
            float step = dashSpeed * Time.deltaTime; 
            
            if (traveled + step > currentDashDistance) 
            {
                step = currentDashDistance - traveled;
            }

            _cc.Move(dashDir * step);
            traveled += step;

            if (isAttack)
            {
                CheckAndDestroyProjectiles(transform.position, dashDir);
            }

            yield return null;
        }

        if (_animator != null)
        {
            StartCoroutine(WaitForDashAnimationEnd());
        }

        if (hitShield)
        {
            Vector3 knockbackDir = -dashDir.normalized;
            float knockbackDuration = 0.15f;
            float knockbackSpeed = 40f;
            float elapsed = 0f;
            
            while (elapsed < knockbackDuration)
            {
                float t = elapsed / knockbackDuration;
                float currentSpeed = Mathf.Lerp(knockbackSpeed, 0f, t);
                _cc.Move(knockbackDir * currentSpeed * Time.deltaTime);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // Snap safety net
        if (doorLandingPos.HasValue && doorInPath != null && doorInPath.IsBroken)
        {
            Vector3 snapTarget = doorLandingPos.Value;
            snapTarget.y = transform.position.y;
            float snapDist = Vector3.Distance(transform.position, snapTarget);
            if (snapDist > 1.5f)
            {
                Debug.Log($"[PlayerDash] Stuck outside door — snapping (dist={snapDist})");
                
                // Temporarily disable the CharacterController to bypass physics collisions
                // and force a true teleport into the room.
                _cc.enabled = false;
                transform.position = snapTarget;
                _cc.enabled = true;
            }
        }

        Physics.IgnoreLayerCollision(_playerLayer, _enemyLayer, false);
        Debug.Log($"[PlayerDash] Dash ended. Layer collision restored. currentRoom={RoomManager.Instance?.CurrentRoom?.RoomName}");

        if (isAttack && !hitSuccess && !hitShield && doorInPath == null)
        {
            StartCoroutine(RecoveryRoutine());
        }

        if (trail != null) trail.emitting = false;
        
        if (!wasAlreadyLocked)
        {
            _movement.isMovementLocked = false;
        }
        
        _isDashing = false;
    }

    #endregion

    #region Damage & Recovery

    IEnumerator DealSequentialDamage(List<RaycastHit> targets, Vector3 attackDirection)
    {
        int validKills = 0;

        foreach (var hit in targets)
        {
            if (hit.collider.TryGetComponent(out IDamageable d)) validKills++;
        }

        if (validKills >= 2 && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddMultiKillBonus(validKills);
        }

        foreach (var hit in targets)
        {
            GameObject victim = hit.collider.gameObject;
            if (victim != null)
            {
                if (victim.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(damageAmount);
                    
                    if (thudSound != null)
                    {
                        AudioService.PlayClip2D(thudSound, 0.4f, 1f);
                    }
                }
            }
            yield return new WaitForSeconds(damageDelay);
        }
    }

    IEnumerator RecoveryRoutine()
    {
        _isPenaltyActive = true;
        SetColor(recoveryColor);

        _targetCamDistance = zoomDistance; 

        float originalScale = Time.timeScale;
        float originalFixed = Time.fixedDeltaTime;

        Time.timeScale = slowMotionFactor;
        Time.fixedDeltaTime = originalFixed * slowMotionFactor; 

        yield return new WaitForSecondsRealtime(missPenalty);

        Time.timeScale = originalScale;
        Time.fixedDeltaTime = originalFixed;

        _targetCamDistance = normalDistance;

        SetColor(normalColor);
        _isPenaltyActive = false;
    }

    #endregion

    #region Animation

    IEnumerator WaitForDashAnimationEnd()
    {
        yield return null;

        while (_animator.GetCurrentAnimatorStateInfo(0).IsName("Dash"))
        {
            yield return null;
        }

        // Animation finished, switch sword back
        if (katanaHip != null && katanaHand != null)
        {
            katanaHip.SetActive(true);
            katanaHand.SetActive(false);
        }
    }

    #endregion

    #region Target Detection

    #endregion

    #region Highlight System

    List<RaycastHit> GetSortedTargets(Vector3 dir, float dist)
    {
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, hitRadius, dir, dist, hitLayer);
        return hits.OrderBy(h => h.distance).ToList();
    }

    void UpdateEnemyHighlights()
    {
        var currentHighlights = new HashSet<EnemyBase>();
        var hits = Physics.SphereCastAll(transform.position, hitRadius, _dashDirection, _dashDistance, hitLayer);

        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent(out EnemyBase enemy))
            {
                currentHighlights.Add(enemy);
            }
        }

        var toRemove = _highlightedEnemies.Where(enemy => enemy == null || !currentHighlights.Contains(enemy)).ToList();
        foreach (var enemy in toRemove)
        {
            if (enemy != null)
            {
                enemy.SetHighlight(false);
            }

            _highlightedEnemies.Remove(enemy);
        }

        foreach (var enemy in currentHighlights)
        {
            if (_highlightedEnemies.Add(enemy))
            {
                enemy.SetHighlight(true);
            }
        }
    }

    void ClearHighlights()
    {
        foreach (var enemy in _highlightedEnemies)
        {
            if (enemy != null)
            {
                enemy.SetHighlight(false);
            }
        }

        _highlightedEnemies.Clear();
    }

    Door GetDoorInDashPath(Vector3 dir, float dist)
    {
        // Start cast from player position (no backward offset — that caused detecting doors behind you).
        // Use narrow doorDetectionRadius (0.5f) to avoid false positives from nearby doors.
        // Track closest door to avoid returning a door behind another door.
        float castDist = Mathf.Max(dist, 2f);
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, doorDetectionRadius, dir, castDist);
        Door closestDoor = null;
        float closestDist = float.MaxValue;
        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent(out Door door) && !door.IsBroken && hit.distance < closestDist)
            {
                closestDoor = door;
                closestDist = hit.distance;
            }
        }
        return closestDoor;
    }

    #endregion

    #region Projectile Swat

    void CheckAndDestroyProjectiles(Vector3 position, Vector3 dashDir)
    {
        Collider[] colliders = Physics.OverlapSphere(position, hitRadius);
        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent<ScatterProjectile>(out var projectile))
            {
                Destroy(projectile.gameObject);
            }
        }
    }

    #endregion

    #region Public API
    public bool IsPlayerDashing() => _isDashing;
    #endregion

    #region Utilities
    void SetColor(Color c) { if (playerRenderer != null) playerRenderer.material.color = c; }
    #endregion

    #region Gizmos
    void OnDrawGizmosSelected() { Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, hitRadius); }
    #endregion
}