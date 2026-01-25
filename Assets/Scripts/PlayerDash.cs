using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerDash : MonoBehaviour
{
    public float maxDashDistance = 6f;
    public float dashSpeed = 40f; 

    public float damageDelay = 0.05f;
    public float missPenalty = 1.0f;
    public float slowMotionFactor = 0.3f;

    public LayerMask hitLayer;
    public float hitRadius = 1.0f; 
    public int damageAmount = 1;

    public CinemachineCamera cam;
    public float normalDistance = 35f;
    public float zoomDistance = 15f;
    public float zoomSpeed = 5f;

    public float dodgeCooldown = 1.5f; 
    private float _lastDodgeTime = -99f;

    public TrailRenderer trail;
    public Renderer playerRenderer; 
    public Color normalColor = Color.cyan;
    public Color recoveryColor = Color.gray;

    private bool _isDashing = false;
    private bool _isPenaltyActive = false;
    
    private CharacterController _cc;
    private PlayerMovement _movement;
    private Camera _mainCam;
    private CinemachinePositionComposer _posComposer; 
    private float _targetCamDistance;

    int _playerLayer;
    int _enemyLayer;
    
    private List<EnemyBase> _highlightedEnemies = new List<EnemyBase>();

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _movement = GetComponent<PlayerMovement>();
        _mainCam = Camera.main;

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
        }
    }

    void Update()
    {
        HandleCameraZoom();

        if (Mouse.current == null || _isDashing) return;

        if (!_isPenaltyActive) UpdateEnemyHighlights();
        else ClearHighlights();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!_isPenaltyActive) StartCoroutine(PerformDash(true));
        }
        else if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (Time.time >= _lastDodgeTime + dodgeCooldown)
            {
                _lastDodgeTime = Time.time;
                StartCoroutine(PerformDash(false));
            }
        }
    }

    void HandleCameraZoom()
    {
        if (_posComposer == null) return;
        float currentDist = _posComposer.CameraDistance;
        if (Mathf.Abs(currentDist - _targetCamDistance) < 0.01f) return;
        float newDist = Mathf.Lerp(currentDist, _targetCamDistance, Time.unscaledDeltaTime * zoomSpeed);
        _posComposer.CameraDistance = newDist;
    }

    IEnumerator PerformDash(bool isAttack)
    {
        _isDashing = true;
        _movement.isMovementLocked = true;
        ClearHighlights();
        if (trail != null) trail.emitting = true;

        Physics.IgnoreLayerCollision(_playerLayer, _enemyLayer, true); 

        Vector3 dashDir = GetDashDirection();
        float distance = GetClampedDistance();
        transform.rotation = Quaternion.LookRotation(dashDir);

        bool hitSuccess = false;
        if (isAttack)
        {
            List<RaycastHit> targets = GetSortedTargets(dashDir, distance);
            if (targets.Count > 0)
            {
                hitSuccess = true;
                _lastDodgeTime = -99f;
                StartCoroutine(DealSequentialDamage(targets));
            }
        }

        float traveled = 0f;
        while (traveled < distance)
        {
            float step = dashSpeed * Time.deltaTime; 
            _cc.Move(dashDir * step);
            traveled += step;
            yield return null;
        }

        Physics.IgnoreLayerCollision(_playerLayer, _enemyLayer, false);

        if (trail != null) trail.emitting = false;
        _movement.isMovementLocked = false;
        _isDashing = false;

        if (isAttack && !hitSuccess)
        {
            StartCoroutine(RecoveryRoutine());
        }
    }

    IEnumerator DealSequentialDamage(List<RaycastHit> targets)
    {
        foreach (var hit in targets)
        {
            GameObject victim = hit.collider.gameObject;
            if (victim != null)
            {
                IDamageable damageable = victim.GetComponent<IDamageable>();
                if (damageable != null) damageable.TakeDamage(damageAmount);
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

    List<RaycastHit> GetSortedTargets(Vector3 dir, float dist)
    {
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, hitRadius, dir, dist, hitLayer);
        return hits.OrderBy(h => h.distance).ToList();
    }

    void UpdateEnemyHighlights()
    {
        ClearHighlights();
        Vector3 dir = GetDashDirection();
        float dist = GetClampedDistance();
        
        var hits = Physics.SphereCastAll(transform.position, hitRadius, dir, dist, hitLayer);
        foreach (var hit in hits)
        {
            EnemyBase enemy = hit.collider.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.SetHighlight(true);
                _highlightedEnemies.Add(enemy);
            }
        }
    }

    void ClearHighlights()
    {
        foreach (var enemy in _highlightedEnemies) if (enemy != null) enemy.SetHighlight(false);
        _highlightedEnemies.Clear();
    }

    float GetClampedDistance()
    {
        Ray ray = _mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane ground = new Plane(Vector3.up, transform.position);
        if (ground.Raycast(ray, out float enter))
             return Mathf.Clamp(Vector3.Distance(transform.position, ray.GetPoint(enter)), 0f, maxDashDistance);
        return maxDashDistance;
    }

    Vector3 GetDashDirection()
    {
        Ray ray = _mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane ground = new Plane(Vector3.up, transform.position);
        if (ground.Raycast(ray, out float enter))
        {
            Vector3 dir = (ray.GetPoint(enter) - transform.position).normalized;
            dir.y = 0;
            return (dir == Vector3.zero) ? transform.forward : dir;
        }
        return transform.forward;
    }

    void SetColor(Color c) { if (playerRenderer != null) playerRenderer.material.color = c; }
    
    void OnDrawGizmosSelected() { Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, hitRadius); }
}