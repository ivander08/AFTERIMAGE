using UnityEngine;
using System.Collections;

public class EnemyRusher : EnemyBase
{
    public float dashRange = 4f;
    public float dashSpeed = 40f;
    public float chargeTime = 0.2f;
    public float dashDuration = 0.15f;
    public float cooldown = 0.8f;

    private float _lastDashTime = -99f;

    protected override void HandleBehavior()
    {
        if (!CanAggro()) return;

        Transform target = GetTarget();
        float dist = Vector3.Distance(transform.position, target.position);

        bool canDash = _lastDashTime + cooldown <= Time.time;

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
        
        _agent.isStopped = true;
        
        Vector3 targetPosition = target.position;
        Vector3 dashDirection = (new Vector3(targetPosition.x, transform.position.y, targetPosition.z) - transform.position).normalized;
        transform.forward = dashDirection;
        
        rend.material.color = new Color(1f, 0.5f, 0f);
        yield return new WaitForSeconds(chargeTime);
        
        rend.material.color = Color.red;
        
        float dashTimer = 0f;
        while (dashTimer < dashDuration)
        {
            transform.position += dashDirection * dashSpeed * Time.deltaTime;

            Collider[] hits = Physics.OverlapSphere(transform.position, 1.0f);
            foreach(var hit in hits)
            {
                if(hit.CompareTag("Player") && hit.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(damage);
                }
            }

            dashTimer += Time.deltaTime;
            yield return null;
        }

        _agent.isStopped = false;
        rend.material.color = _originalColor;
    }
}
