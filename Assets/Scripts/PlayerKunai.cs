using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerKunai : MonoBehaviour
{
    public GameObject kunaiPrefab;
    public Transform spawnPoint;

    public float throwCooldown = 0.5f;
    public int maxKunai = 3;
    public float rechargeTime = 4.0f;

    [SerializeField] private int currentKunai;
    private float _lastThrowTime;
    private bool _isRecharging = false;

    private void Awake()
    {
        currentKunai = maxKunai;
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            AttemptThrow();
        }

        if (currentKunai < maxKunai && !_isRecharging)
        {
            StartCoroutine(RechargeRoutine());
        }
    }

    void AttemptThrow()
    {
        if (Time.time < _lastThrowTime + throwCooldown) return;

        if (currentKunai <= 0)
        {
            Debug.Log("Out of Kunai!");
            return;
        }

        ThrowKunai();
    }

    void ThrowKunai()
    {
        if (kunaiPrefab == null) return;

        _lastThrowTime = Time.time;
        currentKunai--;

        Vector3 spawnPos = (spawnPoint != null) ? spawnPoint.position : transform.position + Vector3.up;
        Instantiate(kunaiPrefab, spawnPos, transform.rotation);
    }

    IEnumerator RechargeRoutine()
    {
        _isRecharging = true;
        yield return new WaitForSeconds(rechargeTime);
        
        if (currentKunai < maxKunai)
        {
            currentKunai++;
        }
        
        _isRecharging = false;
    }
}