using UnityEngine;

public class Door : MonoBehaviour, IDamageable
{
    public string doorName;

    private bool isLocked;
    private Room room;
    private Renderer doorRenderer;
    private Material doorMaterial;
    private Color originalColor;

    private void Start()
    {
        doorRenderer = GetComponent<Renderer>();
        if (doorRenderer != null)
        {
            doorMaterial = doorRenderer.material;
            originalColor = doorMaterial.color;
        }
    }

    public void SetRoom(Room targetRoom)
    {
        room = targetRoom;
    }

    public void TakeDamage(int damage)
    {
        if (isLocked) return;

        if (doorMaterial != null)
        {
            doorMaterial.color = Color.white;
        }

        Break();
    }

    public void Lock()
    {
        isLocked = true;
        if (doorMaterial != null)
        {
            doorMaterial.color = Color.red;
        }
    }

    public void Unlock()
    {
        isLocked = false;
        if (doorMaterial != null)
        {
            doorMaterial.color = originalColor;
        }
    }

    public bool IsLocked()
    {
        return isLocked;
    }

    private void Break()
    {
        if (doorMaterial != null)
        {
            doorMaterial.color = Color.black;
        }
        
        GetComponent<Collider>().enabled = false;
        gameObject.SetActive(false);
    }
}
