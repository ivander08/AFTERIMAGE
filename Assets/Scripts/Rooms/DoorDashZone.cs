using UnityEngine;

public class DoorDashZone : MonoBehaviour
{
    private float _lastTransitionTime;

    public Vector3 GetLandingPosition(Vector3 playerPosition)
    {
        Vector3 toPlayer = playerPosition - transform.position;
        bool isInFront = Vector3.Dot(toPlayer, transform.forward) > 0;
        Vector3 landingDirection = isInFront ? -transform.forward : transform.forward;
        
        return transform.position + landingDirection * 2f;
    }

    public void OnPlayerDashThrough()
    {
        TransitionToNextRoom();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Door door = GetComponent<Door>();
            if (door != null && door.IsBroken)
            {
                TransitionToNextRoom();
            }
        }
    }

    private void TransitionToNextRoom()
    {
        if (Time.time - _lastTransitionTime < 0.5f) return; 
        _lastTransitionTime = Time.time;

        Door door = GetComponent<Door>();
        if (door == null) return;

        Room currentRoom = RoomManager.Instance.CurrentRoom;
        Room destinationRoom = null;

        if (door.roomA == currentRoom) 
            destinationRoom = door.roomB;
        else if (door.roomB == currentRoom) 
            destinationRoom = door.roomA;
        else 
            destinationRoom = door.roomA != null ? door.roomA : door.roomB; 

        if (destinationRoom != null && destinationRoom != currentRoom)
        {
            destinationRoom.SetEntryDoor(door);
            destinationRoom.PlayerEntered();
        }
    }
}