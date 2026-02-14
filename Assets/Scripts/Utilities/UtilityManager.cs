using UnityEngine;
using UnityEngine.InputSystem;

public class UtilityManager : MonoBehaviour
{
    public BaseUtility[] availableUtilities;

    [SerializeField] private int _currentUtilityIndex = 0;
    private PlayerHealth _playerHealth;
    
    private BaseUtility CurrentUtility => 
        availableUtilities != null && availableUtilities.Length > 0 
            ? availableUtilities[_currentUtilityIndex] 
            : null;

    private void Awake()
    {
        _playerHealth = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        HandleSwitching();
        HandleUsage();
    }

    private void HandleSwitching()
    {
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            SwitchToNextUtility();
        }
    }

    private void HandleUsage()
    {
        if (_playerHealth != null && _playerHealth.isDead) return;
        
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            UseCurrentUtility();
        }
    }

    public void SwitchToNextUtility()
    {
        if (availableUtilities == null || availableUtilities.Length <= 1) return;

        _currentUtilityIndex = (_currentUtilityIndex + 1) % availableUtilities.Length;
        Debug.Log($"Switched to: {CurrentUtility?.UtilityName}");
    }

    public bool UseCurrentUtility()
    {
        if (CurrentUtility == null) return false;
        return CurrentUtility.TryUse(transform);
    }

    public BaseUtility GetCurrentUtility() => CurrentUtility;
    public string GetCurrentUtilityName() => CurrentUtility?.UtilityName ?? "None";

    private void OnGUI()
    {
        if (CurrentUtility == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUILayout.Label($"Current: {CurrentUtility.UtilityName}", new GUIStyle(GUI.skin.label) { fontSize = 16 });
        GUILayout.Label($"Uses: {CurrentUtility.CurrentUses}/{CurrentUtility.MaxUses}");
        GUILayout.Label($"Cooldown: {(CurrentUtility.IsOnCooldown ? Mathf.RoundToInt(CurrentUtility.CooldownRemaining * 100) / 100f : 0)}s");
        GUILayout.EndArea();
    }
}