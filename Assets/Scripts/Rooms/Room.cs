// Assets/Scripts/Rooms/Room.cs
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
    
    // NEW: track which enemies have already been notified this entry
    private HashSet<EnemyBase> _notifiedEnemies = new();

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
            _doors.Add(door);
    }

    public void RegisterEnemy(EnemyBase enemy)
    {
        if (enemy == null) return;
        if (!_enemies.Contains(enemy))
        {
            _enemies.Add(enemy);
            enemy.AssignRoom(this);
            enemy.OnDeath += CheckClearCondition;

            if (_isCombatActive || !_isCleared)
                enemy.NotifyPlayerEnteredRoom();
        }
    }

    public void PlayerEntered()
    {
        int aliveCount = _enemies.Count(e => e != null && !e.IsDead);
        Debug.Log($"[Room] PlayerEntered: room={RoomName}, alive={aliveCount}, isCleared={_isCleared}, combatActive={_isCombatActive}");

        RoomManager.Instance.SetCurrentRoom(this);

        if (_roomCaption != null)
            _roomCaption.OnPlayerEntered();

        // Notify any enemy not yet notified this entry
        foreach (var enemy in _enemies)
        {
            if (enemy != null && !enemy.IsDead && !_notifiedEnemies.Contains(enemy))
            {
                Debug.Log($"[Room]   Notifying enemy {enemy.name}");
                enemy.NotifyPlayerEnteredRoom();
                _notifiedEnemies.Add(enemy);
            }
        }

        if (!_captionLocked && !_isCleared && _enemies.Count > 0 && !_isCombatActive)
        {
            LockRoom();
            _isCombatActive = true;
            Debug.Log($"[Room] Room locked for combat: {RoomName}");
        }
    }

    // NEW: call this to re-notify all living enemies (used by the aggro recheck)
    public void ReNotifyAllEnemies()
    {
        foreach (var enemy in _enemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                enemy.NotifyPlayerEnteredRoom();
                _notifiedEnemies.Add(enemy);
            }
        }
    }

    public void SetEntryDoor(Door door) => _entryDoor = door;

    private void LockRoom()
    {
        if (_entryDoor != null) _entryDoor.Lock();
        foreach (var door in _doors) door.Lock();
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
            UnlockRoom();
        else
        {
            LockRoom();
            _isCombatActive = true;
        }
    }

    private void UnlockRoom()
    {
        foreach (var door in _doors)
            if (door != null) door.Unlock();
    }

    private void CheckClearCondition()
    {
        if (_enemies.All(e => e == null || e.IsDead))
        {
            _isCleared = true;
            _isCombatActive = false;
            _notifiedEnemies.Clear();
            Debug.Log($"[Room] Room cleared: {RoomName}");
            UnlockRoom();
            if (_roomCaption != null) _roomCaption.PlayCompletionCaption();
        }
    }

    public List<EnemyBase> GetEnemies() => _enemies;
    public bool IsCleared => _isCleared;
}