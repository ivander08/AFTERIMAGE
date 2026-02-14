using UnityEngine;

public class ThunderClapUtility : BaseUtility
{
    public GameObject thunderclapPrefab;

    protected override void ExecuteUtility(Transform origin)
    {
        if (thunderclapPrefab == null) return;

        Vector3 spawnPos = origin.position + Vector3.up;
        Instantiate(thunderclapPrefab, spawnPos, origin.rotation);
    }

    protected override void OnUsageFailed()
    {
        Debug.Log("Out of Thunderclaps!");
    }

    public override string UtilityName => "Thunderclap";
}