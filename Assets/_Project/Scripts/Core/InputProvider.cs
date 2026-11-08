using System;
using UnityEngine;

namespace Lake.Core
{
    /// <summary>
    /// Singleton. Все потребители подписываются на события в OnEnable.
    /// Никто не читает Input.* напрямую — гарантирует консистентность.
    /// </summary>
    public class InputProvider : MonoBehaviour
    {
        public static InputProvider Instance { get; private set; }

        [Header("Keys")]
        public KeyCode interactKey = KeyCode.E;
        public KeyCode reloadKey = KeyCode.R;
        public KeyCode dropKey = KeyCode.G;
        public KeyCode flashlightKey = KeyCode.F;
        public KeyCode bodycamToggleKey = KeyCode.B;
        public KeyCode rifleSlotKey = KeyCode.Alpha1;
        public KeyCode shotgunSlotKey = KeyCode.Alpha2;

        // Continuous
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool Sprint { get; private set; }
        public bool Crouch { get; private set; }
        public bool ADS { get; private set; }
        public bool FireHeld { get; private set; }

        // Events
        public event Action InteractPressed;
        public event Action ReloadPressed;
        public event Action DropPressed;
        public event Action FlashlightPressed;
        public event Action BodyCamTogglePressed;
        public event Action EscapePressed;
        public event Action<int> WeaponSlotPressed;
        public event Action FireStarted;
        public event Action FireStopped;
        public event Action ADSStarted;
        public event Action ADSEnded;
        public event Action TabPressed;

        bool _firePrev, _adsPrev;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LockCursor();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            LookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            Sprint = Input.GetKey(KeyCode.LeftShift);
            Crouch = Input.GetKey(KeyCode.LeftControl);

            if (Input.GetKeyDown(interactKey))      InteractPressed?.Invoke();
            if (Input.GetKeyDown(reloadKey))        ReloadPressed?.Invoke();
            if (Input.GetKeyDown(dropKey))          DropPressed?.Invoke();
            if (Input.GetKeyDown(flashlightKey))    FlashlightPressed?.Invoke();
            if (Input.GetKeyDown(bodycamToggleKey)) BodyCamTogglePressed?.Invoke();
            if (Input.GetKeyDown(rifleSlotKey))     WeaponSlotPressed?.Invoke(0);
            if (Input.GetKeyDown(shotgunSlotKey))   WeaponSlotPressed?.Invoke(1);

            FireHeld = Input.GetMouseButton(0);
            if (FireHeld && !_firePrev) FireStarted?.Invoke();
            if (!FireHeld && _firePrev) FireStopped?.Invoke();
            _firePrev = FireHeld;

            ADS = Input.GetMouseButton(1);
            if (ADS && !_adsPrev) ADSStarted?.Invoke();
            if (!ADS && _adsPrev) ADSEnded?.Invoke();
            _adsPrev = ADS;

            // Escape → unlock, LMB → lock
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                EscapePressed?.Invoke();
            }
            else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor();
            }

            if (Input.GetKeyDown(KeyCode.Tab))
                TabPressed?.Invoke();
        }

        void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}