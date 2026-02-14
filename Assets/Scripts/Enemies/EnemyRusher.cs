using UnityEngine;
using System.Collections;

public class EnemyRusher : EnemyBase
{
    public float dashRange = 6f;
    public float dashSpeed = 40f;
    public float chargeTime = 0.6f;
    public float dashDuration = 0.2f;
    public float cooldown = 2.5f;

    private float _lastDashTime = -99f;

    protected override void HandleBehavior()
    {
        if (_lastDashTime + cooldown > Time.time) return;
        if (!CanAggro()) return;

        Transform target = GetTarget();
        float dist = Vector3.Distance(transform.position, target.position);

        if (dist <= dashRange)
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
        rend.material.color = new Color(1f, 0.5f, 0f); // Orange warning

        // Track target during charge
        float trackTimer = 0f;
        while (trackTimer < chargeTime)
        {
            if(target != null)
                transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
            
            trackTimer += Time.deltaTime;
            yield return null;
        }

        rend.material.color = Color.red; 
        Vector3 dashDir = transform.forward;
        
        // Execute Dash
        float dashTimer = 0f;
        while (dashTimer < dashDuration)
        {
            // Manual movement since Agent is erratic at high speeds
            transform.position += dashDir * dashSpeed * Time.deltaTime;

            // Simple hitbox check
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

        rend.material.color = Color.gray;
        yield return new WaitForSeconds(1.0f); // Recovery
        rend.material.color = _originalColor;
    }
}
