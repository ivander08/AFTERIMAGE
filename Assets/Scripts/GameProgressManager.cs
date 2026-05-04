using UnityEngine;

/// <summary>
/// Persists game progress (levels unlocked, score totals) between scenes.
/// </summary>
using UnityEngine.SceneManagement;

public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    [Header("Scene Routing")]
    public string level0SceneName = "Level0";
    public string loadoutSceneName = "LoadoutScene";
    public string mainMenuSceneName = "MainMenu";

    [Header("Level Sequence")][Tooltip("Drag your configs in order: Level 1 Config, Level 2 Config, Level 3 Config...")]
    public LevelLoadoutConfig[] loadoutConfigs;

    // LoadoutManager will read this when LoadoutScene opens
    public LevelLoadoutConfig CurrentPendingConfig { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartNewGame()
    {
        // Reset progress to the beginning
        PlayerPrefs.SetString("SavedNextScene", level0SceneName);
        PlayerPrefs.Save();
        SceneTransitionManager.Instance.LoadScene(level0SceneName);
    }

    public void ContinueGame()
    {
        // Read the highest unlocked level scene name, default to Level 0
        string savedScene = PlayerPrefs.GetString("SavedNextScene", level0SceneName);

        if (savedScene == level0SceneName)
        {
            SceneTransitionManager.Instance.LoadScene(level0SceneName);
        }
        else
        {
            // Find the config that matches the saved scene and go to the Loadout UI
            CurrentPendingConfig = GetConfigForScene(savedScene);
            
            if (CurrentPendingConfig != null)
            {
                SceneTransitionManager.Instance.LoadScene(loadoutSceneName);
            }
            else
            {
                // Fallback if data is missing or corrupted
                SceneTransitionManager.Instance.LoadScene(level0SceneName);
            }
        }
    }

    public void CompleteCurrentLevel(string currentSceneName)
    {
        LevelLoadoutConfig nextConfig = GetNextConfig(currentSceneName);

        if (nextConfig != null)
        {
            // Save our progress for the next level
            PlayerPrefs.SetString("SavedNextScene", nextConfig.levelSceneName);
            PlayerPrefs.Save();

            // Setup the LoadoutScene and transition
            CurrentPendingConfig = nextConfig;
            SceneTransitionManager.Instance.LoadScene(loadoutSceneName);
        }
        else
        {
            // No next config means the game is fully beaten.
            PlayerPrefs.DeleteKey("SavedNextScene"); 
            SceneTransitionManager.Instance.LoadScene(mainMenuSceneName);
        }
    }

    private LevelLoadoutConfig GetNextConfig(string currentSceneName)
    {
        if (currentSceneName == level0SceneName && loadoutConfigs.Length > 0)
            return loadoutConfigs[0];

        for (int i = 0; i < loadoutConfigs.Length; i++)  // ← full range
        {
            if (loadoutConfigs[i].levelSceneName == currentSceneName)
            {
                // Last config = true end of game
                if (i + 1 >= loadoutConfigs.Length) return null;
                return loadoutConfigs[i + 1];
            }
        }

        return null;
    }
    
    private LevelLoadoutConfig GetConfigForScene(string sceneName)
    {
        foreach (var config in loadoutConfigs)
        {
            if (config.levelSceneName == sceneName) return config;
        }
        return null;
    }
}