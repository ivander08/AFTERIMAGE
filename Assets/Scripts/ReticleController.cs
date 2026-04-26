using UnityEngine;
using UnityEngine.InputSystem;

public class ReticleController : MonoBehaviour
{
    public Transform player;
    public Transform rawCursor;
    public Transform clampedCursor;
    
    public float maxDashDistance = 6f;
    public LayerMask groundMask;
    public LayerMask environmentMask;

    Camera _cam;

    void Awake()
    {
        _cam = Camera.main;
    }

    void Update()
    {
        if (player == null || Mouse.current == null) return;

        if (CaptionManager.IsFrozen || TutorialUIManager.IsOpen || PreGamePanel.IsPlaying || FinishPanelController.IsFinished || PausePanelController.IsPaused)
        {
            if (rawCursor != null) rawCursor.gameObject.SetActive(false);
            if (clampedCursor != null) clampedCursor.gameObject.SetActive(false);
            
            Cursor.visible = true; 
            return;
        }

        Cursor.visible = false;

        if (rawCursor != null) rawCursor.gameObject.SetActive(true);
        if (clampedCursor != null) clampedCursor.gameObject.SetActive(true);

        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        
        // Create a plane at player's Y level to raycast against
        Plane playerPlane = new Plane(Vector3.up, player.position.y);
        
        Vector3 hitPoint = player.position;
        
        // Try to hit ground first for accurate Y positioning
        if (Physics.Raycast(ray, out RaycastHit groundHit, 999f, groundMask))
        {
            hitPoint = groundHit.point;
            hitPoint.y = player.position.y + 0.1f;
        }
        else
        {
            // No ground found - project ray onto player's Y plane instead
            if (playerPlane.Raycast(ray, out float enter))
            {
                hitPoint = ray.origin + ray.direction * enter;
                hitPoint.y = player.position.y + 0.1f;
            }
        }

        // Update raw cursor - always follows mouse direction
        if (rawCursor != null)
        {
            rawCursor.position = hitPoint;
        }

        // Update clamped cursor - shows max dash range
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
                    if (!door.IsLocked() && !door.IsBroken)
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
                    Vector3 landingPos = zone.GetLandingPosition(player.position);
                    clampedCursor.position = new Vector3(landingPos.x, hitPoint.y, landingPos.z);
                    return;
                }
            }
            
            Vector3 dir = hitPoint - player.position;
            float dist = dir.magnitude;
            
            if (dist > maxDashDistance)
            {
                dir = dir.normalized * maxDashDistance;
            }

            if (Physics.SphereCast(player.position, 0.5f, dir.normalized, out RaycastHit envHit, dir.magnitude, environmentMask, QueryTriggerInteraction.Ignore))
            {
                dir = dir.normalized * envHit.distance;
            }

            clampedCursor.position = player.position + dir;
            clampedCursor.position = new Vector3(clampedCursor.position.x, hitPoint.y, clampedCursor.position.z);
        }
    }
}