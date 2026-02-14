using UnityEngine;
using System.Collections;

public class EnemyDash : EnemyBase
{
    public float dashRange = 6f;
    public float dashSpeed = 40f;
    public float chargeTime = 0.6f;
    public float lockOnTime = 0.2f;
    public float dashDuration = 0.2f;
    public float cooldown = 2.5f;

    private float _lastDashTime = -99f;
    private CharacterController _playerCC;

    protected override void Awake()
    {
        base.Awake();
        if (_player != null) _player.TryGetComponent(out _playerCC);
    }

    protected override void HandleBehavior()
    {
        if (_lastDashTime + cooldown > Time.time) return;

        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist <= dashRange && CanAggro())
        {
            _agent.ResetPath();
            StartCoroutine(DashAttack());
        }
        else if (dist < detectRange && CanAggro())
        {
            _agent.SetDestination(_player.position);
        }
    }

    IEnumerator DashAttack()
    {
        _lastDashTime = Time.time;

        rend.material.color = new Color(1f, 0.5f, 0f);

        float trackTimer = 0f;
        while (trackTimer < chargeTime - lockOnTime)
        {
            Vector3 futurePos = _player.position + (_playerCC.velocity * 0.3f);
            transform.LookAt(new Vector3(futurePos.x, transform.position.y, futurePos.z));
            trackTimer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(lockOnTime);

        rend.material.color = Color.red; 
        Vector3 dashDir = transform.forward;
        
        float dashTimer = 0f;
        while (dashTimer < dashDuration)
        {
            _agent.Move(dashDir * dashSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _player.position) < 2.0f)
            {
                if (_player.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(damage);
                }
            }

            dashTimer += Time.deltaTime;
            yield return null;
        }

        rend.material.color = Color.gray;
        yield return new WaitForSeconds(1.0f);
        rend.material.color = _originalColor;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (!showGizmos) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, dashRange);
    }
}