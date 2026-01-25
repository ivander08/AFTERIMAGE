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
            
            if (_rb.collisionDetectionMode == CollisionDetectionMode.Discrete)
                _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
    }

    protected virtual void Move()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
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
}