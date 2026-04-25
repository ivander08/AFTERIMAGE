using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShakeService : MonoBehaviour
{
    public static CameraShakeService Instance { get; private set; }
    private CinemachineImpulseSource _impulseSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    /// <summary>
    /// Calls a screen shake. force = 1f is standard. 0.3f is light.
    /// </summary>
    public static void Shake(float force = 1f)
    {
        if (Instance != null && Instance._impulseSource != null)
        {
            Instance._impulseSource.GenerateImpulseWithForce(force);
        }
    }
}