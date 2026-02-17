using UnityEngine;
using System.Collections;

public class EnemyPrism : EnemyBase
{
    public GameObject shardPrefab;
    public float attackRange = 1.5f;
    public float attackWindup = 0.3f;
    public float attackCooldown = 1.5f;

    private float _lastAttackTime = -99f;
    private bool _isAttacking = false;

    protected override void HandleBehavior()
    {
        if (_isAttacking) return;
        if (!CanAggro()) return;

        Transform target = GetTarget();
        float dist = Vector3.Distance(transform.position, target.position);

        if (dist <= detectRange)
        {
            _agent.SetDestination(target.position);

            if (dist <= attackRange && Time.time >= _lastAttackTime + attackCooldown)
            {
                StartCoroutine(AttackRoutine(target));
            }
        }
    }

    IEnumerator AttackRoutine(Transform target)
    {
        _isAttacking = true;
        _agent.isStopped = true;

        transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
        rend.material.color = Color.red;

        yield return new WaitForSeconds(attackWindup);

        if (target != null && Vector3.Distance(transform.position, target.position) <= attackRange + 0.5f)
        {
            if (target.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damage);
            }
        }

        rend.material.color = Color.white;
        yield return new WaitForSeconds(0.5f);

        _lastAttackTime = Time.time;
        _agent.isStopped = false;
        _isAttacking = false;
    }

    protected override void Die()
    {
        if (shardPrefab != null && _myRoom != null)
        {
            Vector3 spawnPos = transform.position;
            Vector3 perpendicular = Vector3.Cross(Vector3.up, transform.forward).normalized;

            Vector3 shard1Pos = spawnPos + perpendicular * 0.5f;
            Vector3 shard2Pos = spawnPos - perpendicular * 0.5f;

            GameObject shard1 = Instantiate(shardPrefab, shard1Pos, transform.rotation);
            GameObject shard2 = Instantiate(shardPrefab, shard2Pos, transform.rotation);

            shard1.transform.SetParent(transform.parent);
            shard2.transform.SetParent(transform.parent);

            if (shard1.TryGetComponent<EnemyBase>(out var enemy1))
            {
                _myRoom.RegisterEnemy(enemy1);
                enemy1.Knockback((shard1Pos - spawnPos).normalized, 25f, 0.3f);
                enemy1.Stun(0.5f);
            }

            if (shard2.TryGetComponent<EnemyBase>(out var enemy2))
            {
                _myRoom.RegisterEnemy(enemy2);
                enemy2.Knockback((shard2Pos - spawnPos).normalized, 25f, 0.3f);
                enemy2.Stun(0.5f);
            }
        }

        base.Die();
    }
}
