using UnityEngine;

[CreateAssetMenu(fileName = "CaptionSequenceAsset", menuName = "AFTERIMAGE/Caption/Sequence Asset")]
public class CaptionSequenceAsset : ScriptableObject
{
    [SerializeField] private CaptionSequence[] sequences = new CaptionSequence[1];
    [SerializeField] private bool freezeInput = false;
    [SerializeField] private bool freezeEnemies = false;

    public CaptionSequence[] Sequences => sequences;
    public int SequenceCount => sequences.Length;
    public bool FreezeInput => freezeInput;
    public bool FreezeEnemies => freezeEnemies;

    public CaptionSequence GetSequence(int index)
    {
        if (index < 0 || index >= sequences.Length) return null;
        return sequences[index];
    }
}
