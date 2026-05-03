// Assets/Scripts/Rooms/EchoArenaController.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.Cinemachine;

public class EchoArenaController : MonoBehaviour
{
    public static EchoArenaController Instance { get; private set; }
    public static bool IsBossActive { get; private set; } = false;

    [Header("References")]
    public EnemyEcho echoBoss;
    public Room room;

    [Header("Positions")]
    public Transform playerStartPos;
    public Transform checkpointPos;

    [Header("HP UI")]
    public GameObject bossHPPanel;
    public Image[] hpDots;
    public Material hpFullMaterial;
    public Material hpEmptyMaterial;

    private PlayerMovement _playerMovement;
    private PlayerHealth _playerHealth;
    private List<EnemyBase> _normalEnemies = new();
    private bool _introPlaying = false;
    private int _echoMaxHP = 3;
        private Vector3 _echoStartPosition;
    private Quaternion _echoStartRotation;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerMovement = player.GetComponent<PlayerMovement>();
            _playerHealth = player.GetComponent<PlayerHealth>();
        }

        if (room != null)
        {
            foreach (var enemy in room.GetEnemies())
            {
                if (enemy != null && enemy != echoBoss)
                {
                    _normalEnemies.Add(enemy);
                    enemy.OnDeath += OnNormalEnemyDied;
                }
            }
        }

        if (bossHPPanel != null) bossHPPanel.SetActive(false);

        if (echoBoss != null)
        {
            _echoStartPosition = echoBoss.transform.position;
            _echoStartRotation = echoBoss.transform.rotation;
            echoBoss.isInvulnerable = true;
            echoBoss.SetFrozen(true);
            echoBoss.OnHpChanged += OnEchoDamaged;
        }
    }

    private void OnNormalEnemyDied()
    {
        bool onlyEchoLeft = true;
        foreach (var enemy in _normalEnemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                onlyEchoLeft = false;
                break;
            }
        }

        if (onlyEchoLeft && !IsBossActive && !_introPlaying)
            StartBossFight();
    }

    private void StartBossFight()
    {
        _introPlaying = false;
        IsBossActive = true;

        // Teleport player to start pos
        TeleportPlayer(playerStartPos);

        // Show HP panel
        if (bossHPPanel != null)
        {
            bossHPPanel.SetActive(true);
            UpdateHPDots(_echoMaxHP);
        }

        // Activate Echo
        if (echoBoss != null)
        {
            echoBoss.isInvulnerable = false;
            echoBoss.SetFrozen(false);
            echoBoss.AssignRoom(room);
            RoomManager.Instance.SetCurrentRoom(room);
            echoBoss.NotifyPlayerEnteredRoom();
        }

        Debug.Log("[EchoArena] Boss fight started!");
    }

    public void OnEchoDamaged(int currentHP)
    {
        UpdateHPDots(currentHP);
    }

    private void UpdateHPDots(int currentHP)
    {
        if (hpDots == null) return;
        for (int i = 0; i < hpDots.Length; i++)
        {
            if (hpDots[i] != null)
                hpDots[i].material = i < currentHP ? hpFullMaterial : hpEmptyMaterial;
        }
    }

    // Called by DeathPanelController when Try Again is pressed
    public void RespawnForRetry()
    {
        if (!IsBossActive) return;
        StartCoroutine(RetryRoutine());
    }

    private IEnumerator RetryRoutine()
    {
        yield return null;

        TeleportPlayer(checkpointPos);

        if (_playerHealth != null) _playerHealth.ResetHealth();

        Animator playerAnimator = _playerMovement.GetComponentInChildren<Animator>();
        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger("deathTrigger");
            playerAnimator.SetInteger("deathIndex", 0);
            playerAnimator.Play("Breathing Idle", 0, 0f); // force back to Idle immediately
        }

        // Re-enable components that PlayerHealth.Die() disabled
        if (_playerMovement != null)
        {
            _playerMovement.enabled = true;
            _playerMovement.isMovementLocked = false;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerDash pd = player.GetComponent<PlayerDash>();
            if (pd != null) pd.enabled = true;

            // Restore player color (Die() sets it to black)
            Renderer r = player.GetComponentInChildren<Renderer>();
            if (r != null) r.material.color = Color.cyan;

            // Re-enable reticle
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null && ph.reticleObject != null)
                ph.reticleObject.SetActive(true);
        }

        if (echoBoss != null)
        {
            // Teleport Echo back to start
            CharacterController echoCC = echoBoss.GetComponent<CharacterController>();
            if (echoCC != null) echoCC.enabled = false;
            echoBoss.transform.position = _echoStartPosition;
            echoBoss.transform.rotation = _echoStartRotation;
            if (echoCC != null) echoCC.enabled = true;

            echoBoss.ResetForRetry();
            echoBoss.isInvulnerable = false;
            echoBoss.AssignRoom(room);
            RoomManager.Instance.SetCurrentRoom(room);
            echoBoss.NotifyPlayerEnteredRoom();
        }

        if (bossHPPanel != null) bossHPPanel.SetActive(true);
        UpdateHPDots(_echoMaxHP);

        // Unlock audio
        AudioService.SetLock(false);
        if (AmbientAudioController.Instance != null)
            AmbientAudioController.Instance.CrossfadeTo(
                AmbientAudioController.Instance.startingAmbient,
                1.5f
            );

        Debug.Log("[EchoArena] Retry — fight resumed from checkpoint.");

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;

        if (DeathPanelController.Instance != null)
            DeathPanelController.Instance.gameObject.SetActive(false);
    }

    public void SkipToBossEncounter()
    {
        // Kill every enemy in the entire level except Echo
        Room[] allRooms = FindObjectsOfType<Room>();
        foreach (var r in allRooms)
        {
            var enemiesCopy = new List<EnemyBase>(r.GetEnemies());
            foreach (var enemy in enemiesCopy)
            {
                if (enemy != null && enemy != echoBoss && !enemy.IsDead)
                    enemy.ForceKill();
            }
        }

        if (!IsBossActive)
            StartBossFight();
    }

    private void TeleportPlayer(Transform target)
    {
        if (target == null || _playerMovement == null) return;
        CharacterController cc = _playerMovement.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        _playerMovement.transform.position = target.position;
        _playerMovement.transform.rotation = target.rotation;
        if (cc != null) cc.enabled = true;
    }

    private void OnDestroy()
    {
        if (echoBoss != null)
            echoBoss.OnHpChanged -= OnEchoDamaged;

        if (Instance == this)
        {
            Instance = null;
            IsBossActive = false;
        }
    }
}