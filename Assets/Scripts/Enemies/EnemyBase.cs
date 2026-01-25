using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    public int health = 1;
    public float detectRange = 15f;
    public int damage = 1;
    public Renderer rend;
    [SerializeField] public bool showGizmos = false;
    protected Color _originalColor;
    protected NavMeshAgent _agent;
    protected Transform _player;
    protected bool _isDead = false;

    protected virtual void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;

        if (rend == null) rend = GetComponent<Renderer>();
        if (rend != null) _originalColor = rend.material.color;
        
        GetComponent<Rigidbody>().isKinematic = true;
    }

    protected virtual void Update()
    {
        if (_isDead || _player == null) return;
        HandleBehavior();
    }

    protected abstract void HandleBehavior();

    public void TakeDamage(int damage)
    {
        if (_isDead) return;
        health -= damage;
        StartCoroutine(FlashWhite());
        if (health <= 0) Die();
    }

    protected virtual void Die()
    {
        _isDead = true;
        _agent.isStopped = true;
        StopAllCoroutines();
        if (rend) rend.material.color = Color.black;
        GetComponent<Collider>().enabled = false;
        Destroy(gameObject, 1f);
    }

    public void SetHighlight(bool active)
    {
        if (_isDead || rend == null) return;
        if (rend.material.color == _originalColor || rend.material.color == Color.yellow)
        {
            rend.material.color = active ? Color.yellow : _originalColor;
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }

    IEnumerator FlashWhite()
    {
        if (rend != null)
        {
            Color prev = rend.material.color;
            rend.material.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            rend.material.color = prev;
        }
    }
}