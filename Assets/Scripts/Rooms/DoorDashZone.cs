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
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 playerPos = player != null ? player.transform.position : transform.position;
        TransitionToNextRoom(playerPos);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Door door = GetComponent<Door>();
            if (door != null && door.IsBroken && !door.IsLocked())
            {
                TransitionToNextRoom(other.transform.position);
            }
        }
    }

    private void TransitionToNextRoom(Vector3 playerPos)
    {
        if (Time.time - _lastTransitionTime < 0.3f) return;
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
        {
            // Ghost entry: entered from a non-connected room.
            // If one room is cleared and the other has enemies, go to the uncleared one.
            bool aCleared = door.roomA != null && door.roomA.IsCleared;
            bool bCleared = door.roomB != null && door.roomB.IsCleared;
            
            if (aCleared != bCleared)
            {
                destinationRoom = aCleared ? door.roomB : door.roomA;
            }
            else
            {
                // Both same state — pick by approach direction
                Vector3 dirFromDoor = (playerPos - transform.position).normalized;
                dirFromDoor.y = 0;
                Vector3 dirToA = (door.roomA.transform.position - transform.position).normalized;
                dirToA.y = 0;
                destinationRoom = Vector3.Dot(dirFromDoor, dirToA) > 0 ? door.roomB : door.roomA;
            }
        }

        Debug.Log($"[DoorDashZone] TransitionToNextRoom: door={door.DoorName}, currentRoom={currentRoom?.RoomName}, destinationRoom={destinationRoom?.RoomName}");

        if (destinationRoom != null && destinationRoom != currentRoom)
        {
            destinationRoom.SetEntryDoor(door);
            destinationRoom.PlayerEntered();
        }
    }
}