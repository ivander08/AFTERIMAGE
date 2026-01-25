using UnityEngine;
using UnityEngine.InputSystem;

public class ReticleController : MonoBehaviour
{
    public Transform player;
    public Transform rawCursor;
    public Transform clampedCursor;
    
    public float maxDashDistance = 6f;
    public LayerMask groundMask;

    Camera _cam;

    void Awake()
    {
        _cam = Camera.main;
        Cursor.visible = false; 
    }

    void Update()
    {
        if (player == null || Mouse.current == null) return;

        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        
        if (Physics.Raycast(ray, out RaycastHit hit, 999f, groundMask))
        {
            Vector3 hitPoint = hit.point;
            hitPoint.y = player.position.y + 0.1f;

            if (rawCursor != null)
            {
                rawCursor.position = hitPoint;
            }

            if (clampedCursor != null)
            {
                Vector3 dir = hitPoint - player.position;
                float dist = dir.magnitude;
                
                if (dist > maxDashDistance)
                {
                    dir = dir.normalized * maxDashDistance;
                }

                clampedCursor.position = player.position + dir;
                clampedCursor.position = new Vector3(clampedCursor.position.x, hitPoint.y, clampedCursor.position.z);
            }
        }
    }
}