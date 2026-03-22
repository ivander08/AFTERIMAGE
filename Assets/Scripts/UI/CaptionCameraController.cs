using UnityEngine;
using Unity.Cinemachine;

public class CaptionCameraController : MonoBehaviour
{
    public static CaptionCameraController Instance { get; private set; }
    public static bool IsDriving => Instance != null && (Instance._isDrivingPosition || Instance._isDrivingZoom);

    public Transform camTarget;
    public CinemachinePositionComposer posComposer;
    public float lerpSpeed = 5f;

    private bool _isDrivingPosition;
    private bool _isDrivingZoom;
    private Vector3 _targetPosition;
    private float _targetZoom;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (_isDrivingPosition && camTarget != null)
            camTarget.position = Vector3.Lerp(camTarget.position, _targetPosition, lerpSpeed * Time.deltaTime);

        if (_isDrivingZoom && posComposer != null)
            posComposer.CameraDistance = Mathf.Lerp(posComposer.CameraDistance, _targetZoom, lerpSpeed * Time.deltaTime);
    }

    public void ShowMessage(CaptionMessage message)
    {
        if (!string.IsNullOrEmpty(message.cameraTargetId))
        {
            if (CaptionCameraTarget.TryGet(message.cameraTargetId, out Vector3 pos))
            {
                _targetPosition = pos;
                _isDrivingPosition = true;
            }
        }
        else
        {
            _isDrivingPosition = false;
        }

        if (message.cameraZoom > 0)
        {
            _targetZoom = message.cameraZoom;
            _isDrivingZoom = true;
        }
        else
        {
            _isDrivingZoom = false;
        }
    }

    public void Release()
    {
        _isDrivingPosition = false;
        _isDrivingZoom = false;
    }
}
