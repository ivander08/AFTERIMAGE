using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    private Room room;

    private void Start()
    {
        room = GetComponentInParent<Room>();
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Player") && room != null)
        {
            room.PlayerEntered();
        }
    }
}
