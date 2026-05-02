using UnityEngine;
using System.Collections;

/// <summary>
/// Singleton manager that tracks the player's current room
/// and triggers room entry logic.
/// </summary>

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }
    public Room CurrentRoom { get; private set; }

    [SerializeField] private Room startingRoom;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private IEnumerator Start()
    {
        // Wait for PreGamePanel to finish if it's playing
        while (PreGamePanel.IsPlaying)
        {
            yield return null;
        }

        if (startingRoom != null)
        {
            startingRoom.PlayerEntered();
        }
    }

    public void SetCurrentRoom(Room room)
    {
        CurrentRoom = room;
    }
}