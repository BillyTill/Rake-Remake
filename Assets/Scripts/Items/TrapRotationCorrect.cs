using UnityEngine;

/// <summary>
/// Corrects trap rotation to align with ground normal.
/// </summary>
public class TrapRotationCorrect : MonoBehaviour
{
    [SerializeField] private float _correctionSpeed = 5f;

    private void Start()
    {
        // Align with ground on spawn
        AlignToGround();
    }

    private void Update()
    {
        // Continuously correct (for moving platforms, etc)
        AlignToGround();
    }

    private void AlignToGround()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 2f))
        {
            Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _correctionSpeed * Time.deltaTime);
        }
    }
}