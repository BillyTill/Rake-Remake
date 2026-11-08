using UnityEngine;

/// <summary>
/// Animal head vision detection.
/// Determines if animal can see the Rake or player.
/// </summary>
public class AnimalHead : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _viewDistance = 20f;
    [SerializeField] private float _viewAngle = 90f;
    [SerializeField] private LayerMask _viewLayers;
    [SerializeField] private Transform _eyeTransform;

    public bool IsSeen { get; private set; } = false;

    private void Update()
    {
        CheckVision();
    }

    private void CheckVision()
    {
        IsSeen = false;

        if (_eyeTransform == null) return;

        // Find Rake
        var rake = GameObject.FindGameObjectWithTag("Rake");
        if (rake != null)
        {
            Vector3 direction = (rake.transform.position - _eyeTransform.position).normalized;
            float angle = Vector3.Angle(_eyeTransform.forward, direction);
            float distance = Vector3.Distance(_eyeTransform.position, rake.transform.position);

            if (angle < _viewAngle * 0.5f && distance < _viewDistance)
            {
                Ray ray = new Ray(_eyeTransform.position, direction);
                if (Physics.Raycast(ray, distance, _viewLayers))
                {
                    IsSeen = true;
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_eyeTransform == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_eyeTransform.position, _viewDistance);
        
        Vector3 leftDir = Quaternion.Euler(0, -_viewAngle * 0.5f, 0) * _eyeTransform.forward;
        Vector3 rightDir = Quaternion.Euler(0, _viewAngle * 0.5f, 0) * _eyeTransform.forward;
        
        Gizmos.DrawRay(_eyeTransform.position, leftDir * _viewDistance);
        Gizmos.DrawRay(_eyeTransform.position, rightDir * _viewDistance);
    }
}