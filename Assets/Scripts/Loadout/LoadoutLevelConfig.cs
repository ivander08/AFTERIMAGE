using UnityEngine;[CreateAssetMenu(fileName = "LevelConfig", menuName = "AFTERIMAGE/Level Loadout Config")]
public class LevelLoadoutConfig : ScriptableObject
{
    public string levelSceneName;
    public UtilityDefinition[] availableUtilities;

    [Header("Flashback Cutscene")]
    public bool hasFlashback = false;
    public Sprite flashbackImage;
    public AudioClip flashbackAudio;
    [TextArea(2, 4)] public string flashbackTextAfter;[Header("Pre-Game Briefing")]
    public string dateText = "17 January 2147";
    public string locationText = "South Hayazuki District, Japan";
    [TextArea] public string missionText = "Cleanse the Lumina Clan.";
    [Tooltip("If assigned, the loadout screen will announce this utility as newly unlocked.")]
    public UtilityDefinition newlyUnlockedUtility;
}