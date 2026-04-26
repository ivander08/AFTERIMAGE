using UnityEngine;

public class HoloLureUtility : BaseUtility
{
    public GameObject lureProjectilePrefab; 
    [Header("Audio")]
    public AudioClip throwSfx;
    [Range(0f, 1f)] public float throwSfxVolume = 0.2f;

    public override string UtilityName => "Holo-Lure";

    protected override void ExecuteUtility(Transform origin)
    {
        if (lureProjectilePrefab == null) return;

        if (throwSfx != null)
        {
            AudioService.PlayClip2D(throwSfx, throwSfxVolume, 1f);
        }
        
        Vector3 spawnPos = origin.position + Vector3.up;
        Instantiate(lureProjectilePrefab, spawnPos, origin.rotation);
    }
}
