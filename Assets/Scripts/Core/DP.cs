using UnityEngine;

/// <summary>
/// Detection Point - used for AI vision/hearing checks.
/// </summary>
public class DP : MonoBehaviour
{
    [SerializeField] private float _radius = 5f;
    [SerializeField] private LayerMask _detectionLayers;
    [SerializeField] private bool _drawGizmos = true;

    public bool CheckDetection(out Collider[] hits)
    {
        hits = Physics.OverlapSphere(transform.position, _radius, _detectionLayers);
        return hits.Length > 0;
    }

    public bool CheckDetectionTag(string tag)
    {
        var hits = Physics.OverlapSphere(transform.position, _radius, _detectionLayers);
        foreach (var hit in hits)
        {
            if (hit.CompareTag(tag))
                return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}