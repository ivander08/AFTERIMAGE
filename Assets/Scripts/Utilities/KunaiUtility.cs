using UnityEngine;

public class KunaiUtility : BaseUtility
{
    public GameObject kunaiPrefab;

    protected override void ExecuteUtility()
    {
        if (kunaiPrefab == null) return;

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up;
        Instantiate(kunaiPrefab, spawnPos, transform.rotation);
    }

    protected override void OnUsageFailed()
    {
        Debug.Log("Out of Kunai!");
    }

    public override string UtilityName => "Kunai";
}