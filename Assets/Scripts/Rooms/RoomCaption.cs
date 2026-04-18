using UnityEngine;
using System.Collections;

public class RoomCaption : MonoBehaviour
{
    [SerializeField] private RoomCaptionConfig captionConfig;
    private Room _room;
    private bool _captionPlayed = false;
    private bool _freezeEnemiesActive = false;

    private void Awake()
    {
        _room = GetComponent<Room>();
        if (_room == null)
        {
            Debug.LogError($"RoomCaption on {gameObject.name} requires a Room component!", this);
        }
    }

    /// <summary>
    /// Called by Room when player enters. Plays captions if available and configured.
    /// </summary>
    public void OnPlayerEntered()
    {
        if (_captionPlayed) return;
        if (captionConfig == null || !captionConfig.HasCaptions) return;
        if (!captionConfig.IsTutorialRoom) return;

        Debug.Log($"[RoomCaption] Playing caption for room: {gameObject.name}");
        _captionPlayed = true;
        PlayCaptions();
    }

    private void PlayCaptions()
    {
        if (_room == null) return;

        // Lock room during caption
        _room.LockRoomForCaption();

        // Play caption sequences
        CaptionSequenceAsset captionAsset = captionConfig.CaptionSequenceAsset;
        if (captionAsset == null)
        {
            Debug.LogError($"[RoomCaption] CaptionSequenceAsset is NULL! Did you assign it in the config?");
            return;
        }
        
        _freezeEnemiesActive = captionAsset.FreezeEnemies;
        if (_freezeEnemiesActive)
            SetRoomEnemiesFrozen(true);

        CaptionManager.Instance.Play(captionAsset.Sequences, OnCaptionComplete, captionAsset.FreezeInput);
    }

     private void OnCaptionComplete()
    {
        if (_room == null) return;

        if (captionConfig != null && captionConfig.TutorialData.showTutorial && TutorialUIManager.Instance != null)
        {
            TutorialUIManager.Instance.ShowTutorial(captionConfig.TutorialData, FinishRoomUnlock);
        }
        else
        {
            FinishRoomUnlock();
        }
    }

    private void FinishRoomUnlock()
    {
        if (_freezeEnemiesActive)
            SetRoomEnemiesFrozen(false);

        _room.UnlockRoomAfterCaption();
    }

    private void SetRoomEnemiesFrozen(bool frozen)
    {
        foreach (var enemy in _room.GetEnemies())
        {
            if (enemy != null && !enemy.IsDead)
                enemy.SetFrozen(frozen);
        }
    }

    /// <summary>
    /// Set caption config (can be used if assigning config at runtime)
    /// </summary>
    public void SetCaptionConfig(RoomCaptionConfig config)
    {
        captionConfig = config;
    }

    public bool HasCaptions => captionConfig != null && captionConfig.HasCaptions;

    public void PlayCompletionCaption()
    {
        if (captionConfig == null || !captionConfig.HasCompletionCaption) return;

        // Play completion captions
        CaptionSequenceAsset captionAsset = captionConfig.CompletionCaptionAsset;
        _freezeEnemiesActive = captionAsset.FreezeEnemies;
        CaptionManager.Instance.Play(captionAsset.Sequences, OnCompletionCaptionComplete, captionAsset.FreezeInput);
    }

    private void OnCompletionCaptionComplete()
    {
        // Caption complete
    }
}
