using UnityEngine;[CreateAssetMenu(fileName = "LevelConfig", menuName = "AFTERIMAGE/Level Loadout Config")]
public class LevelLoadoutConfig : ScriptableObject
{
    public string levelSceneName;
    public UtilityDefinition[] availableUtilities;

    [Header("Pre-Game Briefing")]
    public string dateText = "17 January 2147";
    public string locationText = "South Hayazuki District, Japan";[TextArea] public string missionText = "Cleanse the Lumina Clan.";
}