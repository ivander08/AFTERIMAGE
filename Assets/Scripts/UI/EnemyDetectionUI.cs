using UnityEngine;
using UnityEngine.UI;

public class EnemyDetectionUI : MonoBehaviour
{
    public Image targetingSquare;
    public Image indicatorIcon;
    public Color indicatorColor = Color.white;
    private Color _originalTargetingSquareColor;
    private RectTransform _rectTransform;

    private void Start()
    {
        SetupIndicator();
        _originalTargetingSquareColor = targetingSquare.color;
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
            indicatorIcon.color = indicatorColor;
        }
    }

    public void SetHighlightColor(Color color)
    {
        if (targetingSquare != null)
        {
            targetingSquare.color = color;
        }
    }
}