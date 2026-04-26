using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LoadoutHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{[Header("Color Swap")]
    public bool useColorSwap = false;
    public Color normalColor = new Color(1f, 0.6f, 0f, 1f);
    public Color hoverColor  = new Color(0.2f, 0.8f, 1f, 1f);
    public Color clickColor  = Color.white;

    [Header("Material Swap")]
    public bool useMaterialSwap = false;
    public Material normalMaterial;
    public Material hoverMaterial;
    public Material clickMaterial;

    [Header("Audio")]
    public AudioClip hoverSound;
    [Range(0f, 1f)] public float hoverVolume = 0.5f;
    public AudioClip clickSound;
    [Range(0f, 1f)] public float clickVolume = 0.8f;

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
        _isHovered = false;
        _isClicking = false;
        CancelInvoke(nameof(ReturnToNormal));
        ApplyNormal();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        if (!_isClicking)
        {
            ApplyHover();
            if (hoverSound != null) AudioService.PlayClip2D(hoverSound, hoverVolume);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        if (!_isClicking)
        {
            ApplyNormal();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _isClicking = true;
        ApplyClick();
        
        if (clickSound != null) AudioService.PlayClip2D(clickSound, clickVolume);

        CancelInvoke(nameof(ReturnToNormal));
        Invoke(nameof(ReturnToNormal), 0.1f);
    }

    private void ReturnToNormal()
    {
        _isClicking = false;
        if (_isHovered) ApplyHover();
        else ApplyNormal();
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