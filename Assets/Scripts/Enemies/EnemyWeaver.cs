using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Support enemy that tethers to a nearby ally, making it invulnerable
/// until the Weaver is defeated.
/// </summary>

public class EnemyWeaver : EnemyBase
{
    public float tetherRange = 20f;
    public LayerMask enemyLayer;
    public Color tetherColor = Color.cyan;
    public Material tetherLineMaterial;
    public Material tetherOutlineMaterial;

    private LineRenderer _tetherLine;
    private EnemyBase _tetheredEnemy;
    private List<SkinnedMeshRenderer> _tetheredRenderers = new();

    protected override void Awake()
    {
        base.Awake();
        
        _tetherLine = gameObject.AddComponent<LineRenderer>();
        _tetherLine.startWidth = 0.1f;
        _tetherLine.endWidth = 0.1f;
        _tetherLine.positionCount = 2;

        if (tetherLineMaterial != null)
        {
            _tetherLine.material = new Material(tetherLineMaterial);
        }
        else
        {
            _tetherLine.material = new Material(Shader.Find("Sprites/Default"));
        }

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
            .Where(enemy => enemy != null && enemy != this && !enemy.IsDead && !(enemy is EnemyShard) && enemy.MyRoom == _myRoom)
            .ToArray();

        if (validEnemies.Length > 0)
        {
            _tetheredEnemy = validEnemies[Random.Range(0, validEnemies.Length)];
            _tetheredEnemy.isInvulnerable = true;
            _tetherLine.enabled = true;
            ApplyTetherVisual(_tetheredEnemy);
        }
    }

    void UpdateTetherVisual()
    {
        if (_tetheredEnemy == null) return;

        _tetherLine.SetPosition(0, transform.position + Vector3.up * 0.5f);
        _tetherLine.SetPosition(1, _tetheredEnemy.transform.position + Vector3.up * 0.5f);
    }

    void ApplyTetherVisual(EnemyBase target)
    {
        _tetheredRenderers.Clear();
        SkinnedMeshRenderer[] renderers = target.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var renderer in renderers)
        {
            int originalCount = renderer.materials.Length;
            var materials = new Material[originalCount + 1];
            System.Array.Copy(renderer.materials, materials, originalCount);
            materials[originalCount] = tetherOutlineMaterial;
            renderer.materials = materials;
            _tetheredRenderers.Add(renderer);
        }
    }

    void RemoveTetherVisual()
    {
        foreach (var renderer in _tetheredRenderers)
        {
            if (renderer == null) continue;

            int currentCount = renderer.materials.Length;
            if (currentCount > 0)
            {
                var materials = new Material[currentCount - 1];
                System.Array.Copy(renderer.materials, materials, currentCount - 1);
                renderer.materials = materials;
            }
        }
        _tetheredRenderers.Clear();
    }

    void ClearTether()
    {
        if (_tetheredEnemy != null)
        {
            _tetheredEnemy.isInvulnerable = false;
            _tetheredEnemy = null;
        }

        RemoveTetherVisual();
        _tetherLine.enabled = false;
    }

    protected override void Die()
    {
        ClearTether();
        base.Die();
    }
}
