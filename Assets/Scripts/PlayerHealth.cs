using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Cinemachine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public int maxHealth = 1;
    private int _currentHealth;
    private Animator _animator;

    public bool isDead = false;

    public AudioClip[] deathSounds;
    public GameObject[] bloodDecalPrefabs;[Header("Death Camera Zoom")]
    public CinemachineCamera deathCamera;
    public float deathZoomDistance = 10f;
    public float deathZoomSpeed = 3f;

    public GameObject reticleObject;

    void Awake()
    {
        _currentHealth = maxHealth;
        _animator = GetComponentInChildren<Animator>();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        _currentHealth -= damage;
        Debug.Log("PLAYER HIT!");

        CameraShakeService.Shake(0.8f);

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        if (reticleObject != null) reticleObject.SetActive(false);

        // 1. Lock the audio service to stop enemies from making noise
        AudioService.StopAllSFX();
        AudioService.SetLock(true); 

        if (AmbientAudioController.Instance != null)
        {
            AmbientAudioController.Instance.FadeToSilence(1.0f);
        }

        AudioService.PlayRandom(deathSounds, transform.position, 2f, 0.95f, 1.05f);
        
        SpawnBloodPool();
        
        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<PlayerDash>().enabled = false;

        GetComponentInChildren<Renderer>().material.color = Color.black;

        if (_animator != null)
        {
            _animator.SetInteger("deathIndex", UnityEngine.Random.Range(0, 3));
            _animator.SetTrigger("deathTrigger");
        }

        // Instantly show the death panel & play its sound
        if (DeathPanelController.Instance != null)
        {
            DeathPanelController.Instance.Show();
        }

        // Trigger the camera zoom
        if (deathCamera != null)
        {
            StartCoroutine(DeathZoomRoutine());
        }
    }

    private IEnumerator DeathZoomRoutine()
    {
        var posComposer = deathCamera.GetComponent<CinemachinePositionComposer>();
        if (posComposer == null) yield break;

        // Smoothly zoom the camera in forever (until the scene restarts)
        while (true)
        {
            posComposer.CameraDistance = Mathf.Lerp(posComposer.CameraDistance, deathZoomDistance, Time.deltaTime * deathZoomSpeed);
            yield return null;
        }
    }

    void SpawnBloodPool()
    {
        if (bloodDecalPrefabs == null || bloodDecalPrefabs.Length == 0) return;

        int randomIndex = UnityEngine.Random.Range(0, bloodDecalPrefabs.Length);
        GameObject prefab = bloodDecalPrefabs[randomIndex];
        Vector3 spawnPos = transform.position + Vector3.up * 0.1f;
        GameObject decal = Instantiate(prefab, spawnPos, prefab.transform.rotation);
        decal.transform.Rotate(Vector3.forward, UnityEngine.Random.Range(0f, 360f), Space.Self);
    }
}