using UnityEngine;

/// <summary>
/// Base class for interactive objects in the world.
/// Handles player interaction via the FPSCamera raycast system.
/// </summary>
public class DynamicObject : MonoBehaviour
{
    /// <summary>
    /// Called when player interacts with this object (presses E).
    /// </summary>
    public virtual void Action()
    {
        // Override in derived classes
    }

    /// <summary>
    /// Optional: Display interaction hint text.
    /// </summary>
    public virtual string GetInteractionText()
    {
        return "Press 'E' to interact";
    }
}