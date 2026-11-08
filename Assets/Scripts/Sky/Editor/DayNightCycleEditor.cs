using UnityEditor;
using UnityEngine;
using Lake.Sky;

namespace Lake.SkyEditor
{
    /// <summary>
    /// Инспектор цикла суток. Ползунок текущего часа вынесен наверх и работает
    /// сразу: тянешь — сцена меняется под рукой, и в игре, и в редакторе.
    /// Часы, минуты и кнопки-переходы рядом, чтобы не считать «0.29 = 7:00».
    /// </summary>
    [CustomEditor(typeof(DayNightCycle))]
    public sealed class DayNightCycleEditor : Editor
    {
        private static readonly string[] MomentLabels = { "Ночь 00:00", "Рассвет 06:00", "Полдень 12:00", "Закат 18:00" };
        private static readonly float[] MomentHours = { 0f, 6f, 12f, 18f };

        /// <summary>В игре часы идут сами — пусть ползунок за ними успевает.</summary>
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            var cycle = (DayNightCycle)target;

            serializedObject.Update();

            DrawTimeBlock(cycle);

            EditorGUILayout.Space();

            // Остальные поля — как есть. Ползунок часов уже нарисован выше,
            // второй такой же в общем списке только мешает.
            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (property.name == "hour")
                    continue;

                EditorGUILayout.PropertyField(property, true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTimeBlock(DayNightCycle cycle)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Часы суток", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                float value = EditorGUILayout.Slider("Текущее время", cycle.Hour, 0f, 24f);

                if (EditorGUI.EndChangeCheck())
                    SetTime(cycle, value);

                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int i = 0; i < MomentHours.Length; i++)
                    {
                        if (GUILayout.Button(MomentLabels[i], EditorStyles.miniButton))
                            SetTime(cycle, MomentHours[i]);
                    }
                }

                string clock = Clock(cycle.Hour);
                string state = cycle.previewInEditor
                    ? (Application.isPlaying ? $"погода: {cycle.WeatherName}" : "сцена меняется без Play Mode")
                    : "предпросмотр в редакторе выключен";

                EditorGUILayout.LabelField($"{clock}  ·  {cycle.Phase}  ·  {state}", EditorStyles.miniLabel);

                if (GUILayout.Button("Сбросить цвета и кривые к значениям по умолчанию"))
                {
                    Undo.RecordObject(cycle, "Сброс кривых суток");
                    cycle.ResetCurves();
                    cycle.ApplyNow();
                    EditorUtility.SetDirty(cycle);
                    SceneView.RepaintAll();
                }
            }
        }

        private static void SetTime(DayNightCycle cycle, float value)
        {
            Undo.RecordObject(cycle, "Время суток");
            cycle.SetHour(value);
            EditorUtility.SetDirty(cycle);
            SceneView.RepaintAll();
        }

        /// <summary>Час в вид «23:45»: по дробному числу этого не прочитать.</summary>
        private static string Clock(float hour)
        {
            int h = Mathf.FloorToInt(Mathf.Repeat(hour, 24f));
            int m = Mathf.RoundToInt((Mathf.Repeat(hour, 24f) - h) * 60f);

            if (m >= 60)
            {
                m = 0;
                h = (h + 1) % 24;
            }

            return $"{h:00}:{m:00}";
        }
    }
}
