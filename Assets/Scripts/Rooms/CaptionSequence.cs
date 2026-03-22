using UnityEngine;

[System.Serializable]
public class CaptionMessage
{
    public string text;
    [Tooltip("Id of a CaptionCameraTarget in the scene. Empty = no change.")]
    public string cameraTargetId;
    [Tooltip("Camera zoom distance override. 0 = no change.")]
    public float cameraZoom;
}

[System.Serializable]
public class CaptionSequence
{
    public string speakerName;
    public CaptionMessage[] messages;
    [Tooltip("Auto-advances after finishing. Set to 0 for manual advance.")]
    public float autoAdvanceDelay = 0f;
}
