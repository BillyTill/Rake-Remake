using UnityEngine;

/// <summary>
/// Camera angle constraint for camera computer view.
/// </summary>
public class CameraAngle : MonoBehaviour
{
    [SerializeField] private float _minY = -45f;
    [SerializeField] private float _maxY = 45f;
    [SerializeField] private float _minX = -30f;
    [SerializeField] private float _maxX = 30f;

    private void LateUpdate()
    {
        Vector3 euler = transform.localEulerAngles;
        
        // Normalize angles
        if (euler.x > 180f) euler.x -= 360f;
        if (euler.y > 180f) euler.y -= 360f;
        if (euler.z > 180f) euler.z -= 360f;

        // Clamp
        euler.x = Mathf.Clamp(euler.x, _minX, _maxX);
        euler.y = Mathf.Clamp(euler.y, _minY, _maxY);
        euler.z = 0f;

        transform.localEulerAngles = euler;
    }
}