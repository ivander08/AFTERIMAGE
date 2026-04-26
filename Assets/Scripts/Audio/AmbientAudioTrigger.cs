// Assets/Scripts/Audio/AmbientAudioTrigger.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AmbientAudioTrigger : MonoBehaviour
{[Header("Audio Track")]
    [Tooltip("The audio to permanently switch to when touching this trigger.")]
    public AudioClip ambientClip;

    [Header("Settings")]
    public float crossfadeDuration = 1.5f;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && ambientClip != null)
        {
            if (AmbientAudioController.Instance != null)
            {
                AmbientAudioController.Instance.CrossfadeTo(ambientClip, crossfadeDuration);
            }
        }
    }
}