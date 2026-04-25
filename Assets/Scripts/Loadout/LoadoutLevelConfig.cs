using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "AFTERIMAGE/Level Loadout Config")]
public class LevelLoadoutConfig : ScriptableObject
{
    public string levelSceneName;
    public UtilityDefinition[] availableUtilities;
}