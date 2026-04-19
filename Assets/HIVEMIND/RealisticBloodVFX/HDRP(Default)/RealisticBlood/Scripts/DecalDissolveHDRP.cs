using System.Collections.Generic;
using UnityEngine;

public class HDRPDecalDissolver : MonoBehaviour
{
    [Header("Dissolve Timing")]
    public float minDelay = 0.5f;           // Min time before starting dissolve
    public float maxDelay = 1.5f;           // Max time before starting dissolve
    public float dissolveDuration = 1.5f;   // Time it takes to fully dissolve

    private float timer = 0f;
    private float dissolveDelay;
    private bool startedDissolve = false;

    private List<Material> decalMaterials = new List<Material>();

    void Start()
    {
        // Pick a random dissolve delay between min and max
        dissolveDelay = Random.Range(minDelay, maxDelay);

        CollectDecalMaterials();

        if (decalMaterials.Count == 0)
        {
            Debug.LogError("No supported DecalProjector components with a _Dissolve material were found.");
        }
    }

    private void CollectDecalMaterials()
    {
#if UNITY_RENDER_PIPELINE_HIGH_DEFINITION
        var hdrpProjectors = GetComponentsInChildren<UnityEngine.Rendering.HighDefinition.DecalProjector>();
        foreach (var projector in hdrpProjectors)
        {
            TryCloneAndRegisterMaterial(projector.material, cloned => projector.material = cloned);
        }
#endif

#if UNITY_RENDER_PIPELINE_UNIVERSAL
        var urpProjectors = GetComponentsInChildren<UnityEngine.Rendering.Universal.DecalProjector>();
        foreach (var projector in urpProjectors)//
        {
            TryCloneAndRegisterMaterial(projector.material, cloned => projector.material = cloned);
        }
#endif
    }

    private void TryCloneAndRegisterMaterial(Material originalMat, System.Action<Material> assignMaterial)
    {
        if (originalMat == null)
        {
            Debug.LogWarning("Decal Projector has no material.");
            return;
        }

        if (!originalMat.HasProperty("_Dissolve"))
        {
            Debug.LogWarning($"Material '{originalMat.name}' does not have a '_Dissolve' property.");
            return;
        }

        // Clone the material to avoid modifying shared instances.
        Material matInstance = Instantiate(originalMat);
        matInstance.SetFloat("_Dissolve", 0f);
        assignMaterial(matInstance);
        decalMaterials.Add(matInstance);
    }

    void Update()
    {
        if (decalMaterials.Count == 0) return;

        timer += Time.deltaTime;

        if (!startedDissolve && timer >= dissolveDelay)
        {
            startedDissolve = true;
            timer = 0f;
        }

        if (startedDissolve)
        {
            float dissolveValue = Mathf.Clamp01(timer / dissolveDuration);

            foreach (Material mat in decalMaterials)
            {
                if (mat != null)
                    mat.SetFloat("_Dissolve", dissolveValue);
            }

            if (dissolveValue >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
