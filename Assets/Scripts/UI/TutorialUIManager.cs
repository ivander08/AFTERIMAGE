// Assets/Scripts/UI/TutorialUIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TutorialUIManager : MonoBehaviour
{
    public static TutorialUIManager Instance { get; private set; }
    
    // We can use this to keep the player frozen while the tutorial is open
    public static bool IsOpen => Instance != null && Instance.panelRoot.activeInHierarchy;

    [Header("UI References")]
    public GameObject panelRoot; // Assign the TutorialPanel itself
    public Image tutorialImage; // Assign TutorialSprite
    public TextMeshProUGUI mainText; // Assign TutorialText
    public TextMeshProUGUI tipText; // Assign TipText
    public Button tryItButton; // Assign the UI Button component

    private Action _onCompleteCallback;

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
        
        if (panelRoot != null) panelRoot.SetActive(false);
        if (tryItButton != null) tryItButton.onClick.AddListener(CloseTutorial);
    }

    public void ShowTutorial(RoomCaptionConfig.TutorialConfigData data, Action onComplete)
    {
        _onCompleteCallback = onComplete;

        if (tutorialImage != null)
        {
            tutorialImage.sprite = data.tutorialSprite;
            tutorialImage.material = data.tutorialMaterial;
        }

        if (mainText != null) mainText.text = data.mainText;
        if (tipText != null) tipText.text = data.tipText;

        panelRoot.SetActive(true);
    }

    private void CloseTutorial()
    {
        panelRoot.SetActive(false);
        _onCompleteCallback?.Invoke();
        _onCompleteCallback = null;
    }
}