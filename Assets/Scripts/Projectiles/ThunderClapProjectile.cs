using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class ThunderClapProjectile : BaseProjectile
{
    public float explosionRadius = 4f;
    public float maxRange = 8f;
    public float stunDuration = 2.5f;
    public LayerMask enemyLayer;

    [Header("VFX")]
    public GameObject explosionVfx;
    [Header("Audio")]
    public AudioClip hitSfx;

    private Vector3 _startPosition;
    private bool _hasExploded = false;
    private bool _didPlayHitSfx;

    private void PlayHitSfxOnce()
    {
        if (_didPlayHitSfx || hitSfx == null) return;
        _didPlayHitSfx = true;
        AudioService.PlayClip(hitSfx, transform.position, 1f, 1f, minDistance: 16f);
    }

    protected override void Awake()
    {
        base.Awake();
        _startPosition = transform.position;
        
        GetComponent<Collider>().isTrigger = true; 
    }

    protected override void Update()
    {
        if (_hasExploded) return;

        base.Update();

        float distanceTraveled = Vector3.Distance(_startPosition, transform.position);
        if (distanceTraveled >= maxRange)
        {
            Explode(null);
        }
    }

    public override void OnHit(Collider other)
    {
        if (_hasExploded) return;
        
        EnemyPhalanx phalanxParent = other.GetComponentInParent<EnemyPhalanx>();
        if (phalanxParent != null && other.gameObject != phalanxParent.gameObject)
        {
            Debug.Log($"[ThunderClap] Hit shield!");
            phalanxParent.BreakShield();
        }
        
        Explode(other.gameObject);
    }

    void Explode(GameObject directHitObj)
    {
        _hasExploded = true;
        
        if (explosionVfx != null)
        {
            Instantiate(explosionVfx, transform.position, Quaternion.LookRotation(_lastHitNormal));
        }
        
        if (directHitObj != null)
        {
            if (directHitObj.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(1); 
            }
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, enemyLayer);
        
        foreach (var hitCollider in hitColliders)
        {
            if (directHitObj != null && hitCollider.gameObject == directHitObj) continue;

            EnemyPhalanx phalanxParent = hitCollider.GetComponentInParent<EnemyPhalanx>();
            if (phalanxParent != null && hitCollider.gameObject != phalanxParent.gameObject)
            {
                Debug.Log($"[ThunderClap-Explode] Hit shield!");
                phalanxParent.BreakShield();
            }

            if (hitCollider.TryGetComponent(out EnemyBase enemy))
            {
                enemy.Stun(stunDuration);
            }
        }

        if ((directHitObj != null || hitColliders.Length > 0) && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddUtilityScore("Thunderclap");
        }

        CameraShakeService.Shake(0.75f);

        PlayHitSfxOnce();
        
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}