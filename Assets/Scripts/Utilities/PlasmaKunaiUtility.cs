using UnityEngine;

public class PlasmaKunaiUtility : BaseUtility
{
    public GameObject plasmaKunaiPrefab;
    [Header("Audio")]
    public AudioClip throwSfx;
    [Range(0f, 1f)] public float throwSfxVolume = 0.2f;

    protected override void ExecuteUtility(Transform origin)
    {
        if (plasmaKunaiPrefab == null) return;

        if (throwSfx != null)
        {
            AudioService.PlayClip2D(throwSfx, throwSfxVolume, 1f);
        }

        Vector3 spawnPos = origin.position + Vector3.up;
        Instantiate(plasmaKunaiPrefab, spawnPos, origin.rotation);
    }

    protected override void OnUsageFailed()
    {
        Debug.Log("Out of Plasma Kunai!");
    }

    public override string UtilityName => "Plasma Kunai";
}