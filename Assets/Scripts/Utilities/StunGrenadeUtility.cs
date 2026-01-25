using UnityEngine;

public class StunGrenadeUtility : BaseUtility
{
    public GameObject stunGrenadePrefab;

    protected override void ExecuteUtility()
    {
        if (stunGrenadePrefab == null) return;

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up;
        Instantiate(stunGrenadePrefab, spawnPos, transform.rotation);
    }

    protected override void OnUsageFailed()
    {
        Debug.Log("Out of Stun Grenades!");
    }

    public override string UtilityName => "Stun Grenade";
}