using UnityEngine;

/// <summary>
/// Rotates object continuously (for props, effects, etc).
/// </summary>
public class IORotator : MonoBehaviour
{
    [SerializeField] private Vector3 _rotationSpeed = new Vector3(0, 90, 0);
    [SerializeField] private Space _space = Space.Self;

    private void Update()
    {
        transform.Rotate(_rotationSpeed * Time.deltaTime, _space);
    }
}