// Assets/Scripts/UI/DeathPanelController.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class DeathPanelController : MonoBehaviour
{
    public static DeathPanelController Instance { get; private set; }

    [Header("Settings")]
    public float fadeDuration = 1.5f;
    public AudioClip deathPanelAudioClip;[Header("UI References")]
    public Button tryAgainButton;
    

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        
        gameObject.SetActive(false); 

        if (tryAgainButton != null)
        {
            tryAgainButton.onClick.AddListener(OnTryAgainClicked);
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        Cursor.visible = true; 

        // Hide the button entirely to bypass the glowing material alpha bug
        if (tryAgainButton != null)
        {
            tryAgainButton.gameObject.SetActive(false);
        }

        // Play the UI sound immediately
        if (deathPanelAudioClip != null)
        {
            AudioService.PlayClip2D(deathPanelAudioClip, 0.2f);
        }

        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;

        // Reveal the glowing button AFTER the background is done fading
        if (tryAgainButton != null)
        {
            tryAgainButton.gameObject.SetActive(true);
        }
    }

    private void OnTryAgainClicked()
    {
        AudioService.SetLock(false); // Unlock for the next life
        PreGamePanel.SkipNextPreGame = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}