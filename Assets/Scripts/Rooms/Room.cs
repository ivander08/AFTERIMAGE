using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Room : MonoBehaviour
{
    public string roomName;

    private List<EnemyBase> _enemies = new();
    private List<Door> _doors = new();
    private Door _entryDoor;
    private bool _isCleared;

    private void Start()
    {
        _enemies.AddRange(GetComponentsInChildren<EnemyBase>());
        _doors.AddRange(GetComponentsInChildren<Door>());

        foreach (var enemy in _enemies)
        {
            enemy.AssignRoom(this);
            enemy.OnDeath += CheckClearCondition;
        }

        foreach (var door in _doors)
        {
            door.SetRoom(this);
        }
    }

    public void PlayerEntered()
    {
        RoomManager.Instance.SetCurrentRoom(this);
        if (!_isCleared && _enemies.Count > 0)
        {
            LockRoom();
        }
    }

    public void SetEntryDoor(Door door)
    {
        _entryDoor = door;

        if (_isCleared)
        {
            Destroy(door.gameObject);
            _entryDoor = null;
        }
    }

    private void LockRoom()
    {
        if (_entryDoor != null)
        {
            _entryDoor.Lock();
        }
        
        foreach (var door in _doors)
        {
            door.Lock();
        }
    }

    private void UnlockRoom()
    {
        if (_entryDoor != null)
        {
            Destroy(_entryDoor.gameObject);
        }
        
        foreach (var door in _doors)
        {
            if (door != null)
                door.Unlock();
        }
    }

    private void CheckClearCondition()
    {
        if (_enemies.All(e => e == null || e.IsDead))
        {
            _isCleared = true;
            UnlockRoom();
        }
    }

    public List<EnemyBase> GetEnemies()
    {
        return _enemies;
    }
}
