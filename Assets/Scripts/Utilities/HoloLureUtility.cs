using UnityEngine;

public class HoloLureUtility : BaseUtility
{
    public GameObject lureProjectilePrefab; 

    public override string UtilityName => "Holo-Lure";

    protected override void ExecuteUtility(Transform origin)
    {
        if (lureProjectilePrefab == null) return;
        
        Vector3 spawnPos = origin.position + Vector3.up;
        Instantiate(lureProjectilePrefab, spawnPos, origin.rotation);
    }
}
