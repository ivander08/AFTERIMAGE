using UnityEngine;
using UnityEngine.InputSystem;

public class UtilityManager : MonoBehaviour
{
    public BaseUtility[] availableUtilities;

    [SerializeField] private int _currentUtilityIndex = 0;
    
    private BaseUtility CurrentUtility => 
        availableUtilities != null && availableUtilities.Length > 0 
            ? availableUtilities[_currentUtilityIndex] 
            : null;

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
        return CurrentUtility.TryUse();
    }

    void OnGUI()
    {
        if (CurrentUtility == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUILayout.Label($"Current Utility: {CurrentUtility.UtilityName}");
        GUILayout.Label($"Uses: {CurrentUtility.CurrentUses}/{CurrentUtility.MaxUses}");
        
        if (CurrentUtility.IsOnCooldown)
        {
            GUILayout.Label($"Cooldown: {CurrentUtility.CooldownRemaining:F1}s");
        }
        
        GUILayout.Label("Q - Switch Utility | Right Click - Use");
        GUILayout.EndArea();
    }

    public BaseUtility GetCurrentUtility() => CurrentUtility;
    public string GetCurrentUtilityName() => CurrentUtility?.UtilityName ?? "None";
}