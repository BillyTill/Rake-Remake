using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using Lake.Sky;

namespace Lake.SkyEditor
{
    /// <summary>
    /// Разовая сборка цикла суток в сцене: находит солнце, луну, профиль неба,
    /// создаёт объект с циклом и погодой и прописывает ссылки.
    ///
    /// Руками это десяток перетаскиваний, в которых легко промахнуться, а
    /// несведённая ссылка молчит до запуска.
    /// </summary>
    public static class SkyCycleMenu
    {
        private const string HolderName = "Day Night Cycle";

        [MenuItem("Tools/Lake/Цикл суток: настроить сцену", priority = 30)]
        public static void SetupCycle()
        {
            var scene = SceneManager.GetActiveScene();

            Light sun = FindDirectional("Sun", "Directional Light");
            Light moon = FindDirectional("MoonLight", "Moon");
            Volume volume = FindGlobalVolume();

            if (volume == null)
            {
                EditorUtility.DisplayDialog("Lake",
                    "В сцене не найдена глобальная Volume с профилем.\n" +
                    "Цикл суток не сможет менять небо, туман и облака.",
                    "Понятно");
                return;
            }

            var holder = GameObject.Find(HolderName);
            if (holder == null)
            {
                holder = new GameObject(HolderName);
                Undo.RegisterCreatedObjectUndo(holder, "Создать цикл суток");
            }

            var cycle = holder.GetComponent<DayNightCycle>();
            if (cycle == null)
                cycle = Undo.AddComponent<DayNightCycle>(holder);

            var weather = holder.GetComponent<WeatherController>();
            if (weather == null)
                weather = Undo.AddComponent<WeatherController>(holder);

            cycle.sun = sun;
            cycle.moon = moon;
            cycle.volume = volume;
            cycle.weather = weather;

            EditorUtility.SetDirty(cycle);
            EditorUtility.SetDirty(weather);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Lake] Цикл суток настроен.\n" +
                      $"  солнце: {(sun != null ? sun.name : "НЕ НАЙДЕНО")}\n" +
                      $"  луна:   {(moon != null ? moon.name : "НЕ НАЙДЕНО")}\n" +
                      $"  объём:  {volume.name}\n" +
                      $"  объект: {HolderName} (цикл, погода, фотосъёмка)");

            if (EditorUtility.DisplayDialog("Lake",
                    "Цикл суток настроен и сцена сохранена.\n\n" +
                    "Заодно выключить облака пакета (CustomPassVolume на объекте Clouds)?\n" +
                    "Сейчас небо рисуют встроенные объёмные облака HDRP — второй слой\n" +
                    "может дублировать их и стоит кадров.",
                    "Выключить облака пакета", "Оставить"))
            {
                SetPackageClouds(false);
            }
        }

        [MenuItem("Tools/Lake/Облака пакета: включить/выключить", priority = 31)]
        public static void TogglePackageClouds()
        {
            var volumes = Object.FindObjectsByType<CustomPassVolume>(FindObjectsSortMode.None);

            bool anyEnabled = false;
            for (int i = 0; i < volumes.Length; i++)
                if (volumes[i].enabled)
                    anyEnabled = true;

            SetPackageClouds(!anyEnabled);
        }

        private static void SetPackageClouds(bool enabled)
        {
            var volumes = Object.FindObjectsByType<CustomPassVolume>(FindObjectsSortMode.None);

            if (volumes.Length == 0)
            {
                Debug.Log("[Lake] В сцене нет CustomPassVolume — облака пакета и так выключены.");
                return;
            }

            for (int i = 0; i < volumes.Length; i++)
            {
                Undo.RecordObject(volumes[i], "Переключить облака пакета");
                volumes[i].enabled = enabled;
                EditorUtility.SetDirty(volumes[i]);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            Debug.Log(enabled
                ? "[Lake] Облака пакета включены: CustomPassVolume работает поверх встроенных облаков."
                : "[Lake] Облака пакета выключены: небо рисуют только встроенные объёмные облака HDRP.");
        }

        private static Light FindDirectional(params string[] names)
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);

            for (int n = 0; n < names.Length; n++)
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].type == LightType.Directional &&
                        string.Equals(lights[i].name, names[n], System.StringComparison.OrdinalIgnoreCase))
                        return lights[i];
                }
            }

            return null;
        }

        private static Volume FindGlobalVolume()
        {
            var volumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
            Volume fallback = null;

            for (int i = 0; i < volumes.Length; i++)
            {
                if (!volumes[i].isGlobal || volumes[i].sharedProfile == null)
                    continue;

                if (volumes[i].name.Contains("Sky") || volumes[i].sharedProfile.name.Contains("Sky"))
                    return volumes[i];

                if (fallback == null)
                    fallback = volumes[i];
            }

            return fallback;
        }
    }
}
