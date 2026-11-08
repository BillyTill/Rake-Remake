#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Lake.Player;
using Lake.Weapons;
using Lake.Health;
using Lake.BodyCam;
using Lake.Flashlight;
using Lake.UI;
using Lake.Outline;
using Lake.Weapons.VFX;

namespace Lake.Scene
{
    public class HorrorSceneBuilder : MonoBehaviour
    {
        [SerializeField] GameObject playerPrefab;
        [SerializeField] WeaponDataSO rifleData;
        [SerializeField] WeaponDataSO shotgunData;
        [SerializeField] GameObject riflePrefab;
        [SerializeField] GameObject shotgunPrefab;
        [SerializeField] BodyCamConfigSO bodyCamConfig;

        [ContextMenu("Generate Scene")]
        public void GenerateScene()
        {
            ClearAll();
            CreateGround(); CreateForest(); CreateCave(); CreateLighting();
            CreateBodyCamVolume(); CreatePlayer(); CreateWeaponPickups(); CreateTargets();
            CreateHud();
            CreateDebugUI();
        }

        void ClearAll()
        {
            foreach (var name in new[] { "Generated_Ground", "Generated_Forest", "Generated_Cave",
                "Generated_Lighting", "Generated_BodyCam", "Generated_Player", "Generated_Pickups", "Generated_Targets", "Generated_HUD" })
            {
                var obj = GameObject.Find(name);
                if (obj != null) DestroyImmediate(obj);
            }
        }

        void CreateGround()
        {
            var root = new GameObject("Generated_Ground");
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.SetParent(root.transform);
            plane.transform.localScale = new Vector3(5f, 1f, 5f);
            plane.tag = "Ground";
        }

        void CreateForest()
        {
            var root = new GameObject("Generated_Forest");
            var rng = new System.Random(42);
            for (int i = 0; i < 60; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = 10f + (float)rng.NextDouble() * 18f;
                Vector3 pos = new(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.transform.SetParent(root.transform);
                trunk.transform.position = pos + Vector3.up * 2f;
                trunk.transform.localScale = new Vector3(0.3f, 2f, 0.3f);
                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.transform.SetParent(root.transform);
                crown.transform.position = pos + Vector3.up * 5f;
                crown.transform.localScale = Vector3.one * 3f;
                DestroyImmediate(crown.GetComponent<Collider>());
            }
        }

        void CreateCave()
        {
            var root = new GameObject("Generated_Cave");
            Wall(root, new Vector3(-4f, 2.5f, 20f), new Vector3(0.5f, 5f, 30f));
            Wall(root, new Vector3(4f, 2.5f, 20f), new Vector3(0.5f, 5f, 30f));
            Wall(root, new Vector3(0f, 5f, 20f), new Vector3(8.5f, 0.5f, 30f));
        }

        void Wall(GameObject parent, Vector3 pos, Vector3 scale)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.transform.SetParent(parent.transform);
            w.transform.position = pos;
            w.transform.localScale = scale;
        }

        void CreateLighting()
        {
            var root = new GameObject("Generated_Lighting");
            var moon = new GameObject("Moonlight");
            moon.transform.SetParent(root.transform);
            var l = moon.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 0.15f;
            l.color = new Color(0.4f, 0.45f, 0.6f);
            l.shadows = LightShadows.Soft;
            moon.transform.rotation = Quaternion.Euler(15f, -45f, 0f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.03f;
            RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.03f);
        }

        void CreateBodyCamVolume()
        {
            var go = new GameObject("Generated_BodyCam");
            go.AddComponent<BodyCamController>();
            if (bodyCamConfig != null)
            {
                var so = new UnityEditor.SerializedObject(go.GetComponent<BodyCamController>());
                so.FindProperty("config").objectReferenceValue = bodyCamConfig;
                so.ApplyModifiedProperties();
            }
        }

        void CreatePlayer()
        {
            var root = new GameObject("Generated_Player");
            var sp = root.AddComponent<PlayerSpawner>();
            var so = new UnityEditor.SerializedObject(sp);
            so.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
            so.ApplyModifiedProperties();
        }

        void CreateDebugUI()
        {
            var builder = GameObject.Find("SceneBuilder");
            if (builder != null)
            {
                builder.AddComponent<Lake.Core.DebugUI>();
            }
        }

        void CreateWeaponPickups()
        {
            var root = new GameObject("Generated_Pickups");
            // Create weapon prefabs if not provided
            if (riflePrefab == null) riflePrefab = BuildWeaponPrefab(WeaponType.Rifle, rifleData);
            if (shotgunPrefab == null) shotgunPrefab = BuildWeaponPrefab(WeaponType.Shotgun, shotgunData);
            SpawnPickup(root, WeaponType.Rifle, rifleData, riflePrefab, new Vector3(-3f, 0.5f, -18f));
            SpawnPickup(root, WeaponType.Shotgun, shotgunData, shotgunPrefab, new Vector3(0f, 0.5f, -15f));
        }

        GameObject BuildWeaponPrefab(WeaponType type, WeaponDataSO data)
        {
            var root = new GameObject($"{type}_Prefab");
            root.SetActive(false);

            var sway = new GameObject("SwayContainer");
            sway.transform.SetParent(root.transform, false);
            sway.AddComponent<WeaponSwayV2>();

            var anim = new GameObject("AnimContainer");
            anim.transform.SetParent(sway.transform, false);
            var animComp = anim.AddComponent<WeaponAnimator>();
            var soAnim = new UnityEditor.SerializedObject(animComp);
            soAnim.FindProperty("hipPosition").vector3Value = data.hipPosition;
            soAnim.FindProperty("adsPosition").vector3Value = data.adsPosition;
            soAnim.FindProperty("weaponModel").objectReferenceValue = anim.transform; // Will be set to visual below
            soAnim.ApplyModifiedProperties();

            // Меш оружия
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(anim.transform, false);
            DestroyImmediate(visual.GetComponent<Collider>());
            if (type == WeaponType.Rifle)
            {
                visual.transform.localScale = new Vector3(0.06f, 0.08f, 0.8f);
                visual.transform.localPosition = new Vector3(0, 0, 0.3f);
            }
            else
            {
                visual.transform.localScale = new Vector3(0.08f, 0.1f, 1.0f);
                visual.transform.localPosition = new Vector3(0, 0, 0.35f);
            }

            // Assign weaponModel to the visual
            soAnim.FindProperty("weaponModel").objectReferenceValue = visual.transform;
            soAnim.ApplyModifiedProperties();

            // MuzzlePoint
            var muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(anim.transform, false);
            muzzle.transform.localPosition = new Vector3(0, 0, type == WeaponType.Rifle ? 0.75f : 0.9f);

            // EjectionPort
            var eject = new GameObject("EjectionPort");
            eject.transform.SetParent(anim.transform, false);
            eject.transform.localPosition = new Vector3(0.04f, 0.03f, 0.3f);

            // Компоненты
            var audio = root.AddComponent<WeaponAudio>();
            var shell = root.AddComponent<ShellEjector>();
            var soShell = new UnityEditor.SerializedObject(shell);
            soShell.FindProperty("ejectionPort").objectReferenceValue = eject.transform;
            soShell.FindProperty("shellPrefab").objectReferenceValue =
                ShellPrefabFactory.CreateShellPrefab(type == WeaponType.Shotgun);
            soShell.ApplyModifiedProperties();

            var rt = root.AddComponent<WeaponRuntime>();
            var so = new UnityEditor.SerializedObject(rt);
            so.FindProperty("data").objectReferenceValue = data;
            so.FindProperty("muzzlePoint").objectReferenceValue = muzzle.transform;
            so.FindProperty("animator").objectReferenceValue = animComp;
            so.FindProperty("shellEjector").objectReferenceValue = shell;
            so.FindProperty("audio").objectReferenceValue = audio;
            so.FindProperty("tracerPrefab").objectReferenceValue = null;
            so.ApplyModifiedProperties();

            return root;
        }

        void SpawnPickup(GameObject parent, WeaponType type, WeaponDataSO data, GameObject prefab, Vector3 pos)
        {
            var go = new GameObject($"{type}_Pickup");
            go.transform.SetParent(parent.transform);
            go.transform.position = pos;
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(go.transform);
            cube.transform.localScale = new Vector3(0.3f, 0.15f, 1f);
            DestroyImmediate(cube.GetComponent<Collider>());

            // Add collider on root for interaction raycast
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(0.3f, 0.15f, 1f);
            col.center = new Vector3(0f, 0.075f, 0f);

            var pickup = go.AddComponent<WeaponPickup>();
            var so = new UnityEditor.SerializedObject(pickup);
            so.FindProperty("type").enumValueIndex = (int)type;
            so.FindProperty("data").objectReferenceValue = data;
            so.FindProperty("weaponPrefab").objectReferenceValue = prefab;
            so.FindProperty("displayName").stringValue = data != null ? data.displayName : type.ToString();
            so.ApplyModifiedProperties();

            // Add Highlightable for outline
            go.AddComponent<Highlightable>().HighlightColor = new Color(1f, 0.85f, 0.2f);
        }

        void CreateTargets()
        {
            var root = new GameObject("Generated_Targets");
            SpawnTarget(root, new Vector3(-5f, 1.5f, -10f));
            SpawnTarget(root, new Vector3(0f, 1.5f, 15f));
            SpawnTarget(root, new Vector3(8f, 1.5f, 30f));
        }

        void SpawnTarget(GameObject parent, Vector3 pos)
        {
            var root = new GameObject("Target");
            root.transform.SetParent(parent.transform);
            root.transform.position = pos;

            // Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(root.transform);
            body.transform.localScale = new Vector3(0.8f, 1.4f, 0.3f);
            body.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            body.AddComponent<HitZone>().Multiplier = 1f;

            // Head
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(root.transform);
            head.transform.localScale = Vector3.one * 0.4f;
            head.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            head.AddComponent<HitZone>().Multiplier = 3f;
            head.GetComponent<HitZone>().ZoneType = Lake.Core.HitZoneType.Head;

            var hc = root.AddComponent<HealthComponent>();
            var so = new UnityEditor.SerializedObject(hc);
            so.FindProperty("maxHealth").floatValue = 50f;
            so.FindProperty("destroyOnDeath").boolValue = true;
            so.FindProperty("destroyDelay").floatValue = 2f;
            so.ApplyModifiedProperties();

            // Add Highlightable for outline
            root.AddComponent<Highlightable>().HighlightColor = new Color(1f, 0.4f, 0.2f);

            root.AddComponent<TestTarget>();
        }

        void CreateHud()
        {
            var hudGo = new GameObject("Generated_HUD");
            hudGo.AddComponent<HudBuilder>().Build();
        }
    }
}
#endif