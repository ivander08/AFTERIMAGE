using UnityEngine;

[CreateAssetMenu(fileName = "RoomCaptionConfig", menuName = "AFTERIMAGE/Caption/Room Config")]
public class RoomCaptionConfig : ScriptableObject
{
    [SerializeField] private bool isTutorialRoom = true;
    [SerializeField] private CaptionSequenceAsset captionSequenceAsset;
    [SerializeField] private CaptionSequenceAsset completionCaptionAsset;

    public bool IsTutorialRoom => isTutorialRoom;
    public CaptionSequenceAsset CaptionSequenceAsset => captionSequenceAsset;
    public CaptionSequenceAsset CompletionCaptionAsset => completionCaptionAsset;

    public bool HasCaptions => captionSequenceAsset != null && captionSequenceAsset.SequenceCount > 0;
    public bool HasCompletionCaption => completionCaptionAsset != null && completionCaptionAsset.SequenceCount > 0;
}
