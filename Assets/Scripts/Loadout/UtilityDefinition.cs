using UnityEngine;

[CreateAssetMenu(fileName = "UtilityDef", menuName = "AFTERIMAGE/Utility Definition")]
public class UtilityDefinition : ScriptableObject
{
    public string utilityName;
    public Sprite icon;
    public Material iconMaterial;
    public int slotCost = 2;         // Plasma Kunai = 1, rest = 2
    public int defaultCount = 1;     // default amount in the loadout tray
    [TextArea] public string description;
}