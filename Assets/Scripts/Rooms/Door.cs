using UnityEngine;

public class Door : MonoBehaviour 
{
    public string doorName;

    private bool isLocked;
    private Room room;
    private Renderer doorRenderer;
    private Material doorMaterial;
    private Color originalColor;
    private Collider _col;

    private void Start()
    {
        doorRenderer = GetComponent<Renderer>();
        _col = GetComponent<Collider>();

        if (doorRenderer != null)
        {
            doorMaterial = doorRenderer.material;
            originalColor = doorMaterial.color;
        }
    }

    public void SetRoom(Room targetRoom) => room = targetRoom;

    public void Break()
    {
        if (isLocked) return;

        if (doorRenderer != null) doorRenderer.enabled = false;
        if (_col != null) _col.enabled = false;
    }

    public void Lock()
    {
        isLocked = true;
        if (doorRenderer != null) 
        {
            doorRenderer.enabled = true;
            doorMaterial.color = Color.red;
        }
        if (_col != null) _col.enabled = true;
    }

    public void Unlock()
    {
        isLocked = false;
        if (doorRenderer != null) 
        {
            doorRenderer.enabled = true;
            doorMaterial.color = originalColor;
        }
        if (_col != null) _col.enabled = true;
    }

    public bool IsLocked() => isLocked;
}
