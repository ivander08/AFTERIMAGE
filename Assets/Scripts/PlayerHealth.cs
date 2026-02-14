using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public int maxHealth = 1;
    private int _currentHealth;

    public bool isDead = false;

    void Awake()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        _currentHealth -= damage;
        Debug.Log("PLAYER HIT!");

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        
        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<PlayerDash>().enabled = false;

        GetComponentInChildren<Renderer>().material.color = Color.black;

        Invoke(nameof(RestartLevel), 1f);
    }

    void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}