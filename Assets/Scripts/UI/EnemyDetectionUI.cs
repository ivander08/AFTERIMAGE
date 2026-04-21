using UnityEngine;
using UnityEngine.UI;

public class EnemyDetectionUI : MonoBehaviour
{
    public Image targetingSquare;
    public Image indicatorIcon;
    public Color indicatorColor = Color.white;
    public AudioClip highlightEnterSound;
    public AudioClip highlightExitSound;

    private Color _originalTargetingSquareColor;
    private RectTransform _rectTransform;
    private bool _isHighlighted;

    private void Start()
    {
        if (targetingSquare != null)
        {
            _originalTargetingSquareColor = targetingSquare.color;
            targetingSquare.enabled = false;
        }

        SetupIndicator();
        _rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (_rectTransform != null)
        {
            _rectTransform.rotation = Quaternion.Euler(-90, 0, 0);
        }
    }

    private void SetupIndicator()
    {
        if (indicatorIcon != null)
        {
            // This icon is intentionally unused for now.
            indicatorIcon.gameObject.SetActive(false);
        }
    }

    public void SetHighlighted(bool active)
    {
        if (_isHighlighted == active)
        {
            return;
        }

        _isHighlighted = active;

        if (targetingSquare != null)
        {
            targetingSquare.enabled = active;
            targetingSquare.color = active ? Color.yellow : _originalTargetingSquareColor;
        }

        if (active)
        {
            AudioService.PlayClip2D(highlightEnterSound, 0.05f);
        }
        else
        {
            AudioService.PlayClip2D(highlightExitSound, 0.05f);
        }
    }

    public void SetHighlightColor(Color color)
    {
        SetHighlighted(color == Color.white);
    }
}