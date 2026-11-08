using UnityEngine;

/// <summary>
/// Base class for all pickup items in the world.
/// </summary>
public class Item : MonoBehaviour
{
    public string ItemName;
    public int Count = 1;
    public int ID; // Unique ID for save/load

    [SerializeField] private AudioClip _pickupSound;

    /// <summary>
    /// Called when player picks up this item.
    /// Override in derived classes.
    /// </summary>
    public virtual void PickupItem()
    {
        if (_pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(_pickupSound, transform.position);
        }
        Destroy(gameObject);
    }
}