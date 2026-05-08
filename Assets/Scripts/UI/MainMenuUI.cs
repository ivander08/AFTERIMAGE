using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public Button continueButton;

    private void Start()
    {
        // 1. Force the cursor to be visible and unlocked when returning to the menu
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (continueButton == null) return;

        // Check if save data exists
        bool hasSave = PlayerPrefs.HasKey("SavedNextScene");
        
        // Disable the button if no save
        continueButton.interactable = hasSave;

        // Tell the text effect script to update its color to "Disabled"
        if (continueButton.TryGetComponent(out MenuTextHoverEffect effect))
        {
            effect.RefreshVisuals();
        }
    }

    // Call this from your New Game Button's OnClick event
    public void StartNewGame()
    {
        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.StartNewGame();
    }

    // Call this from your Continue Button's OnClick event
    public void ContinueGame()
    {
        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.ContinueGame();
    }

    // Call this from your Exit Button's OnClick event
    public void QuitGame()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();
        
        // This ensures the play mode stops if you are testing in the Unity Editor
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}