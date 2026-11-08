using UnityEngine;
using Mirror;

/// <summary>
/// Networked player component for multiplayer support.
/// Original game is single-player; this adds basic LAN multiplayer.
/// </summary>
public class NetworkedPlayer : NetworkBehaviour
{
    [SyncVar] public string PlayerName;
    [SyncVar] public int PlayerID;
    [SyncVar] public int Health = 100;
    [SyncVar] public bool IsRake = false;

    public bool IsLocalPlayer { get; private set; } = false;

    private FPSController _fpsController;
    private Animator _animator;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        IsLocalPlayer = true;

        _fpsController = GetComponent<FPSController>();
        _animator = GetComponentInChildren<Animator>();

        // Enable local player components
        if (_fpsController != null) _fpsController.enabled = true;
        if (_animator != null) _animator.enabled = true;

        // Set up camera
        var cam = GetComponentInChildren<Camera>();
        if (cam != null) cam.enabled = true;

        var audioListener = GetComponentInChildren<AudioListener>();
        if (audioListener != null) audioListener.enabled = true;

        // Register with game manager
        GameManager.Instance?.RegisterLocalPlayer(this);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!isLocalPlayer)
        {
            SetupRemotePlayer();
        }
    }

    private void SetupRemotePlayer()
    {
        // Disable input components
        var fpsController = GetComponent<FPSController>();
        if (fpsController != null) fpsController.enabled = false;

        var playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null) playerMovement.enabled = false;

        var cameraMouseLook = GetComponentInChildren<CameraMouseLook>();
        if (cameraMouseLook != null) cameraMouseLook.enabled = false;

        var fpsCamera = GetComponentInChildren<FPSCamera>();
        if (fpsCamera != null) fpsCamera.enabled = false;

        var inventory = GetComponent<Inventory>();
        if (inventory != null) inventory.enabled = false;

        var weaponManager = GetComponent<WeaponManager>();
        if (weaponManager != null) weaponManager.enabled = false;

        // Disable cameras (both main and weapon)
        var cameras = GetComponentsInChildren<Camera>(true);
        foreach (var cam in cameras) cam.enabled = false;

        // Disable audio listener
        var audioListener = GetComponentInChildren<AudioListener>();
        if (audioListener != null) audioListener.enabled = false;

        // Disable weapon view models
        if (weaponManager != null)
        {
            foreach (var weapon in weaponManager.PlayerWeapons)
            {
                weapon.gameObject.SetActive(false);
            }
        }

        // Keep animator enabled for animation sync
        var animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.enabled = true;

        // Add visual representation for remote player
        CreateRemoteVisual();
    }

    private void CreateRemoteVisual()
    {
        // Create a simple capsule for remote player visualization
        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "RemotePlayerVisual";
        capsule.transform.SetParent(transform, false);
        capsule.transform.localPosition = Vector3.up;
        Destroy(capsule.GetComponent<Collider>());
        
        // Set layer to remote player
        int remoteLayer = LayerMask.NameToLayer("RemotePlayer");
        if (remoteLayer >= 0) capsule.layer = remoteLayer;
    }

    public void SetAsLocalPlayer()
    {
        IsLocalPlayer = true;
        enabled = true;
    }

    public void TakeDamage(int amount, string damageType)
    {
        if (!isServer) return;

        Health -= amount;
        Health = Mathf.Max(0, Health);

        RpcOnDamageTaken(amount, damageType);

        if (Health <= 0)
        {
            RpcOnDeath();
        }
    }

    [ClientRpc]
    private void RpcOnDamageTaken(int amount, string damageType)
    {
        // Play damage effects on all clients
        var anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("Hit");
        }
    }

    [ClientRpc]
    private void RpcOnDeath()
    {
        // Handle death on all clients
        var anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetBool("Dead", true);
        }

        // Disable collider
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    [Command]
    public void CmdFireWeapon(int weaponID, Vector3 origin, Vector3 direction)
    {
        // Server-authoritative shooting
        RpcFireWeapon(weaponID, origin, direction);
    }

    [ClientRpc]
    private void RpcFireWeapon(int weaponID, Vector3 origin, Vector3 direction)
    {
        // Visual effects on all clients
        if (IsLocalPlayer) return; // Local player handles own effects

        // Spawn muzzle flash, play sound, etc.
    }

    private void Update()
    {
        if (!IsLocalPlayer) return;

        // Send position/rotation to server
        if (isClient)
        {
            CmdUpdateTransform(transform.position, transform.rotation);
        }
    }

    [Command]
    private void CmdUpdateTransform(Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;
    }
}