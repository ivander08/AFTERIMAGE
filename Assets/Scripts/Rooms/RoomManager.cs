// Assets/Scripts/Rooms/RoomManager.cs
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
        
        // FIX: Instantly set the current room so early dashes know exactly where they are starting from
        if (startingRoom != null)
        {
            CurrentRoom = startingRoom;
        }
    }

    private IEnumerator Start()
    {
        while (PreGamePanel.IsPlaying)
            yield return null;

        // Only enter the starting room if no dash has already moved us elsewhere
        if (startingRoom != null && CurrentRoom == startingRoom)
        {
            startingRoom.PlayerEntered();
        }
        else if (CurrentRoom != null && CurrentRoom != startingRoom)
        {
            // Player fast-dashed into another room before PreGame finished.
            // Re-notify that room's enemies to ensure aggro registered correctly.
            Debug.Log($"[RoomManager] Start: player already in {CurrentRoom.RoomName}, re-notifying.");
            CurrentRoom.ReNotifyAllEnemies();
        }
    }

    private static bool IsLevel3() =>
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Level3";

    public void SetCurrentRoom(Room room)
    {
        bool leavingEasterEgg = CurrentRoom != null && CurrentRoom.name == "Room_18" && IsLevel3();
        bool enteringEasterEgg = room != null && room.name == "Room_18" && IsLevel3();

        CurrentRoom = room;

        if (enteringEasterEgg && !leavingEasterEgg)
            MusicManager.Instance?.PlayEasterEggMusic();
        else if (!enteringEasterEgg && leavingEasterEgg)
            MusicManager.Instance?.ResumeLevelMusic();
    }
}