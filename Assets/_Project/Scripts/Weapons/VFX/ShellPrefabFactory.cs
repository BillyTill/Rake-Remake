using UnityEngine;

namespace Lake.Weapons.VFX
{
    public static class ShellPrefabFactory
    {
        public static GameObject CreateShellPrefab(bool isShotgun)
        {
            var go = new GameObject(isShotgun ? "Shell_Shotgun" : "Shell_Rifle");
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.transform.SetParent(go.transform, false);
            // Цилиндр по умолчанию 2м высотой, сжимаем
            if (isShotgun)
            {
                cylinder.transform.localScale = new Vector3(0.025f, 0.035f, 0.025f);
                cylinder.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            else
            {
                cylinder.transform.localScale = new Vector3(0.01f, 0.025f, 0.01f);
                cylinder.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            Object.Destroy(cylinder.GetComponent<Collider>());

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.01f;
            var col = go.AddComponent<CapsuleCollider>();
            col.radius = 0.02f;
            col.height = 0.08f;
            col.isTrigger = false;

            go.SetActive(false);
            return go;
        }
    }
}