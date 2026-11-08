using UnityEngine;

namespace Lake.Weapons.VFX
{
    public class ShellEjector : MonoBehaviour
    {
        [SerializeField] Transform ejectionPort;
        [SerializeField] GameObject shellPrefab;
        [SerializeField] Vector3 localEjectDirection = new Vector3(1f, 0.5f, -0.2f);
        [SerializeField] float ejectForce = 2f;

        public void Eject()
        {
            if (shellPrefab == null || ejectionPort == null) return;
            var shell = Object.Instantiate(shellPrefab, ejectionPort.position, ejectionPort.rotation);
            var rb = shell.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 dir = ejectionPort.TransformDirection(localEjectDirection);
                rb.AddForce(dir * ejectForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 0.5f, ForceMode.Impulse);
            }
            Object.Destroy(shell, 5f);
        }
    }
}