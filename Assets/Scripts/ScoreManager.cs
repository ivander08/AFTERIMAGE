using UnityEngine;
using System;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public event Action<int, string> OnScoreAdded;
    public event Action<int> OnChainUpdated;

    [Header("Current Score")]
    [SerializeField] private int _totalScore = 0;
    
    [Header("Chain Settings")]
    public float chainTimeWindow = 2.0f;
    public int chainIncrement = 25;
    public int maxChainBonus = 250;

    private float _lastKillTime = -99f;
    private int _currentChainBonus = 0;

    public int TotalScore => _totalScore;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void AddKillScore(int baseScore, string enemyName)
    {
        int pointsAwarded = baseScore;

        if (Time.time <= _lastKillTime + chainTimeWindow)
        {
            _currentChainBonus += chainIncrement;
            if (_currentChainBonus > maxChainBonus) _currentChainBonus = chainIncrement;
            pointsAwarded += _currentChainBonus;
        }
        else
        {
            _currentChainBonus = 0;
        }
        
        OnChainUpdated?.Invoke(_currentChainBonus);

        _lastKillTime = Time.time;
        AddScore(pointsAwarded, $"KILL");
    }

    public void AddMultiKillBonus(int enemiesKilled)
    {
        if (enemiesKilled >= 2)
        {
            int bonus = enemiesKilled * 40;
            AddScore(bonus, $"Multi-Kill ({enemiesKilled}x)");
        }
    }

    public void AddThrowableBonus() => AddScore(60, "Throwable Stun");
    
    public void AddUltimateBonus() => AddScore(700, "Ultimate: Iaijutsu Break");

    public void AddUtilityScore(string utilityName)
    {
        int score = utilityName.Contains("Kunai") ? 50 : 100;
        AddScore(score, $"Utility Used: {utilityName}");
    }

    public void CalculateTimeBonus(float timeInSeconds)
    {
        int timeBonus = 0;
        if (timeInSeconds <= 30f) timeBonus = 2000;
        else if (timeInSeconds <= 60f) timeBonus = 1500;
        else if (timeInSeconds <= 90f) timeBonus = 1000;
        else if (timeInSeconds <= 120f) timeBonus = 500;

        AddScore(timeBonus, $"Time Bonus ({timeInSeconds:F1}s)");
    }

    private void AddScore(int amount, string reason)
    {
        _totalScore += amount;
        Debug.Log($"[Score] +{amount} | Reason: {reason} | Total: {_totalScore}");
        
        OnScoreAdded?.Invoke(amount, reason);
    }
}