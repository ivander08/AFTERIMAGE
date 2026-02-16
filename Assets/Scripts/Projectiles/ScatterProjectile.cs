using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class ScatterProjectile : BaseProjectile
{
    private int _damage = 1;

    protected override void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        ConfigureRigidbody();
    }

    protected override void Start()
    {
    }

    public void Initialize(Vector3 direction, float projectileSpeed, int damage)
    {
        transform.forward = direction;
        speed = projectileSpeed;
        _damage = damage;
        lifetime = 5f;
        Destroy(gameObject, lifetime);
    }

    protected override void Move()
    {
        float moveDistance = speed * Time.deltaTime;
        transform.Translate(Vector3.forward * moveDistance);
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<ScatterProjectile>() != null)
        {
            return;
        }
        
        OnHit(other);
    }

    public override void OnHit(Collider other)
    {
        if (other.GetComponent<ScatterProjectile>() != null)
        {
            return;
        }
        
        if (other.TryGetComponent(out IDamageable damageable))
        {
            damageable.TakeDamage(_damage);
        }
        
        Destroy(gameObject);
    }
}
