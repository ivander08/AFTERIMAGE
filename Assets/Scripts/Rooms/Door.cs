using UnityEngine;

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

            BoxCollider boxTrigger = gameObject.AddComponent<BoxCollider>();
            boxTrigger.isTrigger = true;
        }

        PlayBreakEffects();
    }

    private void PlayBreakEffects()
    {
        AudioClip clipToPlay = doorMaterial == DoorMaterial.Wood ? woodBreakSound : metalBreakSound;
        if (clipToPlay != null)
        {
            AudioService.PlayClip(clipToPlay, transform.position, 1f, 1f);
        }

        GameObject vfxToSpawn = doorMaterial == DoorMaterial.Wood ? woodBreakVfxPrefab : metalBreakVfxPrefab;
        if (vfxToSpawn != null)
        {
            Instantiate(vfxToSpawn, transform.position, Quaternion.identity);
        }
    }

    public void Lock()
    {
        if (IsBroken) return; // Don't lock a broken door

        isLocked = true;
        if (doorRenderer != null) 
        {
            doorRenderer.enabled = true;
            doorMaterialInstance.color = Color.red;
        }
        if (_col != null) _col.enabled = true;
    }

    public void Unlock()
    {
        if (IsBroken) return; // Don't magically rebuild the door visually

        isLocked = false;
        if (doorRenderer != null) 
        {
            doorRenderer.enabled = true;
            doorMaterialInstance.color = originalColor;
        }
        if (_col != null) _col.enabled = true;
    }

    public bool IsLocked() => isLocked;
}