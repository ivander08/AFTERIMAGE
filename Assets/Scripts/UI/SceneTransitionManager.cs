// Assets/Scripts/UI/SceneTransitionManager.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public float fadeDuration = 0.5f;
    public Color fadeColor = Color.black;

    private Canvas _canvas;
    private Image _fadeImage;
    private bool _isTransitioning;

    private static SceneTransitionManager _instance;
    public static SceneTransitionManager Instance
    {
        get
        {
            // If it doesn't exist, create it instantly
            if (_instance == null)
            {
                _instance = FindObjectOfType<SceneTransitionManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SceneTransitionManager");
                    _instance = go.AddComponent<SceneTransitionManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        CreateFadeUI();
    }

    private void CreateFadeUI()
    {
        // 1. Create a Canvas that sits on top of EVERYTHING (Sort Order 999)
        GameObject canvasObj = new GameObject("TransitionCanvas");
        canvasObj.transform.SetParent(transform);
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceCamera;
        _canvas.sortingOrder = 999;
        canvasObj.AddComponent<CanvasScaler>();

        // 2. Create the Black Image
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        _fadeImage = imageObj.AddComponent<Image>();
        _fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f); // Start transparent
        _fadeImage.raycastTarget = false; // Don't block clicks initially

        // 3. Stretch it across the screen
        RectTransform rect = _fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public void LoadScene(string sceneName)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        _isTransitioning = true;
        _fadeImage.raycastTarget = true; // Block clicks during transition

        // Fade Out (To Black)
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            _fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }

        // Load the scene asynchronously so the game doesn't freeze
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
        {
            yield return null;
        }

        // Optional: A tiny breathing room after loading before fading in
        yield return new WaitForSeconds(0.2f);

        // Fade In (To Transparent)
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            _fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }

        _fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        _fadeImage.raycastTarget = false;
        _isTransitioning = false;
    }
}