using UnityEngine;

/// <summary>
/// Door that can be locked/unlocked and broken by player dash.
/// </summary>

public enum DoorMaterial
{
    Wood,
    Metal
}

public class Door : MonoBehaviour 
{
    public string DoorName => gameObject.name;
    public DoorMaterial doorMaterial = DoorMaterial.Wood;

    public Room roomA;
    public Room roomB;

    [Header("Effects")]
    public AudioClip woodBreakSound;
    public AudioClip metalBreakSound;
    public GameObject woodBreakVfxPrefab;
    public GameObject metalBreakVfxPrefab;

    private bool isLocked;
    public bool IsBroken { get; private set; }
    
    private Renderer doorRenderer;
    private Material doorMaterialInstance;
    private Color originalColor;
    private Collider _col;

    private bool _isOverrideLocked = false;

    private void Awake()
    {
        if (roomA != null) roomA.RegisterDoor(this);
        if (roomB != null) roomB.RegisterDoor(this);
    }

    private void Start()
    {
        doorRenderer = GetComponent<Renderer>();
        _col = GetComponent<Collider>();

        if (doorRenderer != null)
        {
            doorMaterialInstance = doorRenderer.material;
            originalColor = doorMaterialInstance.color;
        }
    }

    public void SetOverrideLock(bool state)
    {
        _isOverrideLocked = state;
        if (state)
        {
            Lock();
        }
    }

    public void Break()
    {
        if (isLocked || IsBroken) return;
        
        IsBroken = true;

        if (doorRenderer != null) doorRenderer.enabled = false;
        
        // Reuse the existing collider as a trigger instead of creating a new tiny BoxCollider.
        // This guarantees the trigger matches the door mesh size exactly.
        if (_col != null)
        {
            _col.isTrigger = true;
        }

        PlayBreakEffects();
    }

    private void PlayBreakEffects()
    {
        AudioClip clipToPlay = doorMaterial == DoorMaterial.Wood ? woodBreakSound : metalBreakSound;
        if (clipToPlay != null)
        {
            AudioService.PlayClip2D(clipToPlay, 0.2f);
        }

        GameObject vfxToSpawn = doorMaterial == DoorMaterial.Wood ? woodBreakVfxPrefab : metalBreakVfxPrefab;
        if (vfxToSpawn != null)
        {
            Instantiate(vfxToSpawn, transform.position, Quaternion.identity);
        }
    }

    public void Lock()
    {
        isLocked = true;

        // Trap mechanic: broken doors become solid + red during combat.
        // When the room is cleared, Unlock() opens them again.
        if (doorRenderer != null)
        {
            doorRenderer.enabled = true;
            doorMaterialInstance.color = Color.red;
        }

        if (_col != null)
        {
            _col.enabled = true;
            _col.isTrigger = false;
        }
    }

    public void Unlock()
    {
        // ADDED: Prevent adjacent rooms from unlocking this door if it's strictly overridden
        if (_isOverrideLocked) return;

        isLocked = false;

        if (IsBroken)
        {
            if (doorRenderer != null) doorRenderer.enabled = false;
            // Keep the collider enabled as a trigger so the player can walk through.
            if (_col != null)
            {
                _col.enabled = true;
                _col.isTrigger = true;
            }
        }
        else
        {
            if (doorRenderer != null)
            {
                doorRenderer.enabled = true;
                doorMaterialInstance.color = originalColor;
            }
            
            if (_col != null)
            {
                _col.enabled = true;
                _col.isTrigger = false;
            }
        }
    }

    /// <summary>
    /// Immediately set the locked flag without changing visuals/colliders.
    /// Use this before DelayedLockRoom() to prevent OnTriggerEnter ghost transitions
    /// during the 0.15s window before the door becomes physically solid.
    /// </summary>
    public void MarkLocked() => isLocked = true;

    public bool IsLocked() => isLocked;
}