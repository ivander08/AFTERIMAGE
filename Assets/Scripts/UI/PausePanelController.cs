// Assets/Scripts/UI/PausePanelController.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CanvasGroup))]
public class PausePanelController : MonoBehaviour
{
    public static PausePanelController Instance { get; private set; }
    public static bool IsPaused { get; private set; }

    [Header("UI References")]
    public Button resumeButton;
    public Button exitButton;

    [Header("Scene Routing")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Audio")]
    public AudioClip pauseSfx;
    public AudioClip unpauseSfx;

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _canvasGroup = GetComponent<CanvasGroup>();
        
        // Ensure it starts hidden
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        IsPaused = false;

        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (exitButton != null) exitButton.onClick.AddListener(ExitToMainMenu);
    }

    private void Update()
    {
        // Listen for the Escape key to toggle pause
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        // Don't allow pausing if the game is busy with intro or outro
        if (PreGamePanel.IsPlaying || FinishPanelController.IsFinished) return;
        
        // Prevent pausing if the death panel is showing
        if (DeathPanelController.Instance != null && DeathPanelController.Instance.gameObject.activeInHierarchy) return;

        // Failsafe: Prevent pausing if the player is technically dead but the panel hasn't faded in yet
        PlayerHealth player = FindObjectOfType<PlayerHealth>();
        if (player != null && player.isDead) return;

        if (IsPaused)
            ResumeGame();
        else
            PauseGame();
    }

    private void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f; 
        AudioListener.pause = true; // Mute world & ambient sounds

        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        Cursor.visible = true; 

        if (pauseSfx != null) AudioService.PlayClip2D(pauseSfx, 0.5f);
    }

    private void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f; 
        AudioListener.pause = false; // Unmute world sounds

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        Cursor.visible = false; 

        if (unpauseSfx != null) AudioService.PlayClip2D(unpauseSfx, 0.5f);
    }

    private void ExitToMainMenu()
    {
        Time.timeScale = 1f; 
        IsPaused = false;
        AudioListener.pause = false; // CRITICAL: Unmute before changing scenes!
        AudioService.SetLock(false);

        // This will now auto-spawn the transition manager if it's missing
        SceneTransitionManager.Instance.LoadScene(mainMenuSceneName);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f; 
        AudioListener.pause = false; // Failsafe
    }
}