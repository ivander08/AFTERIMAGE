// Assets/Scripts/Rooms/FinalRoomGate.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// Keeps a door permanently locked (red) until every Room in the scene
/// except the target room itself is cleared. Once all are cleared, the
/// door unlocks normally and Room_30 combat begins when the player dashes through.
/// </summary>
[RequireComponent(typeof(Door))]
public class FinalRoomGate : MonoBehaviour
{
    [Tooltip("The room that should only unlock after everything else is cleared.")]
    public Room finalRoom;

    [Tooltip("How often (seconds) to poll whether all other rooms are cleared.")]
    public float checkInterval = 0.5f;

    private Door _door;
    private bool _unlocked = false;

    private void Awake()
    {
        _door = GetComponent<Door>();
    }

    private IEnumerator Start()
    {
        // Force-lock immediately and keep it red until conditions are met
        _door.Lock();

        while (!_unlocked)
        {
            yield return new WaitForSeconds(checkInterval);

            if (AreAllOtherRoomsCleared())
            {
                _unlocked = true;
                _door.Unlock();
                Debug.Log("[FinalRoomGate] All rooms cleared — final room door unlocked.");
            }
        }
    }

    private bool AreAllOtherRoomsCleared()
    {
        Room[] allRooms = FindObjectsOfType<Room>();
        foreach (var room in allRooms)
        {
            // Skip the final room itself
            if (room == finalRoom) continue;

            // If any room still has a living enemy, not cleared yet
            foreach (var enemy in room.GetEnemies())
            {
                if (enemy != null && !enemy.IsDead)
                    return false;
            }
        }
        return true;
    }
}