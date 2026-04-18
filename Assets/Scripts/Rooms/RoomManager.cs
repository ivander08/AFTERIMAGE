using UnityEngine;

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

    private void Start()
    {
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