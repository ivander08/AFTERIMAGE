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
                Vector3 dashDir = (hitPoint - player.position).normalized;
                dashDir.y = 0;
                
                RaycastHit[] hits = Physics.SphereCastAll(player.position, 1f, dashDir, Mathf.Max(maxDashDistance, 2f));
                Door doorInPath = null;
                
                foreach (var rayHit in hits)
                {
                    if (rayHit.collider.TryGetComponent(out Door door))
                    {
                        if (!door.IsLocked())
                        {
                            doorInPath = door;
                            break;
                        }
                    }
                }
                
                if (doorInPath != null)
                {
                    DoorDashZone zone = doorInPath.GetComponent<DoorDashZone>();
                    if (zone != null)
                    {
                        Vector3 landingPos = zone.GetLandingPosition();
                        clampedCursor.position = new Vector3(landingPos.x, hit.point.y, landingPos.z);
                        return;
                    }
                }
                
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