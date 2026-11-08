using UnityEngine;

/// <summary>
/// Base class for camera mounting points.
/// Both stationary cameras and animal cameras inherit from this.
/// </summary>
public class RECCameras_obj : DynamicObject
{
    /// <summary>
    /// Called when player interacts to place a camera here.
    /// </summary>
    public virtual void SetupCamera() { }

    /// <summary>
    /// Check if a camera can be placed here.
    /// </summary>
    /// <returns>True if placement is allowed</returns>
    public virtual bool CheckSetup()
    {
        return false;
    }
}