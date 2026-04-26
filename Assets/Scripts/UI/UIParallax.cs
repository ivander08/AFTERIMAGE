// Assets/Scripts/UI/UIParallax.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class UIParallax : MonoBehaviour
{
    [Header("Settings")][Tooltip("How much the UI moves. Higher = more movement.")]
    public float movementMultiplier = 15f;
    [Tooltip("How smooth the transition is.")]
    public float smoothSpeed = 5f;
    [Tooltip("Invert the movement direction?")]
    public bool invertDirection = false;

    private RectTransform _rectTransform;
    private Vector2 _startPosition;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _startPosition = _rectTransform.anchoredPosition;
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        // 1. Get mouse position
        Vector2 mousePos = Mouse.current.position.ReadValue();

        // 2. Normalize mouse position relative to screen center (-1 to 1)
        float xOffset = (mousePos.x / Screen.width) - 0.5f;
        float yOffset = (mousePos.y / Screen.height) - 0.5f;

        // 3. Calculate target position
        Vector2 offset = new Vector2(xOffset, yOffset) * movementMultiplier;
        if (invertDirection) offset = -offset;

        Vector2 targetPosition = _startPosition + offset;

        // 4. Smoothly lerp towards target
        _rectTransform.anchoredPosition = Vector2.Lerp(
            _rectTransform.anchoredPosition, 
            targetPosition, 
            Time.unscaledDeltaTime * smoothSpeed
        );
    }
}