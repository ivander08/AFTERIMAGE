using UnityEngine;

public class RepulsorUtility : BaseUtility
{
    [Header("Gameplay")]
    public float radius = 5f;
    public float pushForce = 40f;
    public float stunDuration = 1.0f;
    public LayerMask enemyLayer;

    [Header("VFX")]
    public GameObject activationVfxPrefab;

    [Header("Audio")]
    public AudioClip activationSfx;
    public float activationSfxVolume = 1.5f;

    public override string UtilityName => "Repulsor";

    protected override void ExecuteUtility(Transform origin)
    {
        // ── VFX ──
        if (activationVfxPrefab != null)
        {
            Instantiate(activationVfxPrefab, origin.position, Quaternion.identity);
        }

        // ── Camera Shake ──
        CameraShakeService.Shake(0.75f);

        // ── SFX (2D — UI channel) ──
        if (activationSfx != null)
        {
            AudioService.PlayClip2D(activationSfx, volume: 0.6f, pitch: 1f);
        }

        // ── Physics ──
        Collider[] colliders = Physics.OverlapSphere(origin.position, radius, enemyLayer);
        bool affectedEnemy = false;

        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out EnemyBase enemy))
            {
                // Only affect enemies in the same room as the player
                if (enemy.MyRoom != RoomManager.Instance.CurrentRoom) continue;

                affectedEnemy = true;
                Vector3 direction = (enemy.transform.position - origin.position).normalized;
                float adjustedForce = pushForce;
                
                if (enemy is EnemyPhalanx phalanx && phalanx.HasShield())
                {
                    adjustedForce = phalanx.GetAdjustedRepulsorForce(pushForce);
                    phalanx.BreakShield();
                }
                
                enemy.Knockback(direction, adjustedForce, stunDuration);
            }
        }

        if (affectedEnemy && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddUtilityScore(UtilityName);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
