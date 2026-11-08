#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
// CloudGenerator перенесён в пространство имён Lake.Clouds, чтобы лежать рядом
// с CloudSettings и CloudCustomPass. Редактор из пакета остался нетронутым.
using Lake.Clouds;

[CustomEditor(typeof(CloudGenerator))]
[CanEditMultipleObjects]
public class CloudScript : Editor
{
    // crucial properties, shader fails without them

    private SerializedProperty container;
    private SerializedProperty renderingShader;
    private SerializedProperty blendingShader;

    // main properties
    private SerializedProperty speed;
    private SerializedProperty color;
    private SerializedProperty tileSize;
    private SerializedProperty absorptionCoeff;

    // shape properties
    private SerializedProperty densityConstant;
    private SerializedProperty cloudMaxHeight;
    private SerializedProperty cloudHeightModifier;
    private SerializedProperty detailAmount;
    private SerializedProperty detailModifier;
    private SerializedProperty cloudBottomModifier;

    // lighting properties
    private SerializedProperty lightingType;
    private SerializedProperty sceneLight;

    private SerializedProperty cloudIntensity;

    // phase function properties
    private SerializedProperty phaseType;
    private SerializedProperty henyeyCoeff;
    private SerializedProperty henyeyRatio;

    private SerializedProperty henyeyIntensity;


    // powder effect properties
    private SerializedProperty powderCoeff;
    private SerializedProperty powderAmount;
    private SerializedProperty powderIntensity;


    // performance properties
    private SerializedProperty useBlueNoiseRay;
    private SerializedProperty useBlueNoiseLight;
    private SerializedProperty blueNoiseLightAmount;
    private SerializedProperty blueNoiseRayAmount;
    private SerializedProperty lightMarchSteps;
    private SerializedProperty lightMarchDecrease;
    private SerializedProperty rayMarchDecrease;
    private SerializedProperty rayMarchStepSize;
    private SerializedProperty temporalUpsampling;
    private SerializedProperty blendingCoeff;

    // Своё поле порта: доля разрешения, в котором идёт марш.
    private SerializedProperty marchScale;

    private void OnEnable()
    {
        // Часть полей пакета в этом порту не существует вовсе: шейдеры живут
        // в CloudSettings, а фазы и темпорального сглаживания в шейдере нет.
        // FindProperty возвращает null, и раньше отрисовка падала на этом
        // с NullReferenceException — отсюда проверки в помощниках ниже.
        container = serializedObject.FindProperty("container");
        renderingShader = serializedObject.FindProperty("renderingShader");
        blendingShader = serializedObject.FindProperty("blendingShader");

        // main properties
        absorptionCoeff = serializedObject.FindProperty("absorptionCoeff");
        speed = serializedObject.FindProperty("speed");
        color = serializedObject.FindProperty("color");
        tileSize = serializedObject.FindProperty("tileSize");

        // shape properties
        densityConstant = serializedObject.FindProperty("densityConstant");
        detailAmount = serializedObject.FindProperty("detailAmount");
        detailModifier = serializedObject.FindProperty("detailModifier");
        cloudMaxHeight = serializedObject.FindProperty("cloudMaxHeight");
        cloudHeightModifier = serializedObject.FindProperty("cloudHeightModifier");
        cloudBottomModifier = serializedObject.FindProperty("cloudBottomModifier");

        // lighting properties
        sceneLight = serializedObject.FindProperty("sceneLight");
        lightingType = serializedObject.FindProperty("lightingType");
        cloudIntensity = serializedObject.FindProperty("cloudIntensity");

        // phase function properties
        phaseType = serializedObject.FindProperty("phaseType");
        henyeyCoeff = serializedObject.FindProperty("henyeyCoeff");
        henyeyRatio = serializedObject.FindProperty("henyeyRatio");
        henyeyIntensity = serializedObject.FindProperty("henyeyIntensity");

        // performance properties for ray march
        rayMarchStepSize = serializedObject.FindProperty("rayMarchStepSize");
        rayMarchDecrease = serializedObject.FindProperty("rayMarchDecrease");
        useBlueNoiseRay = serializedObject.FindProperty("useBlueNoiseRay");
        blueNoiseRayAmount = serializedObject.FindProperty("blueNoiseRayAmount");

        // performance settings for light march
        lightMarchSteps = serializedObject.FindProperty("lightMarchSteps");
        lightMarchDecrease = serializedObject.FindProperty("lightMarchDecrease");
        useBlueNoiseLight = serializedObject.FindProperty("useBlueNoiseLight");
        blueNoiseLightAmount = serializedObject.FindProperty("blueNoiseLightAmount");

        // powder effect settings
        powderCoeff = serializedObject.FindProperty("powderCoeff");
        powderAmount = serializedObject.FindProperty("powderAmount");
        powderIntensity = serializedObject.FindProperty("powderIntensity");

        // temporal upsampling properties
        temporalUpsampling = serializedObject.FindProperty("temporalUpsampling");
        blendingCoeff = serializedObject.FindProperty("blendingCoeff");

        marchScale = serializedObject.FindProperty("marchScale");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // the crucial parameters that need to be set by the user
        EditorGUILayout.Space();
        Field(container, "Container");
        Field(renderingShader, "Cloud Rendering Shader", "на CloudSettings");
        Field(blendingShader, "Environment Blending Shader", "на CloudSettings");
        EditorGUILayout.Space();

        // the main properties of the cloud shader
        EditorGUILayout.LabelField("--- Main ---", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        Field(tileSize, "Cloud Size");
        Field(speed, "Speed");
        Field(color, "Color");
        Slider(absorptionCoeff, 0, 1, "Absorption Coefficient");
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();

        // shape properties
        EditorGUILayout.LabelField("--- Shape ---", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        Slider(densityConstant, 0, 5, "Density Multiplier");
        Slider(cloudMaxHeight, 0, 1, "Max Height");
        EditorGUILayout.LabelField("Detail Noise");
        EditorGUI.indentLevel++;
        Slider(detailAmount, 0, 1, "Max Value");
        Slider(detailModifier, 0, 1, "Detail Modifier");
        EditorGUI.indentLevel--;
        EditorGUILayout.LabelField("Roundness");
        EditorGUI.indentLevel++;
        Slider(cloudHeightModifier, 0, 1, "Cloud Top");
        Slider(cloudBottomModifier, 0, 0.2f, "Cloud Bottom");
        EditorGUI.indentLevel--;
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();

        // lighting properties
        EditorGUILayout.LabelField("--- Lighting ---", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        Field(cloudIntensity, "Light Intensity");
        int[] lightOptionsValues = { 0, 1, 2 };
        string[] lightOptionsDisplayed = { "Environmental", "Scene Light", "None" };
        IntPopup(lightingType, "Sun", lightOptionsDisplayed, lightOptionsValues);
        // if scene light is set, let the user pick which light he wants to use
        if (lightingType != null && lightingType.intValue == 1)
        {
            EditorGUI.indentLevel++;
            Field(sceneLight, "Scene Light");
            EditorGUI.indentLevel--;
        }
        int[] phaseOptionsValues = { 0, 1 };
        string[] phaseOptionsDisplayed = { "Henyey-Greenstein", "None" };
        IntPopup(phaseType, "Phase Function", phaseOptionsDisplayed, phaseOptionsValues);
        // if the phase function is chosen, choose its properties.
        // Фазы в шейдере нет — параметры показываем всегда, они работают.
        if (phaseType == null || phaseType.intValue == 0)
        {
            EditorGUI.indentLevel++;
            Slider(henyeyRatio, 0, 1, "Weight");
            Field(henyeyIntensity, "Intensity");
            Slider(henyeyCoeff, -1, 1, "Assymetry Parameter");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.LabelField("Powder Effect");
        EditorGUI.indentLevel++;
        Slider(powderAmount, 0, 1, "Amount");
        Slider(powderIntensity, 0, 50, "Intensity");
        Slider(powderCoeff, 0, 1, "Extinction Coefficient");
        EditorGUI.indentLevel--;
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();

        // the performance parameters
        EditorGUILayout.LabelField("--- Performance ---", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Ray March");
        EditorGUI.indentLevel++;
        Field(rayMarchStepSize, "Step Size");
        Field(rayMarchDecrease, "Step Size Decrease");
        Toggle(useBlueNoiseRay, "Blue Noise");
        // if blue noise for raymarch is chosen, choose its settings
        if (useBlueNoiseRay != null && useBlueNoiseRay.boolValue)
        {
            EditorGUI.indentLevel++;
            Slider(blueNoiseRayAmount, 1, 2, "Amount");
            EditorGUI.indentLevel--;
        }
        EditorGUI.indentLevel--;
        EditorGUILayout.LabelField("Light March");
        EditorGUI.indentLevel++;
        IntSlider(lightMarchSteps, 1, 4, "Step Count");
        Slider(lightMarchDecrease, 1, 10, "Step Size Decrease");
        Toggle(useBlueNoiseLight, "Blue Noise");
        // if blue noise for lightmarch is chosen, choose its settings
        if (useBlueNoiseLight != null && useBlueNoiseLight.boolValue)
        {
            EditorGUI.indentLevel++;
            Slider(blueNoiseLightAmount, 0, 0.5f, "Amount");
            EditorGUI.indentLevel--;
        }
        EditorGUI.indentLevel--;
        Toggle(temporalUpsampling, "Temporal Upsampling", "в порте — Sampling Scale");
        // if temporal upsampling is chosen, choose its settings
        if (temporalUpsampling != null && temporalUpsampling.boolValue)
        {
            EditorGUI.indentLevel++;
            Slider(blendingCoeff, 0, 1, "Blending Coefficient");
            EditorGUI.indentLevel--;
        }
        // Марш идёт в долях разрешения — параметр самого порта, в пакете его нет.
        Slider(marchScale, 0.25f, 1f, "Sampling Scale");
        EditorGUI.indentLevel--;

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Отрисовка поля, которого может не быть в компоненте. Параметры пакета,
    /// переехавшие в CloudSettings или исчезнувшие вместе со старым шейдером,
    /// показываем неактивной строкой: пустая строка выглядела бы как забытое поле.
    /// </summary>
    private static void Field(SerializedProperty property, string label, string where = null)
    {
        if (property != null)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(label));
            return;
        }

        Missing(label, where);
    }

    private static void Slider(SerializedProperty property, float min, float max, string label)
    {
        if (property != null)
        {
            EditorGUILayout.Slider(property, min, max, new GUIContent(label));
            return;
        }

        Missing(label);
    }

    private static void IntSlider(SerializedProperty property, int min, int max, string label)
    {
        if (property != null)
        {
            EditorGUILayout.IntSlider(property, min, max, new GUIContent(label));
            return;
        }

        Missing(label);
    }

    private static void IntPopup(SerializedProperty property, string label, string[] displayed, int[] values)
    {
        if (property != null)
        {
            property.intValue = EditorGUILayout.IntPopup(label, property.intValue, displayed, values);
            return;
        }

        Missing(label);
    }

    private static void Toggle(SerializedProperty property, string label, string where = null)
    {
        if (property != null)
        {
            property.boolValue = EditorGUILayout.Toggle(label, property.boolValue);
            return;
        }

        Missing(label, where);
    }

    private static void Missing(string label, string where = null)
    {
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.LabelField(label, where == null ? "— не используется" : $"— см. {where}");
        }
    }
}
#endif
