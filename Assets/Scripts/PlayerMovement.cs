using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float rotateSpeed = 40f;
    public LayerMask groundMask;

    public Transform camTarget;

    public float camLookAhead = 2f; 
    public float maxMouseDist = 10f;

    public float camSmoothSpeed = 20f;

    [HideInInspector] public bool isMovementLocked = false;

    CharacterController _cc;
    Camera _cam;

    private Vector3 _currentAimDirection; 

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _cam = Camera.main;
    }

    void Update()
    {
        HandleInputAndRotation();

        if (!isMovementLocked)
        {
            HandleMovement();
        }
    }

    void LateUpdate()
    {
        UpdateCameraTarget();
    }

    void HandleInputAndRotation()
    {
        if (Mouse.current == null) return;
        
        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 999f, groundMask))
        {
            Vector3 target = hit.point;
            target.y = transform.position.y;

            _currentAimDirection = target - transform.position;

            if (_currentAimDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(_currentAimDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
            }
        }
    }

    void HandleMovement()
    {
        Vector2 input = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1;
            if (Keyboard.current.sKey.isPressed) input.y -= 1;
            if (Keyboard.current.aKey.isPressed) input.x -= 1;
            if (Keyboard.current.dKey.isPressed) input.x += 1;
        }

        Vector3 move = new Vector3(input.x, 0f, input.y).normalized;
        Vector3 finalMove = move * moveSpeed;
        finalMove.y = -9.8f; 

        _cc.Move(finalMove * Time.deltaTime);
    }

    void UpdateCameraTarget()
    {
        if (camTarget == null) return;

        float currentDist = _currentAimDirection.magnitude;

        float t = Mathf.Clamp01(currentDist / maxMouseDist);

        Vector3 offset = _currentAimDirection.normalized * Mathf.Lerp(0f, camLookAhead, t);

        Vector3 desiredPos = transform.position + offset;
        camTarget.position = Vector3.Lerp(camTarget.position, desiredPos, camSmoothSpeed * Time.deltaTime);
    }
}