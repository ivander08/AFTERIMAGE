using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
public class IaijutsuBreakAbility : MonoBehaviour
{
    public float dashSpeed = 50f;
    public float returnDashSpeed = 100f;
    public float dashInterval = 0.05f;
    public float dramaticPause = 0.4f;
    public TrailRenderer trailRenderer;
    public bool highlightEnemies = true;

    private bool _usedThisLevel = false;
    private PlayerMovement _playerMovement;
    private CharacterController _characterController;
    private bool _isExecuting = false;

    private void Awake()
    {
        _usedThisLevel = false;
        _playerMovement = GetComponent<PlayerMovement>();
        _characterController = GetComponent<CharacterController>();

        if (trailRenderer == null)
        {
            trailRenderer = GetComponentInChildren<TrailRenderer>();
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryActivateAbility();
        }
    }

    private void TryActivateAbility()
    {
        if (!CanUse()) return;
        StartCoroutine(ExecuteIaijutsuBreak());
    }

    private bool CanUse()
    {
        if (_usedThisLevel) return false;
        if (_isExecuting) return false;
        if (RoomManager.Instance == null || RoomManager.Instance.CurrentRoom == null) return false;

        List<EnemyBase> enemies = RoomManager.Instance.CurrentRoom.GetEnemies();
        if (enemies == null || enemies.Count == 0) return false;

        foreach (var enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead) return true;
        }

        return false;
    }

    private IEnumerator ExecuteIaijutsuBreak()
    {
        _isExecuting = true;
        _usedThisLevel = true;

        List<EnemyBase> enemies = RoomManager.Instance.CurrentRoom.GetEnemies();
        List<EnemyBase> targetEnemies = new List<EnemyBase>();
        List<Vector3> targetPositions = new List<Vector3>();

        foreach (var enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                targetEnemies.Add(enemy);
                targetPositions.Add(enemy.transform.position);
                enemy.SetFrozen(true);
                if (highlightEnemies) enemy.SetHighlight(true);
            }
        }

        if (targetEnemies.Count == 0)
        {
            _isExecuting = false;
            yield break;
        }

        if (_playerMovement != null) _playerMovement.isMovementLocked = true;
        
        Vector3 originalPosition = transform.position;
        if (trailRenderer != null) trailRenderer.emitting = true;

        for (int i = 0; i < targetPositions.Count; i++)
        {
            yield return StartCoroutine(DashToPosition(targetPositions[i], dashSpeed));
            yield return new WaitForSeconds(dashInterval);
        }

        yield return StartCoroutine(DashToPosition(originalPosition, returnDashSpeed));

        if (trailRenderer != null) trailRenderer.emitting = false;
        
        yield return new WaitForSeconds(dramaticPause);

        foreach (var enemy in targetEnemies)
        {
            if (enemy != null && !enemy.IsDead) enemy.TakeDamage(9999);
        }

        if (highlightEnemies)
        {
            foreach (var enemy in targetEnemies)
            {
                if (enemy != null) enemy.SetHighlight(false);
            }
        }

        if (_playerMovement != null) _playerMovement.isMovementLocked = false;
        _isExecuting = false;
    }

    private IEnumerator DashToPosition(Vector3 targetPosition, float speed)
    {
        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float duration = distance / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, t);
            
            if (_characterController != null)
            {
                _characterController.Move(newPosition - transform.position);
            }
            else
            {
                transform.position = newPosition;
            }

            yield return null;
        }

        if (_characterController != null)
        {
            _characterController.Move(targetPosition - transform.position);
        }
        else
        {
            transform.position = targetPosition;
        }
    }

    public bool IsAvailable() => !_usedThisLevel && !_isExecuting;
    public bool IsExecuting() => _isExecuting;
    public void ResetAbility() => _usedThisLevel = false;
}
