// Портированный под SRP/HDRP рейтрейс объёмных облаков.
//
// Оригинал: https://github.com/todoval/CloudsShader (CloudRendering.shader)
// Написан под встроенный рендер-пайплайн: CGPROGRAM, UnityCG.cginc,
// UnityObjectToClipPos, OnRenderImage и Graphics.Blit. В HDRP этого нет
// ни в шейдере, ни в скрипте, поэтому всё переписано.
//
// Алгоритм рейтрейса сохранён без изменений (по статье Haggstrom и
// "Convincing Cloud Rendering" Rurik Hogfeldt): AABB-вход в контейнер,
// марш через слой, марш к источнику света, фаза Хенье-Гринштейна,
// powder-эффект Horizon Zero Dawn, синий шум для джиттера.
//
// Отличия от оригинала — только инфраструктурные:
//   * CGPROGRAM -> HLSLPROGRAM, target 4.5
//   * глобалы камеры передаются из RenderPass явными параметрами
//     (_CloudCameraPosition, _CloudInvViewProj, _CloudZBufferParams),
//     потому что имена в HDRP другие и зависеть от них хрупко
//   * глубина линеаризуется через LinearEyeDepth из SRP Core
//   * шейдер ничего не блитит: он считает только облака и отдаёт
//     premultiplied-цвет в альфе — склейку сценой делает композитный проход

// Имя шейдера сохранено оригинальным, "CloudRendering", — чтобы настройки из
// пакета todoval/CloudsShader работали без переназначения в инспекторе.
Shader "CloudRendering"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    // SpaceTransforms.hlsl не используется: он требует UNITY_MATRIX_M из
    // UnityInput.hlsl, а этого файла в этой версии SRP Core нет вообще.
    // Матрицы объекта и камеры передаются из C# собственными параметрами.

    #ifndef PI
        #define PI 3.141592653
    #endif

    // ── Свойства облака ───────────────────────────────────────────────────
    float4  _cloudColor;
    float   _speed;
    float   _tileSize;
    float   _absorptionCoef;

    // ── Свойства формы ────────────────────────────────────────────────────
    float   _detailAmount;
    float   _maxDetailModifier;
    float   _densityConstant;
    float   _cloudMaxHeight;
    float   _cloudHeightModifier;
    float   _cloudBottomModifier;

    // ── Производительность ────────────────────────────────────────────────
    bool    _useBlueNoiseRay;
    bool    _useBlueNoiseLight;
    float   _blueNoiseLightAmount;
    float   _blueNoiseRayAmount;
    int     _lightMarchSteps;
    float   _rayMarchStepSize;
    float   _lightMarchDecrease;
    float   _rayMarchDecrease;

    // ── Контейнер ─────────────────────────────────────────────────────────
    float3  containerBound_Min;
    float3  containerBound_Max;

    // ── Свет ──────────────────────────────────────────────────────────────
    bool    _useLight;
    float3  _lightPosition;
    float4  _lightColor;
    float   _lightIntensity;

    float   _henyeyCoeff;
    float   _henyeyRatio;
    float   _henyeyIntensity;

    float   _powderCoeff;
    float   _powderAmount;
    float   _powderIntensity;

    // ── Данные от RenderPass ──────────────────────────────────────────────
    float3  _CloudCameraPosition;
    float4x4 _CloudInvViewProj;
    float4  _CloudZBufferParams;
    float4  _CloudDepthParams;   // x = дальность марша в контейнере
    float   _UseDepth;           // 0 — не проверять перекрытие геометрией

    // Размер кадра нужен, чтобы перевести SV_POSITION в экранные UV для
    // выборки глубины: на кубе UV вершины — это развёртка грани, а не экран.
    float4  _CloudScreenSize;

    // Temporal-блендинг с предыдущим кадром: оригинальная схема пакета.
    float4x4 _LastVP;
    int     _useTemporalUpsampling;
    float   _temporalBlendFactor;
    TEXTURE2D(_LastCloudTex); SAMPLER(sampler_LastCloudTex);

    // Время передаётся из C#: переменная _Time есть только во встроенном
    // рендер-пайплайне, в SRP Core она не объявлена и шейдер не собирается.
    float4  _CloudTime;

    // Шум формы и детализации — ОБЪЁМНЫЕ текстуры Texture3D. Объявлять их
    // как TEXTURE2D нельзя: сэмплинг идёт через SAMPLE_TEXTURE3D_LOD,
    // а при несовпадении типа Texture3D молча не привязывается к материалу
    // и в кадре появляется маджента — цвет отсутствующей текстуры.
    TEXTURE3D(_ShapeTexture);  SAMPLER(sampler_ShapeTexture);
    TEXTURE3D(_DetailTexture); SAMPLER(sampler_DetailTexture);
    TEXTURE2D(_WeatherMap);     SAMPLER(sampler_WeatherMap);
    TEXTURE2D(_BlueNoise);      SAMPLER(sampler_BlueNoise);
    TEXTURE2D(_CloudDepthTex);  SAMPLER(sampler_CloudDepthTex);

    struct rayContainerInfo
    {
        bool  intersectedBox;
        float dstInsideBox;
        float dstToBox;
    };

    struct raymarchInfo
    {
        float  transmittance;
        float4 density;
    };

    // AABB: луч стартует в точке rayOrigin и идёт в направлении rayDir.
    rayContainerInfo getRayContainerInfo(float3 rayOrigin, float3 rayDir)
    {
        // В оригинале направление делилось на ненулевой вектор; мы
        // приводим его к единичному один раз, чтобы tA/tB были расстояниями.
        rayDir = rayDir / max(1e-5, length(rayDir));

        float3 tA = (containerBound_Min - rayOrigin) / rayDir;
        float3 tB = (containerBound_Max - rayOrigin) / rayDir;

        float3 tmin = min(tA, tB);
        float3 tmax = max(tA, tB);

        float dstFirst  = max(max(tmin.x, tmin.y), tmin.z);
        float dstSecond = min(min(tmax.x, tmax.y), tmax.z);

        rayContainerInfo info;
        info.intersectedBox = (dstFirst > dstSecond) ? false : true;
        info.dstToBox      = max(0, dstFirst);
        info.dstInsideBox  = max(0, dstSecond - info.dstToBox);
        return info;
    }

    bool isInsideBox(float3 position)
    {
        // Оригинал передавал сюда ненормализованную позицию источника света.
        // Для AABB важно только направление, поэтому приводим к единичному.
        float3 dir = normalize(_lightPosition);
        return getRayContainerInfo(position, dir).dstInsideBox > 0;
    }

    float getDistance(float3 A, float3 B)
    {
        return sqrt(pow(A.x - B.x, 2) + pow(A.y - B.y, 2) + pow(A.z - B.z, 2));
    }

    float remap(float value, float originalMin, float originalMax, float newMin, float newMax)
    {
        return newMin + (((value - originalMin) / (originalMax - originalMin)) * (newMax - newMin));
    }

    // Скругление слоя: снизу слегка, сверху сильно.
    float heightAlter(float percentHeight, float heightValue)
    {
        float bottomRound = saturate(remap(percentHeight, 0.0, _cloudBottomModifier, 0.0, 1.0));
        float stopHeight  = saturate(heightValue + _cloudMaxHeight);
        float topRound    = saturate(remap(percentHeight, stopHeight * _cloudHeightModifier, stopHeight, 1.0, 0.0));
        return topRound * bottomRound;
    }

    float densityAlter(float percentHeight, float cloudType)
    {
        float bottomDensity = saturate(remap(percentHeight, 0.0, 0.2, 0.0, 1.0));
        float topDensity    = saturate(remap(percentHeight, 0.9, 1.0, 1.0, 0.0));
        return percentHeight * bottomDensity * topDensity * cloudType * _densityConstant;
    }

    float getDensity(float3 position)
    {
        // _CloudTime — float4, и в оригинале пакета samplePos брался как float3,
        // то есть молча отбрасывался компонент w. Явно отрезаем .xyz.
        float3 samplePos = (position + _CloudTime.xyz * _speed) / _tileSize;

        if (!isInsideBox(position))
            return 0;

        float4 weatherValue = SAMPLE_TEXTURE2D_LOD(_WeatherMap, sampler_WeatherMap, float2(samplePos.x, samplePos.z), 0);
        float heightValue    = weatherValue.g;
        float globalDensity  = weatherValue.b;
        float cloudCoverage  = weatherValue.r;

        float4 shapeDensity = SAMPLE_TEXTURE3D_LOD(_ShapeTexture, sampler_ShapeTexture, samplePos, 0);
        float shapeNoise = shapeDensity.g * 0.625 + shapeDensity.b * 0.25 + shapeDensity.a * 0.125;
        shapeNoise = -(1 - shapeNoise);
        shapeNoise = remap(shapeDensity.r, shapeNoise, 1.0, 0.0, 1.0);

        float heightPercentage = (position.y - containerBound_Min.y) / (containerBound_Max.y - containerBound_Min.y);

        float4 detailDensity = SAMPLE_TEXTURE3D_LOD(_DetailTexture, sampler_DetailTexture, samplePos, 0);
        float detailNoise = detailDensity.r * 0.625 + detailDensity.g * 0.25 + detailDensity.a * 0.125;
        float detailModifier = lerp(detailNoise, 1 - detailNoise, saturate(heightPercentage));
        detailModifier *= _detailAmount * exp(-_maxDetailModifier);

        float heightModifier = heightAlter(heightPercentage, heightValue);
        float densityFactor  = densityAlter(heightPercentage, globalDensity);

        float shapeND = saturate(remap(heightModifier * shapeNoise, 1 - cloudCoverage, 1.0, 0.0, 1.0));
        float finalDensity = saturate(remap(shapeND, detailModifier, 1.0, 0.0, 1.0));

        return finalDensity * densityFactor;
    }

    float getHenyeyGreenstein(float cosAngle, float g)
    {
        return (1 - g * g) / PI * sqrt(pow(max(0.0, 1 + g * g - 2 * g * cosAngle), 3));
    }

    float phaseFunction(float cosAngle)
    {
        float hg = getHenyeyGreenstein(cosAngle, _henyeyCoeff);
        return (1 - _henyeyRatio) + hg * _henyeyRatio * _henyeyIntensity;
    }

    float powderEffect(float depth)
    {
        return (1 - exp(-_powderCoeff * 2 * depth)) * _powderAmount * _powderIntensity + (1 - _powderAmount);
    }

    float lightmarch(float3 pos, float heightPercentage)
    {
        float3 dirVector = float3(_lightPosition.x, _lightPosition.y, _lightPosition.z) - pos;
        dirVector = dirVector / length(dirVector);

        rayContainerInfo containerInfo = getRayContainerInfo(_lightPosition, -dirVector);
        float3 entryPoint = _lightPosition - dirVector * containerInfo.dstToBox;

        float3 currPoint = pos;
        float distanceToMarch = getDistance(entryPoint, pos) * heightPercentage;
        float noOfSteps = (float)_lightMarchSteps;
        float stepDecrease = _lightMarchDecrease;
        float stepSize = distanceToMarch / max(1.0, noOfSteps);

        if (noOfSteps == 2)
            stepSize = distanceToMarch / (1 + stepDecrease);
        else if (noOfSteps == 3)
            stepSize = distanceToMarch / (1 + stepDecrease + pow(stepDecrease, 2));
        else if (noOfSteps == 4)
            stepSize = distanceToMarch / (1 + stepDecrease + pow(stepDecrease, 2) + pow(stepDecrease, 3));

        float accumDensity = 0;
        float transmittance = 1;

        while (noOfSteps > 0)
        {
            float density = getDensity(currPoint);
            if (density > 0)
                accumDensity += density * stepSize;
            else
                break;

            currPoint += dirVector * stepSize;
            stepSize *= stepDecrease;
            noOfSteps--;
        }

        float lightAttenuation = exp(-accumDensity * _absorptionCoef);
        return lightAttenuation * _lightIntensity;
    }

    float3 getIncidentLighting(float3 pos, float3 incVector, float currDensity)
    {
        float heightPercentage = (pos.y - containerBound_Min.y) / (containerBound_Max.y - containerBound_Min.y);

        float lightFromSun = lightmarch(pos, heightPercentage);
        lightFromSun *= powderEffect(currDensity);

        float phaseVal = 1;
        if (_henyeyRatio > 0)
        {
            float3 lightVector = pos - float3(_lightPosition.x, _lightPosition.y, _lightPosition.z);
            lightVector = lightVector / length(lightVector);
            float cosAngle = dot(normalize(incVector), lightVector);
            phaseVal = phaseFunction(cosAngle);
        }
        // Усечение float3 -> float: оригинал пакета присваивал float3 в float,
        // и цвет света схлопывался до одного канала — лунный голубой
        // вырождался в красный. Теперь цвет доходит до облаков целиком.
        float3 incLight = phaseVal * lightFromSun * _lightColor.rgb;

        if (_useBlueNoiseLight)
        {
            float4 blueNoise = SAMPLE_TEXTURE2D_LOD(_BlueNoise, sampler_BlueNoise, float2(pos.x, pos.y), 0);
            float bnVal = saturate(blueNoise.x + blueNoise.y + blueNoise.z) / 3;
            incLight += bnVal * _blueNoiseLightAmount;
        }
        return incLight;
    }

    raymarchInfo raymarch(float3 entryPoint, float3 rayDir)
    {
        float stepSize = _rayMarchStepSize;
        if (stepSize == 0 || _rayMarchDecrease == 0)
        {
            raymarchInfo r;
            r.transmittance = 0;
            r.density = 0;
            return r;
        }

        float transmittance = 1;
        float4 totalDensity = float4(0, 0, 0, 0);
        float3 currPoint = entryPoint;

        if (_useBlueNoiseRay)
        {
            float4 blueNoise = SAMPLE_TEXTURE2D_LOD(_BlueNoise, sampler_BlueNoise, float2(entryPoint.x, entryPoint.y), 0);
            float bnVal = saturate(blueNoise.x + blueNoise.y + blueNoise.z) / 3;
            currPoint += rayDir * ((bnVal * _blueNoiseRayAmount - 0.5) * stepSize);
        }

        // Ограничиваем марш расстоянием контейнера: иначе луч, вошедший
        // в куб, продолжал бы шагать за его пределами.
        float maxDistance = getRayContainerInfo(entryPoint, rayDir).dstInsideBox;
        if (maxDistance <= 0)
        {
            raymarchInfo r;
            r.transmittance = 1;
            r.density = 0;
            return r;
        }

        float travelled = 0;
        while (travelled < maxDistance)
        {
            float density = getDensity(currPoint);
            if (density > 0)
            {
                float3 incLight = 0;
                if (_useLight)
                    incLight = getIncidentLighting(currPoint, rayDir, density);

                float deltaT = exp(-_absorptionCoef * stepSize * density);
                transmittance *= deltaT;
                if (transmittance < 0.01)
                    break;

                totalDensity += float4(density * transmittance * incLight, 0.0);
            }

            currPoint += rayDir * stepSize;
            travelled += stepSize;
            stepSize *= _rayMarchDecrease;
        }

        raymarchInfo result;
        result.transmittance = transmittance;
        result.density = totalDensity * _absorptionCoef;
        return result;
    }

    ENDHLSL

    SubShader
    {
        // Прозрачная очередь обязательна. В непрозрачной материал просто
        // затирал кадр собственным цветом, и контейнер читался как плоская
        // геометрия с резкими гранями, а не как облака.
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

        Cull Off       // камера внутри куба — видны внутренние грани
        ZWrite Off
        ZTest Always

        // Шейдер отдаёт premultiplied-цвет: rgb уже умножен на плотность,
        // в альфе — прозрачность. Поэтому Src = One, а не SrcAlpha.
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "CloudRaymarch"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                // Полноэкранный треугольник: клип-позиция строится прямо из UV.
                o.positionCS = float4(v.uv * 2 - 1, 0, 1);
                o.uv = v.uv;
                return o;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                // Луч восстанавливается обратной проекцией: точка на дальней
                // плоскости минус позиция камеры. В оригинале то же делалось
                // через unity_CameraInvProjection.
                float4 farPoint = mul(_CloudInvViewProj, float4(i.uv * 2 - 1, 1, 1));
                float3 farWorld = farPoint.xyz / farPoint.w;
                float3 rayOrigin = _CloudCameraPosition;
                float3 rayDir = normalize(farWorld - rayOrigin);

                // Если перед облаками есть геометрия ближе контейнера —
                // облака там не рисуются.
                // Проверка выключается флагом: без привязанной текстуры
                // глубины сэмпл возвращает мусор и облака просто исчезают.
                float sceneDepth = 1e9;
                if (_UseDepth > 0.5)
                {
                    float deviceDepth = SAMPLE_TEXTURE2D_LOD(_CloudDepthTex, sampler_CloudDepthTex, i.uv, 0).r;
                    sceneDepth = LinearEyeDepth(deviceDepth, _CloudZBufferParams);
                }

                rayContainerInfo containerInfo = getRayContainerInfo(rayOrigin, rayDir);
                if (!containerInfo.intersectedBox || containerInfo.dstToBox > sceneDepth)
                    return float4(0, 0, 0, 0);

                float3 entryPoint = rayOrigin + rayDir * containerInfo.dstToBox;
                raymarchInfo info = raymarch(entryPoint, rayDir);

                // Premultiplied: композитный проход умножит сцену на
                // transmittance и прибавит это значение.
                float3 cloudColor = info.density.rgb * _cloudColor.rgb;
                float4 result = float4(cloudColor, info.transmittance);

                if (_useTemporalUpsampling != 0)
                {
                    // Репроекция: берём середину отрезка, пройденного лучом
                    // внутри контейнера, переводим её в clip-пространство
                    // ПРОШЛОГО кадра и смотрим, что там было.
                    float3 endPoint = entryPoint + containerInfo.dstInsideBox * rayDir;
                    float4 reprojectionPoint = float4((entryPoint + endPoint) * 0.5, 1);
                    float4 lastClip = mul(_LastVP, reprojectionPoint);
                    float2 lastUV = float2(lastClip.x / lastClip.w, lastClip.y / lastClip.w) * 0.5 + 0.5;

                    // За границами экрана прошлого кадра данных нет —
                    // там берём только текущий кадр, иначе по краям
                    // появляется размытая кайма.
                    float blend = _temporalBlendFactor;
                    if (lastUV.x < 0 || lastUV.x > 1 || lastUV.y < 0 || lastUV.y > 1)
                        blend = 1.0;

                    float4 lastFrameCol = SAMPLE_TEXTURE2D_LOD(_LastCloudTex, sampler_LastCloudTex, lastUV, 0);
                    result = result * blend + lastFrameCol * (1 - blend);
                }

                return result;
            }
            ENDHLSL
        }
    }

    Fallback Off
}