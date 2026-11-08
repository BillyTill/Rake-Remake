using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Lake.Weapons;
using Lake.BodyCam;
using Lake.Player;
using Lake.Scene;
using Lake.Health;

/// <summary>
/// One-click setup for Bodycam Horror Prototype v2.
/// Creates all ScriptableObjects, Prefabs, Scene setup, and generates the level.
/// Run via: Tools/Bodycam v2/Full Setup
/// </summary>
public class BodycamV2Setup : EditorWindow
{
    [MenuItem("Tools/Bodycam v2/Full Setup")]
    public static void FullSetup()
    {
        CreateAssets();
        CreatePrefabs();
        SetupScene();
        Debug.Log("[Bodycam v2] Full setup complete! Enter Play Mode to test.");
    }

    [MenuItem("Tools/Bodycam v2/Create Assets")]
    public static void CreateAssets()
    {
        var assetsDir = "Assets/_Project/Resources";
        System.IO.Directory.CreateDirectory(assetsDir);

        // Weapon_Rifle
        var rifleData = ScriptableObject.CreateInstance<WeaponDataSO>();
        rifleData.type = WeaponType.Rifle;
        rifleData.displayName = "Assault Rifle";
        rifleData.isAutomatic = true;
        rifleData.fireRate = 0.12f;
        rifleData.pelletsPerShot = 1;
        rifleData.damagePerPellet = 25f;
        rifleData.range = 100f;
        rifleData.hitMask = ~0;
        rifleData.baseSpread = 0.3f;
        rifleData.spreadPerShot = 0.25f;
        rifleData.maxSpread = 3f;
        rifleData.spreadRecovery = 4f;
        rifleData.adsSpreadMultiplier = 0f;
        rifleData.falloffStart = 30f;
        rifleData.falloffEnd = 80f;
        rifleData.minDamageMultiplier = 0.4f;
        rifleData.recoilVertical = 1.2f;
        rifleData.recoilHorizontal = 0.3f;
        rifleData.visualKickback = 0.08f;
        rifleData.magazineSize = 30;
        rifleData.reserveAmmoMax = 120;
        rifleData.reloadTime = 2.5f;
        rifleData.shellByShellReload = false;
        rifleData.shellReloadTime = 0.5f;
        rifleData.hipPosition = new Vector3(0.22f, -0.22f, 0.4f);
        rifleData.adsPosition = new Vector3(0f, -0.14f, 0.3f);
        rifleData.hipFov = 65f;
        rifleData.adsFov = 48f;
        rifleData.adsSensitivityMultiplier = 0.55f;
        AssetDatabase.CreateAsset(rifleData, $"{assetsDir}/Weapon_Rifle.asset");

        // Weapon_Shotgun
        var shotgunData = ScriptableObject.CreateInstance<WeaponDataSO>();
        shotgunData.type = WeaponType.Shotgun;
        shotgunData.displayName = "Pump Shotgun";
        shotgunData.isAutomatic = false;
        shotgunData.fireRate = 0.8f;
        shotgunData.pelletsPerShot = 8;
        shotgunData.damagePerPellet = 10f;
        shotgunData.range = 50f;
        shotgunData.hitMask = ~0;
        shotgunData.baseSpread = 5f;
        shotgunData.spreadPerShot = 1.5f;
        shotgunData.maxSpread = 12f;
        shotgunData.spreadRecovery = 3f;
        shotgunData.adsSpreadMultiplier = 0.3f;
        shotgunData.falloffStart = 8f;
        shotgunData.falloffEnd = 25f;
        shotgunData.minDamageMultiplier = 0.15f;
        shotgunData.recoilVertical = 5f;
        shotgunData.recoilHorizontal = 1f;
        shotgunData.visualKickback = 0.15f;
        shotgunData.magazineSize = 8;
        shotgunData.reserveAmmoMax = 32;
        shotgunData.reloadTime = 3f;
        shotgunData.shellByShellReload = true;
        shotgunData.shellReloadTime = 0.5f;
        shotgunData.hipPosition = new Vector3(0.3f, -0.3f, 0.5f);
        shotgunData.adsPosition = new Vector3(0f, -0.18f, 0.4f);
        shotgunData.hipFov = 65f;
        shotgunData.adsFov = 52f;
        shotgunData.adsSensitivityMultiplier = 0.55f;
        AssetDatabase.CreateAsset(shotgunData, $"{assetsDir}/Weapon_Shotgun.asset");

        // BodyCam_Default
        var bodyCamConfig = ScriptableObject.CreateInstance<BodyCamConfigSO>();
        bodyCamConfig.lensDistortion = 0.25f;
        bodyCamConfig.lensScale = 0.95f;
        bodyCamConfig.chromaticAberration = 0.15f;
        bodyCamConfig.vignette = 0.35f;
        bodyCamConfig.filmGrain = 0.4f;
        bodyCamConfig.saturation = -12f;
        bodyCamConfig.contrast = 5f;
        bodyCamConfig.motionBlur = 0.15f;
        bodyCamConfig.bloom = 0.1f;
        AssetDatabase.CreateAsset(bodyCamConfig, $"{assetsDir}/BodyCam_Default.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Bodycam v2] Assets created at " + assetsDir);
    }

    [MenuItem("Tools/Bodycam v2/Create Prefabs")]
    public static void CreatePrefabs()
    {
        var prefabsDir = "Assets/_Project/Prefabs";
        System.IO.Directory.CreateDirectory(prefabsDir);

        // --- MuzzleFlash Prefab ---
        var muzzleFlash = new GameObject("MuzzleFlash");
        var ps = muzzleFlash.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.1f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.9f, 0.4f, 1f),
            new Color(1f, 0.5f, 0.1f, 0f)
        );
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 5, 15) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.05f;
        muzzleFlash.AddComponent<Lake.Weapons.VFX.MuzzleFlash>();
        muzzleFlash.AddComponent<Lake.Weapons.VFX.MuzzleFlashLight>();
        var prefabPath = $"{prefabsDir}/MuzzleFlash.prefab";
        PrefabUtility.SaveAsPrefabAsset(muzzleFlash, prefabPath);
        DestroyImmediate(muzzleFlash);

        // --- Tracer Prefab ---
        var tracer = new GameObject("Tracer");
        var lr = tracer.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = 0.02f;
        lr.endWidth = 0.01f;
        lr.material = new Material(Shader.Find("Sprites/Default"))
        {
            color = new Color(1f, 0.9f, 0.3f, 0.8f)
        };
        lr.startColor = new Color(1f, 0.9f, 0.3f, 0.8f);
        lr.endColor = new Color(1f, 0.5f, 0.1f, 0f);
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        tracer.AddComponent<Lake.Weapons.VFX.TracerLine>();
        prefabPath = $"{prefabsDir}/Tracer.prefab";
        PrefabUtility.SaveAsPrefabAsset(tracer, prefabPath);
        DestroyImmediate(tracer);

        // --- Rifle Prefab ---
        var rifle = new GameObject("Rifle");
        rifle.AddComponent<Lake.Weapons.WeaponRuntime>();
        rifle.AddComponent<Lake.Weapons.WeaponSway>();
        // Visual: simple boxes
        var rifleStock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rifleStock.name = "Stock";
        rifleStock.transform.SetParent(rifle.transform);
        rifleStock.transform.localPosition = new Vector3(0f, -0.05f, -0.2f);
        rifleStock.transform.localScale = new Vector3(0.1f, 0.15f, 0.3f);
        DestroyImmediate(rifleStock.GetComponent<Collider>());

        var rifleBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rifleBody.name = "Body";
        rifleBody.transform.SetParent(rifle.transform);
        rifleBody.transform.localPosition = new Vector3(0f, 0f, 0.1f);
        rifleBody.transform.localScale = new Vector3(0.12f, 0.1f, 0.5f);
        DestroyImmediate(rifleBody.GetComponent<Collider>());

        var rifleBarrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rifleBarrel.name = "Barrel";
        rifleBarrel.transform.SetParent(rifle.transform);
        rifleBarrel.transform.localPosition = new Vector3(0f, 0.02f, 0.5f);
        rifleBarrel.transform.localScale = new Vector3(0.04f, 0.5f, 0.04f);
        rifleBarrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        DestroyImmediate(rifleBarrel.GetComponent<Collider>());

        var muzzlePoint = new GameObject("MuzzlePoint");
        muzzlePoint.transform.SetParent(rifle.transform);
        muzzlePoint.transform.localPosition = new Vector3(0f, 0.02f, 0.8f);
        prefabPath = $"{prefabsDir}/Rifle.prefab";
        PrefabUtility.SaveAsPrefabAsset(rifle, prefabPath);
        DestroyImmediate(rifle);

        // --- Shotgun Prefab ---
        var shotgun = new GameObject("Shotgun");
        shotgun.AddComponent<Lake.Weapons.WeaponRuntime>();
        shotgun.AddComponent<Lake.Weapons.WeaponSway>();

        var sgStock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sgStock.name = "Stock";
        sgStock.transform.SetParent(shotgun.transform);
        sgStock.transform.localPosition = new Vector3(0f, -0.08f, -0.25f);
        sgStock.transform.localScale = new Vector3(0.12f, 0.2f, 0.35f);
        DestroyImmediate(sgStock.GetComponent<Collider>());

        var sgBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sgBody.name = "Body";
        sgBody.transform.SetParent(shotgun.transform);
        sgBody.transform.localPosition = new Vector3(0f, 0f, 0.15f);
        sgBody.transform.localScale = new Vector3(0.14f, 0.12f, 0.6f);
        DestroyImmediate(sgBody.GetComponent<Collider>());

        var sgBarrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        sgBarrel.name = "Barrel";
        sgBarrel.transform.SetParent(shotgun.transform);
        sgBarrel.transform.localPosition = new Vector3(0f, 0.03f, 0.6f);
        sgBarrel.transform.localScale = new Vector3(0.05f, 0.6f, 0.05f);
        sgBarrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        DestroyImmediate(sgBarrel.GetComponent<Collider>());

        var sgMuzzle = new GameObject("MuzzlePoint");
        sgMuzzle.transform.SetParent(shotgun.transform);
        sgMuzzle.transform.localPosition = new Vector3(0f, 0.03f, 0.9f);
        prefabPath = $"{prefabsDir}/Shotgun.prefab";
        PrefabUtility.SaveAsPrefabAsset(shotgun, prefabPath);
        DestroyImmediate(shotgun);

        // --- BodycamPlayer Prefab ---
        CreatePlayerPrefab(prefabsDir);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Bodycam v2] Prefabs created at " + prefabsDir);
    }

    static void CreatePlayerPrefab(string prefabsDir)
    {
        var player = new GameObject("BodycamPlayer");
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        player.AddComponent<Lake.Player.PlayerMotor>();
        player.AddComponent<Lake.Player.BodyFollower>();
        player.AddComponent<Lake.Player.CameraEffects>();
        player.AddComponent<Lake.Player.PlayerInteractor>();
        player.AddComponent<Lake.Health.HealthComponent>();

        // AimPivot
        var aimPivot = new GameObject("AimPivot");
        aimPivot.transform.SetParent(player.transform);
        aimPivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        var aimPivotComp = aimPivot.AddComponent<Lake.Aim.AimPivot>();

        // WeaponHolder (child of AimPivot)
        var weaponHolder = new GameObject("WeaponHolder");
        weaponHolder.transform.SetParent(aimPivot.transform);
        weaponHolder.transform.localPosition = Vector3.zero;
        weaponHolder.transform.localRotation = Quaternion.identity;

        var weaponController = weaponHolder.AddComponent<Lake.Weapons.WeaponController>();
        var adsController = weaponHolder.AddComponent<Lake.Weapons.ADSController>();

        // Flashlight (child of AimPivot)
        var flashlight = new GameObject("Flashlight");
        flashlight.transform.SetParent(aimPivot.transform);
        flashlight.transform.localPosition = new Vector3(0.1f, -0.05f, 0.2f);
        flashlight.transform.localRotation = Quaternion.identity;
        var fl = flashlight.AddComponent<Light>();
        fl.type = LightType.Spot;
        fl.spotAngle = 45f;
        fl.range = 50f;
        fl.color = new Color(1f, 0.95f, 0.85f);
        fl.shadows = LightShadows.Soft;
        fl.enabled = false;
        flashlight.AddComponent<Lake.Flashlight.Flashlight>();

        // CameraContainer (child of Player)
        var cameraContainer = new GameObject("CameraContainer");
        cameraContainer.transform.SetParent(player.transform);
        cameraContainer.transform.localPosition = new Vector3(0f, 1.6f, 0f);

        // Camera (child of CameraContainer)
        var camObj = new GameObject("Camera");
        camObj.transform.SetParent(cameraContainer.transform);
        camObj.transform.localPosition = Vector3.zero;
        camObj.transform.localRotation = Quaternion.identity;
        var cam = camObj.AddComponent<Camera>();
        camObj.AddComponent<Lake.Aim.CameraFollower>();
        camObj.AddComponent<Lake.Aim.RecoilController>();

        // Link references via SerializedObject (since private fields)
        var playerMotor = player.GetComponent<Lake.Player.PlayerMotor>();
        var bodyFollower = player.GetComponent<Lake.Player.BodyFollower>();
        var cameraEffects = player.GetComponent<Lake.Player.CameraEffects>();
        var playerInteractor = player.GetComponent<Lake.Player.PlayerInteractor>();

        var soMotor = new SerializedObject(playerMotor);
        soMotor.FindProperty("cameraTransform").objectReferenceValue = camObj.transform;
        soMotor.ApplyModifiedProperties();

        var soBody = new SerializedObject(bodyFollower);
        soBody.FindProperty("aimPivot").objectReferenceValue = aimPivotComp;
        soBody.ApplyModifiedProperties();

        var soCamEff = new SerializedObject(cameraEffects);
        soCamEff.FindProperty("cameraContainer").objectReferenceValue = cameraContainer.transform;
        soCamEff.FindProperty("motor").objectReferenceValue = playerMotor;
        soCamEff.ApplyModifiedProperties();

        var soInteract = new SerializedObject(playerInteractor);
        soInteract.FindProperty("rayOrigin").objectReferenceValue = aimPivotComp.transform;
        soInteract.ApplyModifiedProperties();

        var soCamFollow = new SerializedObject(camObj.GetComponent<Lake.Aim.CameraFollower>());
        soCamFollow.FindProperty("aimPivot").objectReferenceValue = aimPivotComp;
        soCamFollow.ApplyModifiedProperties();

        var soRecoil = new SerializedObject(camObj.GetComponent<Lake.Aim.RecoilController>());
        soRecoil.FindProperty("aimPivot").objectReferenceValue = aimPivotComp;
        soRecoil.ApplyModifiedProperties();

        var soWeaponCtrl = new SerializedObject(weaponController);
        soWeaponCtrl.FindProperty("weaponHolder").objectReferenceValue = weaponHolder.transform;
        soWeaponCtrl.FindProperty("aimPivotTransform").objectReferenceValue = aimPivotComp.transform;
        soWeaponCtrl.ApplyModifiedProperties();

        var soADS = new SerializedObject(adsController);
        soADS.FindProperty("weaponController").objectReferenceValue = weaponController;
        soADS.FindProperty("aimPivot").objectReferenceValue = aimPivotComp;
        soADS.FindProperty("playerCamera").objectReferenceValue = cam;
        soADS.FindProperty("weaponHolder").objectReferenceValue = weaponHolder.transform;
        soADS.ApplyModifiedProperties();

        var prefabPath = $"{prefabsDir}/BodycamPlayer.prefab";
        PrefabUtility.SaveAsPrefabAsset(player, prefabPath);
        DestroyImmediate(player);
    }

    [MenuItem("Tools/Bodycam v2/Setup Scene")]
    public static void SetupScene()
    {
        // Create fresh scene
        var scenePath = "Assets/_Project/Scenes/BodycamHorrorScene.unity";
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(scenePath));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Ensure Ground tag exists
        var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        var so = new SerializedObject(tagManager);
        var tags = so.FindProperty("tags");
        bool hasGround = false;
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == "Ground") { hasGround = true; break; }
        if (!hasGround)
        {
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "Ground";
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        // Create SceneBuilder and assign references
        var builderObj = new GameObject("SceneBuilder");
        var builder = builderObj.AddComponent<HorrorSceneBuilder>();

        var rifleData = AssetDatabase.LoadAssetAtPath<WeaponDataSO>("Assets/_Project/Resources/Weapon_Rifle.asset");
        var shotgunData = AssetDatabase.LoadAssetAtPath<WeaponDataSO>("Assets/_Project/Resources/Weapon_Shotgun.asset");
        var riflePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Rifle.prefab");
        var shotgunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Shotgun.prefab");
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/BodycamPlayer.prefab");
        var bodyCamConfig = AssetDatabase.LoadAssetAtPath<BodyCamConfigSO>("Assets/_Project/Resources/BodyCam_Default.asset");

        var soBuilder = new SerializedObject(builder);
        soBuilder.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
        soBuilder.FindProperty("rifleData").objectReferenceValue = rifleData;
        soBuilder.FindProperty("shotgunData").objectReferenceValue = shotgunData;
        soBuilder.FindProperty("riflePrefab").objectReferenceValue = riflePrefab;
        soBuilder.FindProperty("shotgunPrefab").objectReferenceValue = shotgunPrefab;
        soBuilder.FindProperty("bodyCamConfig").objectReferenceValue = bodyCamConfig;
        soBuilder.ApplyModifiedProperties();

        // Create InputProvider singleton
        var inputObj = new GameObject("InputProvider");
        inputObj.AddComponent<Lake.Core.InputProvider>();

        // Generate scene
        builder.GenerateScene();

        // Save scene
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[Bodycam v2] Scene saved to " + scenePath);
    }

    [MenuItem("Tools/Bodycam v2/Regenerate Scene")]
    public static void RegenerateScene()
    {
        var builder = FindFirstObjectByType<HorrorSceneBuilder>();
        if (builder != null) builder.GenerateScene();
        else Debug.LogWarning("[Bodycam v2] HorrorSceneBuilder not found in scene");
    }
}