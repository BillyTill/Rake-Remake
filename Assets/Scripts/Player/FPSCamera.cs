using UnityEngine;

/// <summary>
/// Handles player interaction raycast (E key to interact with objects).
/// Attached to the main camera.
/// </summary>
public class FPSCamera : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _interactionDistance = 3f;
    [SerializeField] private LayerMask _interactionLayers;

    private DynamicObject _currentTarget;

    private void Update()
    {
        // Raycast for interaction
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, _interactionDistance, _interactionLayers))
        {
            var dynamicObject = hit.collider.GetComponent<DynamicObject>();
            if (dynamicObject != null)
            {
                _currentTarget = dynamicObject;

                // Show interaction hint
                if (InformationUI.Instance != null)
                {
                    InformationUI.Instance.DrawItemInfo(dynamicObject.GetInteractionText(), true);
                }

                // Interact on E key
                if (Input.GetButtonDown("Use"))
                {
                    dynamicObject.Action();
                }
            }
            else
            {
                ClearTarget();
            }
        }
        else
        {
            ClearTarget();
        }
    }

    private void ClearTarget()
    {
        if (_currentTarget != null)
        {
            _currentTarget = null;
            if (InformationUI.Instance != null)
            {
                InformationUI.Instance.DrawItemInfo("", false);
            }
        }
    }
}