using UnityEngine;

namespace Lake.Weapon
{
    /// <summary>
    /// Процедурные модели оружия из примитивов.
    ///
    /// Меши стволов в проекте нет, поэтому винтовка и дробовик собираются
    /// из кубов и цилиндров во время Awake риги. Ориентир: ствол смотрит в +Z,
    /// дуло — в конце. Когда появятся нормальные меши, билдер заменяется
    /// присвоением готовых GameObject, а вся система адресуется через
    /// WeaponDef.muzzleLocal и якоря, так что ничего не переписывается.
    ///
    /// Материалы только HDRP/Lit: шейдер гарантированно живёт в билде —
    /// на нём сидит вся сцена. Спец-световки (аддитивные вспышки и т.п.)
    /// сознательно не тянутся: их шейдеры рискует вырезать стриппинг, и эффект
    /// молча пропадёт только в стоящем exe. Свечение дают HDRP-света пула FX.
    /// </summary>
    public static class WeaponModelBuilder
    {
        private static Material _steel;
        private static Material _darkMetal;
        private static Material _wood;
        private static Material _scopeGlass;

        /// <summary>Собрать модель оружия. Возвращает корень, у которого +Z = ствол.</summary>
        public static GameObject Build(WeaponId id, Transform parent, float gunYawDeg)
        {
            GameObject root = new GameObject(id == WeaponId.Rifle ? "WMRifle" : "WMShotgun");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.Euler(0f, gunYawDeg, 0f);
            root.transform.localScale = Vector3.one;

            if (id == WeaponId.Rifle)
                BuildRifle(root);
            else
                BuildShotgun(root);

            return root;
        }

        private static void BuildRifle(GameObject root)
        {
            EnsureMaterials();

            // Ствольная коробка.
            Box(root, new Vector3(0f, 0f, 0f), new Vector3(0.052f, 0.085f, 0.32f), _steel);
            // Ствол: цилиндр по умолчанию стоит по Y, разворачиваем на +Z.
            Cylinder(root, new Vector3(0f, 0.005f, 0.31f), 0.013f, 0.42f, _darkMetal);
            // Резкий мушка.
            Box(root, new Vector3(0f, 0.030f, 0.50f), new Vector3(0.006f, 0.036f, 0.007f), _darkMetal);
            // Оптика: труба над ствольной коробкой + оккуляр и объектив.
            Cylinder(root, new Vector3(0f, 0.058f, 0.012f), 0.021f, 0.17f, _darkMetal);
            Cylinder(root, new Vector3(0f, 0.058f, 0.105f), 0.028f, 0.032f, _scopeGlass);
            Cylinder(root, new Vector3(0f, 0.058f, -0.078f), 0.026f, 0.028f, _scopeGlass);
            // Кольца оптики.
            Box(root, new Vector3(0f, 0.046f, -0.02f), new Vector3(0.012f, 0.026f, 0.018f), _steel);
            Box(root, new Vector3(0f, 0.046f, 0.058f), new Vector3(0.012f, 0.026f, 0.018f), _steel);
            // Ложе: приклад и цевьё.
            Box(root, new Vector3(0f, -0.028f, -0.20f), new Vector3(0.05f, 0.105f, 0.20f), _wood);
            Box(root, new Vector3(0f, -0.020f, 0.14f), new Vector3(0.058f, 0.052f, 0.14f), _wood);
            // Пистолетный хват с наклоном.
            GameObject grip = Box(root, new Vector3(0f, -0.082f, -0.055f), new Vector3(0.032f, 0.095f, 0.05f), _wood);
            grip.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            // Магазин.
            GameObject mag = Box(root, new Vector3(0f, -0.070f, 0.02f), new Vector3(0.034f, 0.088f, 0.062f), _darkMetal);
            mag.transform.localRotation = Quaternion.Euler(-6f, 0f, 0f);
            // Затворная рукоять справа.
            Box(root, new Vector3(0.033f, 0.018f, -0.02f), new Vector3(0.018f, 0.02f, 0.06f), _darkMetal);
        }

        private static void BuildShotgun(GameObject root)
        {
            EnsureMaterials();

            // Колея: верхний ствол и под ним трубчатый магазин.
            Cylinder(root, new Vector3(0f, 0.012f, 0.42f), 0.017f, 0.55f, _darkMetal);
            Cylinder(root, new Vector3(0f, -0.020f, 0.30f), 0.014f, 0.42f, _darkMetal);
            // Ствольная коробка.
            Box(root, new Vector3(0f, 0f, -0.02f), new Vector3(0.055f, 0.09f, 0.30f), _steel);
            // Помповое цевьё — по нему «работает рука».
            Box(root, new Vector3(0f, -0.024f, 0.26f), new Vector3(0.062f, 0.062f, 0.15f), _wood);
            // Мушка-перл.
            Box(root, new Vector3(0f, 0.036f, 0.68f), new Vector3(0.006f, 0.026f, 0.010f), _darkMetal);
            // Приклад.
            Box(root, new Vector3(0f, -0.017f, -0.235f), new Vector3(0.052f, 0.11f, 0.26f), _wood);
            GameObject grip = Box(root, new Vector3(0f, -0.078f, -0.075f), new Vector3(0.034f, 0.092f, 0.05f), _wood);
            grip.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            // Выбрасыватель на правой грани.
            Box(root, new Vector3(0.030f, 0.008f, 0.10f), new Vector3(0.01f, 0.05f, 0.03f), _darkMetal);
        }

        // ── Примитивы ─────────────────────────────────────────────────────

        private static GameObject Box(GameObject root, Vector3 localPos, Vector3 size, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Box";
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            // Ножи-детали часто мельче тени: с тень выключаем, экран легче.
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return go;
        }

        /// <summary>Цилиндр с осью +Z (в Unity цилиндр стоит по Y): height — длина ствола в метрах.</summary>
        private static GameObject Cylinder(GameObject root, Vector3 localPos, float radius, float length, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Cylinder";
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            // Примитивный цилиндр имеет диаметр 1 и высоту 1:(radius*2, length).
            go.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return go;
        }

        // ── Материалы ─────────────────────────────────────────────────────

        private static void EnsureMaterials()
        {
            if (_steel != null)
                return;

            // Сталь ствольной коробки: цвет тихий, читается при любом свете
            // за счёт металлических бликов от вспышек (см. WeaponFX).
            _steel = MakeLit(new Color(0.11f, 0.115f, 0.125f), metallic: 0.85f, smoothness: 0.4f);
            _darkMetal = MakeLit(new Color(0.035f, 0.038f, 0.042f), metallic: 0.9f, smoothness: 0.62f);
            _wood = MakeLit(new Color(0.16f, 0.10f, 0.062f), metallic: 0f, smoothness: 0.16f);
            _scopeGlass = MakeLit(new Color(0.02f, 0.04f, 0.06f), metallic: 0.6f, smoothness: 0.95f);
            TryEnableEmission(_scopeGlass, new Color(0.06f, 0.16f, 0.35f), 0.4f);
        }

        private static Material MakeLit(Color baseColor, float metallic, float smoothness)
        {
            Shader shader = Shader.Find("HDRP/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var material = new Material(shader);
            material.SetColor("_BaseColor", baseColor);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);

            return material;
        }

        /// <summary>Свечение — мягкая попытка: если свойства/ключевые слова шейдера не совпали, просто останется обычный цвет.</summary>
        private static void TryEnableEmission(Material material, Color color, float intensity)
        {
            if (!material.HasProperty("_EmissiveColor"))
                return;

            material.EnableKeyword("_EMISSIVE_COLOR");
            material.SetColor("_EmissiveColor", color * Mathf.GammaToLinearSpace(intensity));
            if (material.HasProperty("_EmissiveIntensity"))
                material.SetFloat("_EmissiveIntensity", intensity);
        }
    }
}
