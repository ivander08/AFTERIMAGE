// Assets/Scripts/UI/MenuTextHoverEffect.cs
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuTextHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.cyan;
    public Color clickColor = Color.yellow;
    public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("Audio")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    private TextMeshProUGUI _text;
    private Button _button;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        _button = GetComponent<Button>();
        RefreshVisuals();
    }

    private void OnEnable() => RefreshVisuals();

    public void RefreshVisuals()
    {
        if (_text == null) return;
        
        // If there's a button component and it's not interactable, use disabled color
        if (_button != null && !_button.interactable)
            _text.color = disabledColor;
        else
            _text.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_button != null && !_button.interactable) return;
        
        _text.color = hoverColor;
        if (hoverSound != null) AudioService.PlayClip2D(hoverSound, 0.2f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_button != null && !_button.interactable) return;
        _text.color = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_button != null && !_button.interactable) return;

        _text.color = clickColor;
        if (clickSound != null) AudioService.PlayClip2D(clickSound, 0.2f);
    }
}