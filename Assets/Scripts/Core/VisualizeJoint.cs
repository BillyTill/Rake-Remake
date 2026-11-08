using UnityEngine;

/// <summary>
/// Debug visualization for joints/connectors.
/// </summary>
public class VisualizeJoint : MonoBehaviour
{
    [SerializeField] private Color _jointColor = Color.green;
    [SerializeField] private float _sphereRadius = 0.1f;
    [SerializeField] private bool _drawConnections = true;
    [SerializeField] private Transform[] _connectedTransforms;

    private void OnDrawGizmos()
    {
        Gizmos.color = _jointColor;
        Gizmos.DrawWireSphere(transform.position, _sphereRadius);

        if (_drawConnections && _connectedTransforms != null)
        {
            foreach (var t in _connectedTransforms)
            {
                if (t != null)
                {
                    Gizmos.DrawLine(transform.position, t.position);
                }
            }
        }
    }
}