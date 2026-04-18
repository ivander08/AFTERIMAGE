using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TutorialTrigger : MonoBehaviour
{
    [Header("Tutorial Data to Show")]
    [SerializeField] private RoomCaptionConfig.TutorialConfigData tutorialData;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (tutorialData != null && tutorialData.showTutorial && TutorialUIManager.Instance != null)
            {
                TutorialUIManager.Instance.ShowTutorial(tutorialData, OnTutorialClosed);
            }
            
            gameObject.SetActive(false);
        }
    }

    private void OnTutorialClosed()
    {
    }
}