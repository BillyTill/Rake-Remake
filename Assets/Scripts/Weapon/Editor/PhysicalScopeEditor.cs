using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Lake.Weapon.Editor
{
    /// <summary>
    /// Editor-утилита для автоматического создания и настройки системы физического прицела.
    /// 
    /// Создаёт:
    /// - Render Texture с HDR форматом
    /// - Scope Camera с HD Additional Camera Data
    /// - Настраивает слои и Culling Mask
    /// </summary>
    public static class PhysicalScopeSetup
    {
        private const string RENDER_TEXTURE_PATH = "Assets/Textures/RT_WeaponScope.renderTexture";
        private const int DEFAULT_RESOLUTION = 1024;

        [MenuItem("Lake/Weapon/Setup Physical Scope System", false, 100)]
        public static void SetupPhysicalScopeSystem()
        {
            Debug.Log("[PhysicalScopeSetup] Начало настройки системы физического прицела...");

            // Шаг 1: Создать папки если не существуют
            CreateDirectories();

            // Шаг 2: Создать Render Texture
            RenderTexture rt = CreateRenderTexture();

            // Шаг 3: Проверить слои
            VerifyLayers();

            // Шаг 4: Показать инструкции
            ShowInstructions();

            Debug.Log("[PhysicalScopeSetup] Настройка завершена!");
        }

        [MenuItem("Lake/Weapon/Create Scope Camera", false, 101)]
        public static Camera CreateScopeCamera()
        {
            GameObject scopeCameraObj = new GameObject("ScopeCamera");

            // Добавляем Camera компонент
            Camera scopeCamera = scopeCameraObj.AddComponent<Camera>();
            scopeCamera.enabled = false; // Отключена по умолчанию

            // Настраиваем Camera
            scopeCamera.usePhysicalProperties = true;
            scopeCamera.fieldOfView = 8f; // Узкий угол для оптического прицела
            scopeCamera.nearClipPlane = 0.01f;
            scopeCamera.farClipPlane = 1000f;

            // Настраиваем Culling Mask (исключаем Weapon и PlayerBody)
            int weaponLayer = LayerMask.NameToLayer("Weapon");
            int playerBodyLayer = LayerMask.NameToLayer("PlayerBody");

            scopeCamera.cullingMask = ~0; // Все слои
            if (weaponLayer != -1)
            {
                scopeCamera.cullingMask &= ~(1 << weaponLayer);
                Debug.Log($"[PhysicalScopeSetup] Слой Weapon ({weaponLayer}) исключён из Culling Mask");
            }
            if (playerBodyLayer != -1)
            {
                scopeCamera.cullingMask &= ~(1 << playerBodyLayer);
                Debug.Log($"[PhysicalScopeSetup] Слой PlayerBody ({playerBodyLayer}) исключён из Culling Mask");
            }

            // Добавляем HD Additional Camera Data
            HDAdditionalCameraData hdData = scopeCameraObj.AddComponent<HDAdditionalCameraData>();
            hdData.customRenderingSettings = true;

            // Настраиваем Frame Settings для HDRP
            var frameSettings = hdData.renderingPathCustomFrameSettings;
            frameSettings.SetEnabled(FrameSettingsField.Volumetrics, true);
            frameSettings.SetEnabled(FrameSettingsField.BigTilePrepass, true);
            frameSettings.SetEnabled(FrameSettingsField.Postprocess, true);
            hdData.renderingPathCustomFrameSettings = frameSettings;

            // Назначаем Render Texture
            RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RENDER_TEXTURE_PATH);
            if (rt != null)
            {
                scopeCamera.targetTexture = rt;
                Debug.Log("[PhysicalScopeSetup] Render Texture назначена на Scope Camera");
            }
            else
            {
                Debug.LogWarning("[PhysicalScopeSetup] Render Texture не найдена. Назначьте вручную.");
            }

            // Выбираем созданный объект
            Selection.activeGameObject = scopeCameraObj;

            Debug.Log($"[PhysicalScopeSetup] Scope Camera создана: {scopeCameraObj.name}");
            return scopeCamera;
        }

        private static void CreateDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Textures"))
            {
                AssetDatabase.CreateFolder("Assets", "Textures");
                Debug.Log("[PhysicalScopeSetup] Создана папка Assets/Textures");
            }
        }

        private static RenderTexture CreateRenderTexture()
        {
            // Проверяем, существует ли уже
            RenderTexture existingRT = AssetDatabase.LoadAssetAtPath<RenderTexture>(RENDER_TEXTURE_PATH);
            if (existingRT != null)
            {
                Debug.Log($"[PhysicalScopeSetup] Render Texture уже существует: {RENDER_TEXTURE_PATH}");
                return existingRT;
            }

            // Создаём новую Render Texture
            RenderTexture rt = new RenderTexture(DEFAULT_RESOLUTION, DEFAULT_RESOLUTION, 24, RenderTextureFormat.ARGBHalf)
            {
                name = "RT_WeaponScope",
                antiAliasing = 4, // MSAA 4x
                useMipMap = false,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                dimension = TextureDimension.Tex2D,
                enableRandomWrite = false
            };

            // Сохраняем как ассет
            AssetDatabase.CreateAsset(rt, RENDER_TEXTURE_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PhysicalScopeSetup] Render Texture создана: {RENDER_TEXTURE_PATH}");
            Debug.Log($"  - Разрешение: {DEFAULT_RESOLUTION}x{DEFAULT_RESOLUTION}");
            Debug.Log($"  - Формат: ARGBHalf (HDR)");
            Debug.Log($"  - Depth Buffer: 24-bit");
            Debug.Log($"  - Anti-Aliasing: MSAA 4x");

            Selection.activeObject = rt;
            return rt;
        }

        private static void VerifyLayers()
        {
            // Проверяем наличие слоя Weapon
            int weaponLayer = LayerMask.NameToLayer("Weapon");
            if (weaponLayer == -1)
            {
                Debug.LogWarning("[PhysicalScopeSetup] Слой 'Weapon' не найден!");
                Debug.LogWarning("  → Создайте слой Weapon в Edit → Project Settings → Tags and Layers");
                Debug.LogWarning("  → Рекомендуемый номер: 10");
            }
            else
            {
                Debug.Log($"[PhysicalScopeSetup] Слой 'Weapon' найден: {weaponLayer}");
            }

            // Проверяем PlayerBody (опционально)
            int playerBodyLayer = LayerMask.NameToLayer("PlayerBody");
            if (playerBodyLayer == -1)
            {
                Debug.LogWarning("[PhysicalScopeSetup] Слой 'PlayerBody' не найден (опционально)");
            }
            else
            {
                Debug.Log($"[PhysicalScopeSetup] Слой 'PlayerBody' найден: {playerBodyLayer}");
            }
        }

        private static void ShowInstructions()
        {
            string instructions = @"
╔═══════════════════════════════════════════════════════════════╗
║          СИСТЕМА ФИЗИЧЕСКОГО ПРИЦЕЛА УСТАНОВЛЕНА              ║
╚═══════════════════════════════════════════════════════════════╝

✅ СОЗДАНО:
   • Render Texture: Assets/Textures/RT_WeaponScope.renderTexture
   • Формат: ARGBHalf (HDR для HDRP)
   • Разрешение: 1024x1024

📋 СЛЕДУЮЩИЕ ШАГИ:

1. СОЗДАТЬ SCOPE CAMERA:
   Menu → Lake → Weapon → Create Scope Camera
   
   Камера создастся автоматически с нужными настройками.

2. НАЗНАЧИТЬ МАТЕРИАЛ НА ЛИНЗУ:
   • Создайте Shader Graph: ScopeLensShader (см. документацию)
   • Создайте Material: Mat_ScopeLens
   • Назначьте материал на меш линзы/окуляра в модели оружия

3. ДОБАВИТЬ КОМПОНЕНТ PhysicalScope:
   • Выберите объект оружия (или камеру)
   • Add Component → PhysicalScope
   • Назначьте ссылки:
     - Scope Camera
     - Render Texture (RT_WeaponScope)
     - Animator (если есть)

4. НАСТРОИТЬ СЛОИ:
   • Выберите модель оружия
   • В Inspector установите Layer: Weapon
   • Apply to Children (применить ко всем дочерним)

5. СОЗДАТЬ ANIMATOR CONTROLLER:
   • Параметр: IsAiming (bool)
   • Анимация: Idle ↔ Aim
   • Transition Duration: 0.15 сек

6. ТЕСТИРОВАНИЕ:
   • Play в Unity Editor
   • Нажмите ПКМ (Fire2)
   • Оружие должно приблизиться к камере
   • На линзе должно появиться изображение

═══════════════════════════════════════════════════════════════

⚠️ ВАЖНО:
   • Камера прицела должна быть ОТКЛЮЧЕНА по умолчанию
   • Culling Mask: исключить слой Weapon
   • Render Texture формат: ARGBHalf (обязательно для HDRP)

📖 ДОКУМЕНТАЦИЯ:
   • PhysicalScope.cs — основной скрипт
   • ScopeLensShader_Instructions.md — создание шейдера

═══════════════════════════════════════════════════════════════
";

            Debug.Log(instructions);
        }

        [MenuItem("Lake/Weapon/Verify Scope Setup", false, 102)]
        public static void VerifyScopeSetup()
        {
            Debug.Log("[PhysicalScopeSetup] Проверка настройки...");

            bool allGood = true;

            // Проверяем Render Texture
            RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RENDER_TEXTURE_PATH);
            if (rt == null)
            {
                Debug.LogError("❌ Render Texture не найдена!");
                allGood = false;
            }
            else
            {
                Debug.Log($"✅ Render Texture: {rt.name} ({rt.width}x{rt.height}, {rt.format})");
                
                if (rt.format != RenderTextureFormat.ARGBHalf)
                {
                    Debug.LogWarning("⚠️ Render Texture формат не HDR! Рекомендуется ARGBHalf.");
                }
            }

            // Проверяем слои
            int weaponLayer = LayerMask.NameToLayer("Weapon");
            if (weaponLayer == -1)
            {
                Debug.LogWarning("⚠️ Слой 'Weapon' не найден. Создайте в Project Settings.");
                allGood = false;
            }
            else
            {
                Debug.Log($"✅ Слой 'Weapon': {weaponLayer}");
            }

            // Проверяем PhysicalScope компоненты в сцене
            PhysicalScope[] scopes = Object.FindObjectsOfType<PhysicalScope>();
            if (scopes.Length == 0)
            {
                Debug.LogWarning("⚠️ Компонент PhysicalScope не найден в сцене.");
            }
            else
            {
                Debug.Log($"✅ Найдено PhysicalScope компонентов: {scopes.Length}");
                foreach (var scope in scopes)
                {
                    Debug.Log($"  - {scope.gameObject.name}");
                }
            }

            if (allGood)
            {
                Debug.Log("✅ Все проверки пройдены! Система готова к работе.");
            }
            else
            {
                Debug.LogWarning("⚠️ Обнаружены проблемы. Следуйте инструкциям выше.");
            }
        }
    }
}
