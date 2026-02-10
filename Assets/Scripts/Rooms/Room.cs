using UnityEngine;
using System.Collections.Generic;

public class Room : MonoBehaviour
{
    public string roomName;
    [SerializeField] private Transform environment;

    private List<EnemyBase> enemies = new();
    private int enemiesAlive;
    private List<Door> doors = new();
    private Door entryDoor;

    private void Start()
    {
        RoomManager.Instance.RegisterRoom(this);

        if (environment != null)
        {
            SetupEnvironmentTrigger(environment);
        }

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

    private void SetupEnvironmentTrigger(Transform environment)
    {
        BoxCollider collider = environment.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = environment.gameObject.AddComponent<BoxCollider>();
        }
        collider.isTrigger = true;

        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasGeometry = false;

        Renderer[] renderers = environment.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (!hasGeometry)
            {
                bounds = renderer.bounds;
                hasGeometry = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (hasGeometry)
        {
            Vector3 center = bounds.center - environment.position;
            collider.center = center;
            collider.size = bounds.size;
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
