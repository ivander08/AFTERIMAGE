using UnityEngine;
using System.Collections.Generic;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    private Dictionary<Room, RoomState> roomStates = new();
    private Room currentRoom;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterRoom(Room room)
    {
        roomStates[room] = new RoomState();
    }

    public void SetCurrentRoom(Room room)
    {
        currentRoom = room;
    }

    public Room GetCurrentRoom()
    {
        return currentRoom;
    }

    public bool IsRoomCleared(Room room)
    {
        return roomStates.ContainsKey(room) && roomStates[room].isCleared;
    }

    public void SetRoomCleared(Room room)
    {
        if (roomStates.ContainsKey(room))
        {
            roomStates[room].isCleared = true;
        }
    }

    public void ResetAllRooms()
    {
        foreach (var room in roomStates.Keys)
        {
            roomStates[room].isCleared = false;
        }
        currentRoom = null;
    }

    public bool IsRoomLocked(Room room)
    {
        return roomStates.ContainsKey(room) && roomStates[room].isLocked;
    }

    public void SetRoomLocked(Room room, bool locked)
    {
        if (roomStates.ContainsKey(room))
        {
            roomStates[room].isLocked = locked;
        }
    }
}

public class RoomState
{
    public bool isCleared;
    public bool isLocked;
}
