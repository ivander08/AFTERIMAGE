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
    private bool _isCombatActive = false;
    private bool _captionLocked = false;
    private RoomCaption _roomCaption;

    public bool IsCombatActive => _isCombatActive;

    private void Start()
    {
        _enemies.AddRange(GetComponentsInChildren<EnemyBase>());
        _doors.AddRange(GetComponentsInChildren<Door>());
        _roomCaption = GetComponent<RoomCaption>();

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
        
        // Check if caption should play
        if (_roomCaption != null)
        {
            _roomCaption.OnPlayerEntered();
        }

        // Only lock for combat if no caption is playing and room has enemies
        if (!_captionLocked && !_isCleared && _enemies.Count > 0)
        {
            LockRoom();
            _isCombatActive = true;
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

    public void LockRoomForCaption()
    {
        _captionLocked = true;
        LockRoom();
    }

    public void UnlockRoomAfterCaption()
    {
        _captionLocked = false;
        // Don't unlock fully - let combat lock take over if enemies are alive
        if (_isCleared)
        {
            UnlockRoom();
        }
        else if (_enemies.Count > 0)
        {
            // Re-activate combat lock
            LockRoom();
            _isCombatActive = true;
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
