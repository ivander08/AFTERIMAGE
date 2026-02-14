using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class BaseProjectile : MonoBehaviour
{
    public float speed = 25f;
    public float lifetime = 3f;

    protected Rigidbody _rb;

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        ConfigureRigidbody();
    }

    protected virtual void Start()
    {
        Destroy(gameObject, lifetime);
    }

    protected virtual void Update()
    {
        Move();
    }

    protected virtual void ConfigureRigidbody()
    {
        if (_rb != null)
        {
            _rb.useGravity = false;
            _rb.isKinematic = true; 
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
    }

    protected virtual void Move()
    {
        float moveDistance = speed * Time.deltaTime;

        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, moveDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.CompareTag("Player"))
            {
                transform.position = hit.point; 
                OnHit(hit.collider);
                return; // Stop moving
            }
        }

        transform.Translate(Vector3.forward * moveDistance);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) return;
        OnHit(other);
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player")) return;
        OnHit(collision.collider);
    }

    public abstract void OnHit(Collider other);

    protected virtual void BreakPhalanxShield(Collider other)
    {
        if (other.TryGetComponent(out EnemyPhalanx phalanx))
        {
            Debug.Log($"[BaseProjectile] Shield broken (direct hit)!");
            phalanx.BreakShield();
        }
        else if (other.GetComponentInParent<EnemyPhalanx>() is EnemyPhalanx parentPhalanx)
        {
            Debug.Log($"[BaseProjectile] Shield broken (parent search)!");
            parentPhalanx.BreakShield();
        }
    }
}