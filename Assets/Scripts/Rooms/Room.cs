using UnityEngine;
using System.Collections.Generic;

public class Room : MonoBehaviour
{
    public string roomName;

    private List<EnemyBase> enemies = new();
    private int enemiesAlive;
    private List<Door> doors = new();
    private Door entryDoor;

    private void Start()
    {
        RoomManager.Instance.RegisterRoom(this);

        enemies.AddRange(GetComponentsInChildren<EnemyBase>());
        doors.AddRange(GetComponentsInChildren<Door>());

        enemiesAlive = enemies.Count;

        foreach (var door in doors)
        {
            door.SetRoom(this);
        }

        foreach (var enemy in enemies)
        {
            enemy.OnDeath += HandleEnemyDeath;
        }
    }

    public void PlayerEntered()
    {
        RoomManager.Instance.SetCurrentRoom(this);
        LockRoom();
    }

    public void SetEntryDoor(Door door)
    {
        entryDoor = door;

        if (enemiesAlive == 0)
        {
            Destroy(door.gameObject);
            entryDoor = null;
        }
    }

    private void LockRoom()
    {
        if (enemiesAlive == 0)
            return;

        RoomManager.Instance.SetRoomLocked(this, true);
        
        if (entryDoor != null)
        {
            entryDoor.Lock();
        }
        
        foreach (var door in doors)
        {
            door.Lock();
        }
    }

    private void UnlockRoom()
    {
        RoomManager.Instance.SetRoomLocked(this, false);
        
        if (entryDoor != null)
        {
            Destroy(entryDoor.gameObject);
        }
        
        foreach (var door in doors)
        {
            if (door != null)
                door.Unlock();
        }
    }

    private void HandleEnemyDeath()
    {
        enemiesAlive--;

        if (enemiesAlive <= 0)
        {
            RoomManager.Instance.SetRoomCleared(this);
            UnlockRoom();
        }
    }
}
