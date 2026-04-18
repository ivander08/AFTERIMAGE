using UnityEngine;

[CreateAssetMenu(fileName = "RoomCaptionConfig", menuName = "AFTERIMAGE/Caption/Room Config")]
public class RoomCaptionConfig : ScriptableObject
{
    [System.Serializable]
    public class TutorialConfigData
    {
        public bool showTutorial;
        public Sprite tutorialSprite;
        public Material tutorialMaterial;
        [TextArea(2, 4)] public string mainText;
        public string tipText;
    }

    [SerializeField] private bool isTutorialRoom = true;
    [SerializeField] private CaptionSequenceAsset captionSequenceAsset;
    [SerializeField] private CaptionSequenceAsset completionCaptionAsset;
    
    [Header("Tutorial Panel Settings")]
    [SerializeField] private TutorialConfigData tutorialData;

    public bool IsTutorialRoom => isTutorialRoom;
    public CaptionSequenceAsset CaptionSequenceAsset => captionSequenceAsset;
    public CaptionSequenceAsset CompletionCaptionAsset => completionCaptionAsset;
    public TutorialConfigData TutorialData => tutorialData;

    public bool HasCaptions => captionSequenceAsset != null && captionSequenceAsset.SequenceCount > 0;
    public bool HasCompletionCaption => completionCaptionAsset != null && completionCaptionAsset.SequenceCount > 0;
}