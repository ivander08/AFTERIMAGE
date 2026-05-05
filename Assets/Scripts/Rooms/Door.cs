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
    private Collider _brokenTrigger;

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

    public void Break()
    {
        if (isLocked || IsBroken) return;
        
        IsBroken = true;

        if (doorRenderer != null) doorRenderer.enabled = false;
        
        if (_col != null) 
        {
            _col.enabled = false; 

            _brokenTrigger = gameObject.AddComponent<BoxCollider>();
            _brokenTrigger.isTrigger = true;
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

        Debug.Log($"[Door] Lock: {DoorName}, wasBroken={IsBroken}, colEnabled={_col?.enabled}, brokenTriggerEnabled={_brokenTrigger?.enabled}");

        if (doorRenderer != null)
        {
            doorRenderer.enabled = true;
            doorMaterialInstance.color = Color.red;
        }

        if (_col != null) _col.enabled = true;

        // FIX: Don't disable _brokenTrigger — keeping it always-on prevents
        // Unity from re-firing OnTriggerEnter when the room unlocks later.
        // The DoorDashZone now checks IsLocked() to reject locked-door triggers.
    }

    public void Unlock()
    {
        isLocked = false;

        if (IsBroken)
        {
            if (doorRenderer != null) doorRenderer.enabled = false;
            if (_col != null) _col.enabled = false;
            
            // FIX: Don't re-enable _brokenTrigger here — it stays always-on
            // to prevent Unity from re-firing OnTriggerEnter when unlocking.
            // DoorDashZone.OnTriggerEnter now checks IsLocked() to filter.
        }
        else
        {
            if (doorRenderer != null)
            {
                doorRenderer.enabled = true;
                doorMaterialInstance.color = originalColor;
            }
            
            if (_col != null) _col.enabled = true;
        }
    }

    public bool IsLocked() => isLocked;
}