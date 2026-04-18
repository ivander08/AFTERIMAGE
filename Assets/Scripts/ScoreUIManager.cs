using UnityEngine;
using TMPro;
using System.Collections;

public class ScoreUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI mainScoreText;
    public TextMeshProUGUI chainText;
    public Transform feedContainer;
    
    [Header("Prefabs")]
    public GameObject feedItemPrefab; 

    private float _displayScore = 0f;

    private void Start()
    {
        // Hide chain text by default
        chainText.gameObject.SetActive(false);

        // Listen to the ScoreManager
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreAdded += HandleScoreAdded;
            ScoreManager.Instance.OnChainUpdated += HandleChainUpdated;
        }
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreAdded -= HandleScoreAdded;
            ScoreManager.Instance.OnChainUpdated -= HandleChainUpdated;
        }
    }

    private void Update()
    {
        // Animate the score rolling up quickly instead of snapping instantly
        if (ScoreManager.Instance != null && _displayScore < ScoreManager.Instance.TotalScore)
        {
            _displayScore = Mathf.Lerp(_displayScore, ScoreManager.Instance.TotalScore, Time.deltaTime * 10f);
            
            // Snap to final if it's very close
            if (ScoreManager.Instance.TotalScore - _displayScore < 1f) 
                _displayScore = ScoreManager.Instance.TotalScore;

            mainScoreText.text = Mathf.FloorToInt(_displayScore).ToString();
        }
    }

     private void HandleScoreAdded(int amount, string reason)
    {
        // 1. Spawn the prefab (the Empty/Panel)
        GameObject newItemObj = Instantiate(feedItemPrefab, feedContainer);
        
        // 2. Find all TextMeshPro components inside it
        // Usually, Index 0 is Amount, Index 1 is Reason based on Hierarchy order
        TextMeshProUGUI[] texts = newItemObj.GetComponentsInChildren<TextMeshProUGUI>();
        
        if(texts.Length >= 2)
        {
            texts[0].text = $"+{amount}";
            texts[1].text = reason.ToUpper();
        }
        
        // 3. Set to index 1 (below the Chain text)
        newItemObj.transform.SetSiblingIndex(1); 
        
        // 4. Start the fade on the GameObject
        StartCoroutine(FadeAndDestroyRoutine(newItemObj));
    }

    private void HandleChainUpdated(int chainBonus)
    {
        if (chainBonus > 0)
        {
            chainText.gameObject.SetActive(true);
            chainText.text = $"CHAIN ACTIVE (+{chainBonus})";
        }
        else
        {
            chainText.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeAndDestroyRoutine(GameObject container)
    {
        yield return new WaitForSeconds(1.5f);

        // Get all text components to fade them all out together
        TextMeshProUGUI[] texts = container.GetComponentsInChildren<TextMeshProUGUI>();
        float fadeTime = 0.5f;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            
            foreach(var txt in texts)
            {
                Color c = txt.color;
                c.a = alpha;
                txt.color = c;
            }
            yield return null;
        }

        Destroy(container);
    }
}