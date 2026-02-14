using UnityEngine;

public class HoloLureProjectile : BaseProjectile
{
    public GameObject lureDevicePrefab;

    public override void OnHit(Collider other)
    {
        Debug.Log($"[HoloLure] Hit {other.name}, spawning lure device");
        
        if (lureDevicePrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.1f;
            Instantiate(lureDevicePrefab, spawnPos, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
