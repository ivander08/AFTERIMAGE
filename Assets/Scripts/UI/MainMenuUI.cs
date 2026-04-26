// Assets/Scripts/UI/MainMenuUI.cs
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
}