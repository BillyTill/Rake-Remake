using UnityEngine;

/// <summary>
/// Changes player footstep sound based on surface.
/// </summary>
public class FootstepChanger : MonoBehaviour
{
    [SerializeField] private int _surfaceType = 0; // 0=grass, 1=dirt, 2=wood, 3=stone, etc.

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var movement = other.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.SetFootstepSound(_surfaceType);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var movement = other.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.SetFootstepSound(0); // Default to grass
            }
        }
    }
}