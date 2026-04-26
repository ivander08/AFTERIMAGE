using UnityEngine;

public class ThunderClapUtility : BaseUtility
{
    public GameObject thunderclapPrefab;
    [Header("Audio")]
    public AudioClip throwSfx;
    [Range(0f, 1f)] public float throwSfxVolume = 0.2f;

    protected override void ExecuteUtility(Transform origin)
    {
        if (thunderclapPrefab == null) return;

        if (throwSfx != null)
        {
            AudioService.PlayClip2D(throwSfx, throwSfxVolume, 1f);
        }

        Vector3 spawnPos = origin.position + Vector3.up;
        Instantiate(thunderclapPrefab, spawnPos, origin.rotation);
    }

    protected override void OnUsageFailed()
    {
        Debug.Log("Out of Thunderclaps!");
    }

    public override string UtilityName => "Thunderclap";
}