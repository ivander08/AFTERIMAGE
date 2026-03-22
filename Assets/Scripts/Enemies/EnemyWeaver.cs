using UnityEngine;
using System.Collections;
using System.Linq;

public class EnemyWeaver : EnemyBase
{
    public float tetherRange = 20f;
    public LayerMask enemyLayer;
    public Color tetherColor = Color.cyan;

    private LineRenderer _tetherLine;
    private EnemyBase _tetheredEnemy;

    protected override void Awake()
    {
        base.Awake();
        
        _tetherLine = gameObject.AddComponent<LineRenderer>();
        _tetherLine.startWidth = 0.1f;
        _tetherLine.endWidth = 0.1f;
        _tetherLine.positionCount = 2;
        _tetherLine.material = new Material(Shader.Find("Sprites/Default"));
        _tetherLine.startColor = tetherColor;
        _tetherLine.endColor = tetherColor;
        _tetherLine.enabled = false;
    }

    void Start()
    {
        FindAndTetherEnemy();
        if (_tetheredEnemy != null)
        {
            StartCoroutine(TetherRoutine());
        }
    }

    protected override void HandleBehavior()
    {
        if (!CanAggro()) return;
        
        Transform target = GetTarget();
        if (target != null)
        {
            Vector3 direction = target.position - transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    IEnumerator TetherRoutine()
    {
        while (!_isDead && _tetheredEnemy != null)
        {
            UpdateTetherVisual();
            yield return null;
        }
    }

    void FindAndTetherEnemy()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, tetherRange, enemyLayer);
        
        EnemyBase[] validEnemies = colliders
            .Select(col => col.GetComponent<EnemyBase>())
            .Where(enemy => enemy != null && enemy != this && !enemy.IsDead && !(enemy is EnemyShard))
            .ToArray();

        if (validEnemies.Length > 0)
        {
            _tetheredEnemy = validEnemies[Random.Range(0, validEnemies.Length)];
            _tetheredEnemy.isInvulnerable = true;
            _tetherLine.enabled = true;
        }
    }

    void UpdateTetherVisual()
    {
        if (_tetheredEnemy == null) return;

        _tetherLine.SetPosition(0, transform.position + Vector3.up * 0.5f);
        _tetherLine.SetPosition(1, _tetheredEnemy.transform.position + Vector3.up * 0.5f);
    }

    void ClearTether()
    {
        if (_tetheredEnemy != null)
        {
            _tetheredEnemy.isInvulnerable = false;
            _tetheredEnemy = null;
        }

        _tetherLine.enabled = false;
    }

    protected override void Die()
    {
        ClearTether();
        base.Die();
    }
}
