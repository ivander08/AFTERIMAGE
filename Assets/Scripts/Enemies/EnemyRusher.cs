using UnityEngine;
using System.Collections;

/// <summary>
/// Melee enemy that charges at the player with a dashing attack.
/// Can be countered by the player dashing into it head-on.
/// </summary>

public class EnemyRusher : EnemyBase
{
    public float dashRange = 4f;
    public float dashSpeed = 40f;
    public float chargeTime = 0.2f;
    public float dashDuration = 0.15f;
    public float cooldown = 0.8f;

    public AudioClip chargeSound;
    public AudioClip dashSound;
    public float chargeSoundMinDistance = 8f;
    public float dashSoundMinDistance = 3f;

    private float _lastDashTime = -99f;
    private bool _isDashing = false;

    protected override void Awake()
    {
        base.Awake();
        SetKatanaVisible(false);
    }

    protected override void HandleBehavior()
    {
        if (!CanAggro()) return;

        Transform target = GetTarget();
        float dist = Vector3.Distance(transform.position, target.position);

        bool canDash = _lastDashTime + cooldown <= Time.time;

        // Update walking animation
        if (_animator != null)
        {
            _animator.SetBool("isWalking", dist > dashRange && !_isDashing);
        }

        if (canDash && dist <= dashRange)
        {
            _agent.ResetPath();
            StartCoroutine(DashAttack(target));
        }
        else if (dist < detectRange)
        {
            _agent.SetDestination(target.position);
        }
    }

    IEnumerator DashAttack(Transform target)
    {
        _lastDashTime = Time.time;
        _isDashing = true;
        
        _agent.isStopped = true;
        
        Vector3 targetPosition = target.position;
        Vector3 dashDirection = (new Vector3(targetPosition.x, transform.position.y, targetPosition.z) - transform.position).normalized;
        transform.forward = dashDirection;
        
        // Draw sword and charge up
        SetKatanaVisible(true);
        if (_animator != null)
        {
            _animator.SetBool("isWalking", false);
        }
        
        // Play charge sound
        if (chargeSound != null)
        {
            AudioService.PlayClip(chargeSound, transform.position, volume: 1.5f, minDistance: chargeSoundMinDistance);
        }
        
        yield return new WaitForSeconds(chargeTime);
        
        // Trigger dash animation right before movement
        if (_animator != null)
        {
            _animator.SetTrigger("dashTrigger");
        }
        
        // Play dash movement sound
        if (dashSound != null)
        {
            AudioService.PlayClip(dashSound, transform.position, volume: 2f, minDistance: dashSoundMinDistance);
        }
        
        float dashTimer = 0f;
        while (dashTimer < dashDuration)
        {
            transform.position += dashDirection * dashSpeed * Time.deltaTime;

            Collider[] hits = Physics.OverlapSphere(transform.position, 1.0f);
            foreach(var hit in hits)
            {
                if(hit.CompareTag("Player"))
                {
                    PlayerDash playerDash = hit.GetComponent<PlayerDash>();
                    if (playerDash != null && playerDash.IsPlayerDashing())
                    {
                        // Player is dashing head-on, Rusher takes damage instead
                        if (TryGetComponent(out IDamageable rushDamageable))
                        {
                            rushDamageable.TakeDamage(damage);
                        }
                    }
                    else if (hit.TryGetComponent(out IDamageable damageable))
                    {
                        damageable.TakeDamage(damage);
                    }
                }
            }

            dashTimer += Time.deltaTime;
            yield return null;
        }

        SetKatanaVisible(false);
        _isDashing = false;
        _agent.isStopped = false;
    }
}
