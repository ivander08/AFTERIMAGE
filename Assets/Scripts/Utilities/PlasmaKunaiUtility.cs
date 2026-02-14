using UnityEngine;

public class PlasmaKunaiUtility : BaseUtility
{
    public GameObject plasmaKunaiPrefab;

    protected override void ExecuteUtility(Transform origin)
    {
        if (plasmaKunaiPrefab == null) return;

        Vector3 spawnPos = origin.position + Vector3.up;
        Instantiate(plasmaKunaiPrefab, spawnPos, origin.rotation);
    }

    protected override void OnUsageFailed()
    {
        Debug.Log("Out of Plasma Kunai!");
    }

    public override string UtilityName => "Plasma Kunai";
}