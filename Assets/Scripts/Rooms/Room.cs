using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Room : MonoBehaviour
{
    public string RoomName => gameObject.name;

    private List<EnemyBase> _enemies = new();
    private List<Door> _doors = new();
    private Door _entryDoor;
    private bool _isCleared;
    private bool _isCombatActive = false;
    private bool _captionLocked = false;
    private RoomCaption _roomCaption;

    public bool IsCombatActive => _isCombatActive;

    private void Awake()
    {
        _enemies.AddRange(GetComponentsInChildren<EnemyBase>());
        _roomCaption = GetComponent<RoomCaption>();

        foreach (var enemy in _enemies)
        {
            enemy.AssignRoom(this);
            enemy.OnDeath += CheckClearCondition;
        }
    }

    public void RegisterDoor(Door door)
    {
        if (door != null && !_doors.Contains(door))
        {
            _doors.Add(door);
        }
    }

    public void RegisterEnemy(EnemyBase enemy)
    {
        if (enemy == null) return;
        _enemies.Add(enemy);
        enemy.AssignRoom(this);
        enemy.OnDeath += CheckClearCondition;
    }

    public void PlayerEntered()
    {
        RoomManager.Instance.SetCurrentRoom(this);
        
        if (_roomCaption != null)
        {
            _roomCaption.OnPlayerEntered();
        }

        foreach (var enemy in _enemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                enemy.NotifyPlayerEnteredRoom();
            }
        }

        if (!_captionLocked && !_isCleared && _enemies.Count > 0)
        {
            LockRoom();
            _isCombatActive = true;
        }
    }

    public void SetEntryDoor(Door door)
    {
        _entryDoor = door;
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

    public void LockRoomForCaption()
    {
        _captionLocked = true;
        LockRoom();
    }

    public void UnlockRoomAfterCaption()
    {
        _captionLocked = false;
        
        if (_isCleared || _enemies.Count == 0)
        {
            UnlockRoom();
        }
        else
        {
            // Re-activate combat lock
            LockRoom();
            _isCombatActive = true;
        }
    }

    private void UnlockRoom()
    {
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
            
            // Play completion caption if available
            if (_roomCaption != null)
            {
                _roomCaption.PlayCompletionCaption();
            }
        }
    }

    public List<EnemyBase> GetEnemies()
    {
        return _enemies;
    }
}
