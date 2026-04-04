using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public int maxHealth = 1;
    private int _currentHealth;
    private Animator _animator;

    public bool isDead = false;

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

        if (_animator != null)
        {
            _animator.SetInteger("deathIndex", UnityEngine.Random.Range(0, 3));
            _animator.SetTrigger("deathTrigger");
        }

        Invoke(nameof(RestartLevel), 3f);
    }

    void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}