using UnityEngine;

public class DoorDashZone : MonoBehaviour
{
    public Room destinationRoom;
    
    private Vector3 landingPosition;

    private void Start()
    {
        landingPosition = transform.position + transform.forward * 2f;
    }

    public Vector3 GetLandingPosition()
    {
        return landingPosition;
    }

    public void OnPlayerDashThrough()
    {
        if (destinationRoom != null)
        {
            destinationRoom.SetEntryDoor(GetComponent<Door>());
        }
    }
}
