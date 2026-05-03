using UnityEngine;

public class HoloLureProjectile : BaseProjectile
{
    public GameObject lureDevicePrefab;
    [Header("VFX")]
    public GameObject hitVfx;
    [Header("Lure Spawn")]
    public float lureSpawnDistance = 0.5f;
    public float lureSpawnMinDistance = 0.1f;
    public float lureSpawnMaxDistance = 1.5f;
    public float lureSpawnCheckRadius = 0.25f;
    public float lureSpawnStep = 0.15f;
    [Header("Lure Scale Animation")]
    public float lureGrowDuration = 0.2f;
    public float lureShrinkDuration = 0.2f;
    [Header("Audio")]
    public AudioClip hitSfx;
    public float hitSfxVolume = 1f;
    private bool _didPlayHitSfx;

    private void PlayHitSfxOnce()
    {
        if (_didPlayHitSfx || hitSfx == null) return;
        _didPlayHitSfx = true;
        AudioService.PlayClip(hitSfx, transform.position, hitSfxVolume, 1f, minDistance: 2f);
    }

    public override void OnHit(Collider other)
    {
        Debug.Log($"[HoloLure] Hit {other.name}, spawning lure device");

        PlayHitSfxOnce();
        
        if (hitVfx != null)
        {
            Instantiate(hitVfx, transform.position, Quaternion.LookRotation(_lastHitNormal));
        }

        if (lureDevicePrefab != null)
        {
            float spawnDist = Mathf.Clamp(lureSpawnDistance, lureSpawnMinDistance, lureSpawnMaxDistance);
            Vector3 spawnPos = transform.position + _lastHitNormal * spawnDist;

            int attempts = 0;
            while (Physics.CheckSphere(spawnPos, lureSpawnCheckRadius, ~0, QueryTriggerInteraction.Ignore) && attempts < 12)
            {
                spawnPos += _lastHitNormal * lureSpawnStep;
                attempts++;
                if (Vector3.Distance(transform.position, spawnPos) > lureSpawnMaxDistance) break;
            }

            GameObject lureInstance = Instantiate(lureDevicePrefab, spawnPos, Quaternion.LookRotation(_lastHitNormal));
            LureScaler scaler = lureInstance.GetComponent<LureScaler>();
            if (scaler == null) scaler = lureInstance.AddComponent<LureScaler>();
            scaler.growDuration = lureGrowDuration;
            scaler.shrinkDuration = lureShrinkDuration;
        }

        Destroy(gameObject);
    }
}
