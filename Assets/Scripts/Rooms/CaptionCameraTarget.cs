using UnityEngine;
using System.Collections.Generic;

public class CaptionCameraTarget : MonoBehaviour
{
    [SerializeField] private string id;

    private static readonly Dictionary<string, CaptionCameraTarget> _registry = new();

    public static bool TryGet(string targetId, out Vector3 position)
    {
        if (_registry.TryGetValue(targetId, out var target))
        {
            position = target.transform.position;
            return true;
        }
        position = Vector3.zero;
        return false;
    }

    private void Awake() => _registry[id] = this;
    private void OnDestroy() => _registry.Remove(id);
}
