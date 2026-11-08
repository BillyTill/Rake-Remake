using System;
using UnityEngine;
using Lake.Core;
using Lake.Aim;

namespace Lake.Weapons
{
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] Transform weaponHolder;
        [SerializeField] Transform aimPivotTransform;
        [SerializeField] RecoilController recoilController;

        readonly GameObject[] _instances = new GameObject[2];
        readonly WeaponRuntime[] _runtimes = new WeaponRuntime[2];
        readonly WeaponDataSO[] _data = new WeaponDataSO[2];

        int _current = -1;
        InputProvider _input;

        public WeaponRuntime Current => _current >= 0 ? _runtimes[_current] : null;
        public WeaponRuntime[] AllWeapons => _runtimes;
        public event Action<int, WeaponRuntime> OnSlotChanged;

        void Start()
        {
            _input = InputProvider.Instance;
            _input.WeaponSlotPressed += OnSlotPressed;
            _input.DropPressed += DropCurrent;
        }

        void OnDestroy()
        {
            if (_input == null) return;
            _input.WeaponSlotPressed -= OnSlotPressed;
            _input.DropPressed -= DropCurrent;
        }

        void OnSlotPressed(int slot)
        {
            if (slot < 0 || slot >= 2 || _instances[slot] == null) return;
            SwitchTo(slot);
        }

        public bool TryAddWeapon(WeaponType type, GameObject prefab, WeaponDataSO data)
        {
            int slot = (int)type;
            if (_instances[slot] != null) return false;

            var inst = Instantiate(prefab, weaponHolder);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;

            var rt = inst.GetComponent<WeaponRuntime>();
            if (rt == null) { Destroy(inst); return false; }

            // Assign runtime references
            rt.aimPivot = FindFirstObjectByType<AimPivot>();
            rt.recoil = recoilController ?? FindFirstObjectByType<RecoilController>();

            _instances[slot] = inst;
            _runtimes[slot] = rt;
            _data[slot] = data;
            inst.SetActive(false);
            rt.Initialize();
            SwitchTo(slot);
            return true;
        }

        void SwitchTo(int slot)
        {
            if (slot == _current || _instances[slot] == null) return;
            if (_current >= 0 && _instances[_current] != null) _instances[_current].SetActive(false);
            _current = slot;
            _instances[slot].SetActive(true);
            OnSlotChanged?.Invoke(slot, _runtimes[slot]);
        }

        public void DropCurrent()
        {
            if (_current < 0) return;
            var inst = _instances[_current];
            if (inst == null) return;

            inst.transform.SetParent(null);
            var rb = inst.AddComponent<Rigidbody>();
            rb.mass = 2f;
            inst.AddComponent<BoxCollider>().size = new Vector3(0.3f, 0.15f, 1f);
            inst.SetActive(true);
            rb.AddForce(aimPivotTransform.forward * 5f + Vector3.up * 1.5f, ForceMode.Impulse);

            var pickup = inst.AddComponent<WeaponPickup>();
            pickup.Setup((WeaponType)_current, _data[_current]);

            _instances[_current] = null;
            _runtimes[_current] = null;
            _data[_current] = null;
            _current = -1;

            for (int i = 0; i < 2; i++)
                if (_instances[i] != null) { SwitchTo(i); return; }
        }
    }
}