// Assets/Scripts/Loadout/LoadoutHoverEffect.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LoadoutHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Color Swap")]
    public bool useColorSwap = false;
    public Color normalColor = new Color(1f, 0.6f, 0f, 1f);
    public Color hoverColor  = new Color(0.2f, 0.8f, 1f, 1f);
    public Color clickColor  = Color.white;

    [Header("Material Swap")]
    public bool useMaterialSwap = false;
    public Material normalMaterial;
    public Material hoverMaterial;
    public Material clickMaterial;

    [Header("Debug")]
    public bool debugLog = true;

    private Image _image;
    private bool _isHovered;
    private bool _isClicking;

    private void Awake()
    {
        _image = GetComponent<Image>();
        ApplyNormal();
    }

    private void OnDisable()
    {
        // Reset state when disabled to prevent stuck hover
        _isHovered = false;
        _isClicking = false;
        CancelInvoke(nameof(ReturnToNormal));
        ApplyNormal();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;

        if (debugLog)
            Debug.Log($"[HoverEffect] ENTER → {gameObject.name} | hovered={_isHovered} | clicking={_isClicking}");

        if (!_isClicking)
            ApplyHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;

        if (debugLog)
            Debug.Log($"[HoverEffect] EXIT → {gameObject.name} | hovered={_isHovered} | clicking={_isClicking}");

        if (!_isClicking)
            ApplyNormal();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (debugLog)
            Debug.Log($"[HoverEffect] CLICK → {gameObject.name} | hovered={_isHovered}");

        _isClicking = true;
        ApplyClick();

        CancelInvoke(nameof(ReturnToNormal));
        Invoke(nameof(ReturnToNormal), 0.1f);
    }

    private void ReturnToNormal()
    {
        _isClicking = false;

        if (debugLog)
            Debug.Log($"[HoverEffect] ReturnToNormal → {gameObject.name} | hovered={_isHovered}");

        if (_isHovered)
            ApplyHover();
        else
            ApplyNormal();
    }

    private void ApplyHover()
    {
        if (_image == null) return;
        if (useColorSwap) _image.color = hoverColor;
        if (useMaterialSwap && hoverMaterial != null) _image.material = hoverMaterial;
    }

    private void ApplyClick()
    {
        if (_image == null) return;
        if (useColorSwap) _image.color = clickColor;
        if (useMaterialSwap && clickMaterial != null) _image.material = clickMaterial;
    }

    private void ApplyNormal()
    {
        if (_image == null) return;
        if (useColorSwap) _image.color = normalColor;
        if (useMaterialSwap && normalMaterial != null) _image.material = normalMaterial;
    }
}