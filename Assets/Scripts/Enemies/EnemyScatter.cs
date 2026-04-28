using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class EnemyScatter : EnemyBase
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    
    public float maxRange = 8f;
    public float attackCooldown = 1.2f;
    public float attackWindup = 0.25f;
    
    public int projectileCount = 5;
    public float spreadAngle = 45f;
    public float projectileSpeed = 45f;

    [Header("Audio")]
    public AudioClip aimSfx;
    public AudioClip fireSfx;
    public float aimSfxVolume = 1f;
    public float fireSfxVolume = 1f;
    public float aimSfxMinDistance = 8f;
    public float fireSfxMinDistance = 5f;

    private float _lastAttackTime = -999f;
    private bool _isAttacking = false;

    protected override void Awake()
    {
        base.Awake();
        
        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    protected override void HandleBehavior()
    {
        if (!CanAggro() || _isAttacking) return;

        Transform target = GetTarget();
        if (target == null) return;

        float distance = Vector3.Distance(transform.position, target.position);

        if (_animator != null)
        {
            _animator.SetBool("isWalking", distance > maxRange);
        }

        if (distance > maxRange)
        {
            _agent.isStopped = false; // <-- ADDED: Allow the agent to move again
            _agent.SetDestination(target.position);
        }
        else
        {
            _agent.isStopped = true;
            
            if (Time.time >= _lastAttackTime + attackCooldown)
            {
                StartCoroutine(AttackRoutine(target));
            }
        }
    }

    private IEnumerator AttackRoutine(Transform target)
    {
        _isAttacking = true;

        // 1. TRACKING PHASE: Smoothly turn to face the player BEFORE raising the gun
        float maxTurnTime = 0.5f; 
        float elapsedTurn = 0f;
        
        while (elapsedTurn < maxTurnTime && target != null)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            directionToTarget.y = 0; // Keep rotation flat on the ground
            
            if (directionToTarget.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 12f);
                
                // If we are looking almost directly at the player (within 5 degrees), stop turning early
                if (Quaternion.Angle(transform.rotation, targetRot) < 1f)
                {
                    break;
                }
            }
            elapsedTurn += Time.deltaTime;
            yield return null;
        }

        if (ShouldAbortAttack(target))
        {
            _agent.isStopped = false;
            _isAttacking = false;
            yield break;
        }

        // 2. LOCK PHASE: Raise the gun and lock the trajectory
        if (_animator != null) _animator.SetBool("isAiming", true);

        if (aimSfx != null)
        {
            AudioService.PlayClip(aimSfx, transform.position, aimSfxVolume, 1f, -1f, aimSfxMinDistance, -1f);
        }

        // Lock onto where the player is RIGHT NOW (with the 2.0f height adjustment)
        Vector3 lockedTargetChest = target.position + Vector3.up * 2.0f;
        Vector3 lockedAimDirection = (lockedTargetChest - firePoint.position).normalized;

        // 3. WINDUP PHASE: The enemy stands perfectly still, giving the player time to run away
        yield return new WaitForSeconds(attackWindup);

        if (ShouldAbortAttack(target))
        {
            _agent.isStopped = false;
            _isAttacking = false;
            if (_animator != null) _animator.SetBool("isAiming", false);
            yield break;
        }

        // 4. FIRE PHASE: Shoot at the locked direction
        if (!_isDead)
        {
            if (_animator != null) _animator.SetTrigger("fireTrigger");

            if (fireSfx != null)
            {
                AudioService.PlayClip(fireSfx, transform.position, fireSfxVolume, 1f, -1f, fireSfxMinDistance, -1f);
            }

            FireSpreadProjectiles(lockedAimDirection);
        }
        
        // 5. RECOVERY PHASE: Wait for recoil to finish, then drop the gun
        yield return new WaitForSeconds(0.5f);

        if (_animator != null) _animator.SetBool("isAiming", false);

        _lastAttackTime = Time.time;
        _isAttacking = false;
    }

    private void FireSpreadProjectiles(Vector3 baseDirection)
    {
        if (projectilePrefab == null) return;
        
        float angleStep = spreadAngle / (projectileCount - 1);
        float startAngle = -spreadAngle / 2f;

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * baseDirection;

            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            
            if (projectile.TryGetComponent(out ScatterProjectile scatterProj))
            {
                scatterProj.Initialize(direction, projectileSpeed, damage);
            }
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (!showGizmos) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxRange);
    }
}
