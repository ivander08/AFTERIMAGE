using UnityEngine;
using System.Collections;

public class HoloLure : MonoBehaviour, IDamageable
{
    public float lifetime = 3f;
    public float aggroRadius = 20f;
    public LayerMask enemyLayer;

    [Header("Health")]
    public int maxHits = 3;

    [Header("Destroy FX")]
    public GameObject destroyVfx;
    public AudioClip destroySfx;
    public float destroySfxVolume = 1f;

    [Header("Hit Reaction")]
    public GameObject hitVfx;
    public AudioClip hitSfx;
    public float hitSfxVolume = 1f;
    public float hitFlashDuration = 0.1f;
    public Color hitFlashColor = Color.white;

    private int currentHits;
    private Renderer[] renderers;
    private Color[][] originalColors;
    private MaterialPropertyBlock propBlock;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private void Start()
    {
        currentHits = 0;
        CacheRenderers();
        StartCoroutine(LifetimeRoutine());
        AttractEnemies();
    }

    private void CacheRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        propBlock = new MaterialPropertyBlock();
        originalColors = new Color[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            var matCount = renderers[i].sharedMaterials.Length;
            originalColors[i] = new Color[matCount];
            for (int j = 0; j < matCount; j++)
            {
                originalColors[i][j] = renderers[i].sharedMaterials[j].GetColor(EmissionColor);
            }
        }
    }

    private void OnDestroy()
    {
        ReleaseEnemies();
        StopAllCoroutines();

        if (destroyVfx != null)
        {
            Instantiate(destroyVfx, transform.position, Quaternion.identity);
        }

        if (destroySfx != null)
        {
            AudioService.PlayClip(destroySfx, transform.position, destroySfxVolume, 1f, -1f, 10f, -1f);
        }
    }

    void AttractEnemies()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, aggroRadius, enemyLayer);
        bool affectedEnemy = false;

        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out EnemyBase enemy))
            {
                enemy.SetTargetOverride(transform);
                affectedEnemy = true;
            }
        }

        if (affectedEnemy && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddUtilityScore("Holo-Lure");
        }
    }

    void ReleaseEnemies()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, aggroRadius, enemyLayer);
        foreach (var col in colliders)
        {
            if (col != null && col.TryGetComponent(out EnemyBase enemy))
            {
                enemy.SetTargetOverride(null);
            }
        }
    }

    IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }

    public void TakeDamage(int damage)
    {
        currentHits++;

        if (currentHits >= maxHits)
        {
            Destroy(gameObject);
            return;
        }

        // Play hit feedback
        if (hitVfx != null)
        {
            Instantiate(hitVfx, transform.position, Quaternion.identity);
        }

        if (hitSfx != null)
        {
            AudioService.PlayClip(hitSfx, transform.position, 1f, 1f, -1f, 16f, -1f);
        }

        // Flash emission (visual feedback)
        if (renderers != null && renderers.Length > 0)
        {
            StartCoroutine(FlashRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        // Set all renderers to flash color
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].GetPropertyBlock(propBlock);
            propBlock.SetColor(EmissionColor, hitFlashColor);
            renderers[i].SetPropertyBlock(propBlock);
        }

        yield return new WaitForSeconds(hitFlashDuration);

        // Restore original colors
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].GetPropertyBlock(propBlock);
            propBlock.SetColor(EmissionColor, originalColors[i][0]);
            renderers[i].SetPropertyBlock(propBlock);
        }
    }
}
