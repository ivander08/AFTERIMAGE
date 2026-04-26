using UnityEngine;

public class HoloLureProjectile : BaseProjectile
{
    public GameObject lureDevicePrefab;
    [Header("Audio")]
    public AudioClip hitSfx;
    public float hitSfxVolume = 1f;
    private bool _didPlayHitSfx;

    private void PlayHitSfxOnce()
    {
        if (_didPlayHitSfx || hitSfx == null) return;
        _didPlayHitSfx = true;
        AudioService.PlayClip(hitSfx, transform.position, hitSfxVolume, 1f);
    }

    public override void OnHit(Collider other)
    {
        Debug.Log($"[HoloLure] Hit {other.name}, spawning lure device");

        PlayHitSfxOnce();
        
        if (lureDevicePrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.1f;
            Instantiate(lureDevicePrefab, spawnPos, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
