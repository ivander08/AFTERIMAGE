using UnityEngine;

/// <summary>
/// Base class for all projectile types. Handles movement, collision, lifetime,
/// and shield-breaking logic.
/// </summary>

[RequireComponent(typeof(Rigidbody))]
public abstract class BaseProjectile : MonoBehaviour
{
    public float speed = 25f;
    public float lifetime = 3f;

    protected Rigidbody _rb;
    protected Vector3 _lastHitNormal = Vector3.forward;

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
                _lastHitNormal = hit.normal;
                OnHit(hit.collider);
                return; // Stop moving
            }
        }

        transform.Translate(Vector3.forward * moveDistance);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) return;
        // Triggers don't provide a contact normal, approximate from travel direction
        _lastHitNormal = -transform.forward;
        OnHit(other);
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player")) return;
        if (collision.contactCount > 0)
            _lastHitNormal = collision.GetContact(0).normal;
        else
            _lastHitNormal = -transform.forward;

        OnHit(collision.collider);
    }

    public abstract void OnHit(Collider other);
}