using UnityEngine;

public enum DoorMaterial
{
    Wood,
    Metal
}

public class Door : MonoBehaviour 
{
    public string doorName;
    public DoorMaterial doorMaterial = DoorMaterial.Wood;

    [Header("Effects")]
    public AudioClip woodBreakSound;
    public AudioClip metalBreakSound;
    public GameObject woodBreakVfxPrefab;
    public GameObject metalBreakVfxPrefab;

    private bool isLocked;
    private Room room;
    private Renderer doorRenderer;
    private Material doorMaterialInstance;
    private Color originalColor;
    private Collider _col;

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

    public void SetRoom(Room targetRoom) => room = targetRoom;

    public void Break()
    {
        if (isLocked) return;

        if (doorRenderer != null) doorRenderer.enabled = false;
        if (_col != null) _col.enabled = false;

        PlayBreakEffects();
    }

    private void PlayBreakEffects()
    {
        AudioClip clipToPlay = doorMaterial == DoorMaterial.Wood ? woodBreakSound : metalBreakSound;
        if (clipToPlay != null)
        {
            AudioSource.PlayClipAtPoint(clipToPlay, transform.position);
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
        if (doorRenderer != null) 
        {
            doorRenderer.enabled = true;
            doorMaterialInstance.color = Color.red;
        }
        if (_col != null) _col.enabled = true;
    }

    public void Unlock()
    {
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