using System.Collections;
using UnityEngine;

public class LureScaler : MonoBehaviour
{
    public float growDuration = 0.2f;
    public float shrinkDuration = 0.2f;
    private Vector3 _targetScale = Vector3.one;
    private Coroutine _activeCoroutine;

    void Awake()
    {
        _targetScale = transform.localScale;
        transform.localScale = Vector3.zero;
    }

    void Start()
    {
        if (growDuration > 0f)
        {
            _activeCoroutine = StartCoroutine(GrowCoroutine());
        }
        else
        {
            transform.localScale = _targetScale;
        }
    }

    IEnumerator GrowCoroutine()
    {
        float t = 0f;
        while (t < growDuration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / growDuration);
            transform.localScale = Vector3.Lerp(Vector3.zero, _targetScale, f);
            yield return null;
        }
        transform.localScale = _targetScale;
        _activeCoroutine = null;
    }

    public void ShrinkAndDestroy(float durationOverride = -1f)
    {
        float dur = durationOverride > 0f ? durationOverride : shrinkDuration;
        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
        _activeCoroutine = StartCoroutine(ShrinkCoroutine(dur));
    }

    IEnumerator ShrinkCoroutine(float duration)
    {
        float start = Time.time;
        Vector3 startScale = transform.localScale;
        float t = 0f;
        while (t < duration)
        {
            t = Time.time - start;
            float f = Mathf.Clamp01(t / duration);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, f);
            yield return null;
        }
        transform.localScale = Vector3.zero;
        Destroy(gameObject);
    }
}
