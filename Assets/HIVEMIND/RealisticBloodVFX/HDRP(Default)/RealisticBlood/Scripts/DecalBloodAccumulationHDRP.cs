using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class BloodAccumulationDecal : MonoBehaviour
{
    [Header("Decal Settings")]
    public GameObject bigDecalPrefab;
    public float spawnOffset = 0.01f;
    public Vector3 decalOffset = Vector3.zero;
    public float gridSize = 1.0f;
    public float timeThreshold = 2.5f;
    public float respawnCooldown = 10f;
    public float inactivityResetTime = 1.5f;

    [Header("Fade Settings")]
    public float fadeInDuration = 0.5f;
    public float visibleDuration = 6f;
    public float fadeOutDuration = 1.5f;

    private ParticleSystem ps;
    private List<ParticleCollisionEvent> collisionEvents;

    private Dictionary<Vector3, float> hitStartTimes = new();    // Time when particle started hitting this spot
    private Dictionary<Vector3, float> cooldowns = new();         // Cooldown timers
    private Dictionary<Vector3, float> lastHitTimes = new();      // Last time this spot was hit

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        if (ps == null)
            Debug.LogError("❌ No ParticleSystem found on this GameObject.");

        collisionEvents = new List<ParticleCollisionEvent>();
    }

    void Update()
    {
        float now = Time.time;

        // Clean up hitStartTimes if inactive too long
        List<Vector3> keys = new(hitStartTimes.Keys);
        foreach (var key in keys)
        {
            if (!lastHitTimes.ContainsKey(key) || now - lastHitTimes[key] > inactivityResetTime)
                hitStartTimes.Remove(key);
        }

        // Update and remove expired cooldowns
        List<Vector3> cooldownKeys = new(cooldowns.Keys);
        foreach (var key in cooldownKeys)
        {
            cooldowns[key] -= Time.deltaTime;
            if (cooldowns[key] <= 0f)
                cooldowns.Remove(key);
        }
    }

    void OnParticleCollision(GameObject other)
    {
        if (ps == null || collisionEvents == null)
            return;

        int count = ps.GetCollisionEvents(other, collisionEvents);
        if (count == 0) return;

        float now = Time.time;
        HashSet<Vector3> updatedThisFrame = new();

        for (int i = 0; i < count; i++)
        {
            Vector3 hit = collisionEvents[i].intersection;
            Vector3 normal = collisionEvents[i].normal;
            Vector3 gridPoint = RoundToGrid(hit, gridSize);

            if (cooldowns.ContainsKey(gridPoint))
                continue;

            lastHitTimes[gridPoint] = now;

            if (!updatedThisFrame.Contains(gridPoint))
            {
                updatedThisFrame.Add(gridPoint);

                if (!hitStartTimes.ContainsKey(gridPoint))
                {
                    hitStartTimes[gridPoint] = now; // Start timer
                }

                float elapsed = now - hitStartTimes[gridPoint];
                if (elapsed >= timeThreshold)
                {
                    Vector3 finalPosition = hit + normal * spawnOffset + decalOffset;
                    SpawnBigDecal(finalPosition, normal);

                    hitStartTimes.Remove(gridPoint);
                    cooldowns[gridPoint] = respawnCooldown;
                }
            }
        }
    }

    Vector3 RoundToGrid(Vector3 pos, float size)
    {
        return new Vector3(
            Mathf.Round(pos.x / size) * size,
            Mathf.Round(pos.y / size) * size,
            Mathf.Round(pos.z / size) * size
        );
    }

    void SpawnBigDecal(Vector3 position, Vector3 normal)
    {
        if (bigDecalPrefab == null)
        {
            Debug.LogWarning("⚠️ bigDecalPrefab not assigned.");
            return;
        }

        // Create base rotation looking into the surface
        Quaternion baseRotation = Quaternion.LookRotation(-normal);

        // Add random rotation around the normal vector
        float randomAngle = Random.Range(0f, 360f);
        Quaternion randomTwist = Quaternion.AngleAxis(randomAngle, normal);

        // Combine both rotations
        Quaternion finalRotation = randomTwist * baseRotation;

        GameObject decal = Instantiate(bigDecalPrefab, position, finalRotation);

        if (!TrySetupDecalMaterial(decal, out Material instancedMat))
        {
            Destroy(decal);
            return;
        }

        instancedMat.SetFloat("_Dissolve", 1f);
        StartCoroutine(FadeDecal(instancedMat, decal));
    }

    bool TrySetupDecalMaterial(GameObject decal, out Material instancedMat)
    {
        instancedMat = null;

#if UNITY_RENDER_PIPELINE_HIGH_DEFINITION
        var hdrpProjector = decal.GetComponent<UnityEngine.Rendering.HighDefinition.DecalProjector>();
        if (hdrpProjector != null)
        {
            if (hdrpProjector.material == null || !hdrpProjector.material.HasProperty("_Dissolve"))
            {
                Debug.LogWarning("Decal material is missing or has no _Dissolve property.");
                return false;
            }

            instancedMat = new Material(hdrpProjector.material);
            hdrpProjector.material = instancedMat;
            return true;
        }
#endif

#if UNITY_RENDER_PIPELINE_UNIVERSAL
        var urpProjector = decal.GetComponent<UnityEngine.Rendering.Universal.DecalProjector>();
        if (urpProjector != null)
        {
            if (urpProjector.material == null || !urpProjector.material.HasProperty("_Dissolve"))
            {
                Debug.LogWarning("Decal material is missing or has no _Dissolve property.");
                return false;
            }

            instancedMat = new Material(urpProjector.material);
            urpProjector.material = instancedMat;
            return true;
        }
#endif

        Debug.LogWarning("Spawned decal has no supported DecalProjector (HDRP/URP).");
        return false;
    }

    IEnumerator FadeDecal(Material mat, GameObject decal)
    {
        float t = 0f;

        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            mat.SetFloat("_Dissolve", Mathf.Lerp(1f, 0f, t / fadeInDuration));
            yield return null;
        }

        yield return new WaitForSeconds(visibleDuration);

        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            mat.SetFloat("_Dissolve", Mathf.Lerp(0f, 1f, t / fadeOutDuration));
            yield return null;
        }

        Destroy(decal);
    }
}
