using UnityEngine;

/// <summary>
/// Flashlight trigger - toggles flashlight when player enters specific zones.
/// </summary>
public class Flashlight_trigger : MonoBehaviour
{
    [SerializeField] private bool _forceOn = true;
    [SerializeField] private bool _forceOff = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var fps = other.GetComponent<FPSController>();
            if (fps != null)
            {
                if (_forceOn)
                {
                    fps.SetLightState(true);
                }
                else if (_forceOff)
                {
                    fps.SetLightState(false);
                }
            }
        }
    }
}