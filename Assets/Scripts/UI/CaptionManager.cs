using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.InputSystem;

public class CaptionManager : MonoBehaviour
{
    public static CaptionManager Instance { get; private set; }

    public GameObject captionPanel;
    public TextMeshProUGUI captionText;
    public TextMeshProUGUI speakerNameText;
    public float typeSpeed = 0.03f;
    public AudioClip typeSound;

    public bool IsPlaying => captionPanel != null && captionPanel.activeSelf;
    public bool FreezeActive { get; private set; }
    public static bool IsFrozen => Instance != null && Instance.FreezeActive;

    private Action _onComplete;
    private CaptionSequence[] _currentSequences;
    private int _sequenceIndex;
    private int _messageIndex;
    private bool _isTyping;
    private Coroutine _typeCoroutine;
    private Coroutine _autoAdvanceCoroutine;
    private AudioSource _audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        if (captionPanel != null) captionPanel.SetActive(false);
    }

    public void Play(CaptionSequence[] sequences, Action onComplete, bool freeze = false)
    {
        if (sequences == null || sequences.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        _currentSequences = sequences;
        _onComplete = onComplete;
        _sequenceIndex = 0;
        _messageIndex = 0;
        FreezeActive = freeze;
        
        if (captionPanel != null) captionPanel.SetActive(true);
        ShowNextMessage();
    }

    private void Update()
    {
        if (!IsPlaying) return;
        if (!FreezeActive) return;

        bool nextPressed = false;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) nextPressed = true;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) nextPressed = true;

        if (nextPressed)
        {
            if (_isTyping)
            {
                StopCoroutine(_typeCoroutine);
                captionText.text = _currentSequences[_sequenceIndex].messages[_messageIndex].text;
                _isTyping = false;
            }
            else
            {
                _messageIndex++;
                ShowNextMessage();
            }
        }
    }

    private void ShowNextMessage()
    {
        // Check if we've finished current sequence
        if (_messageIndex >= _currentSequences[_sequenceIndex].messages.Length)
        {
            _sequenceIndex++;
            _messageIndex = 0;

            // Check if all sequences are done
            if (_sequenceIndex >= _currentSequences.Length)
            {
                captionPanel.SetActive(false);
                FreezeActive = false;
                CaptionCameraController.Instance?.Release();
                _onComplete?.Invoke();
                return;
            }
        }

        // Update speaker name and show message
        if (speakerNameText != null)
        {
            speakerNameText.text = _currentSequences[_sequenceIndex].speakerName;
        }

        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        if (_autoAdvanceCoroutine != null) StopCoroutine(_autoAdvanceCoroutine);

        CaptionMessage msg = _currentSequences[_sequenceIndex].messages[_messageIndex];
        CaptionCameraController.Instance?.ShowMessage(msg);
        _typeCoroutine = StartCoroutine(TypeRoutine(msg.text));
    }

    private IEnumerator TypeRoutine(string message)
    {
        _isTyping = true;
        captionText.text = "";
        foreach (char c in message)
        {
            captionText.text += c;
            if (typeSound != null && !char.IsWhiteSpace(c))
            {
                _audioSource.PlayOneShot(typeSound, 0.3f);
            }
            yield return new WaitForSeconds(typeSpeed);
        }
        _isTyping = false;

        // Check for auto-advance
        CaptionSequence currentSequence = _currentSequences[_sequenceIndex];
        if (currentSequence.autoAdvanceDelay > 0)
        {
            _autoAdvanceCoroutine = StartCoroutine(AutoAdvanceRoutine(currentSequence.autoAdvanceDelay));
        }
    }

    private IEnumerator AutoAdvanceRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!_isTyping)
        {
            _messageIndex++;
            ShowNextMessage();
        }
    }
}
