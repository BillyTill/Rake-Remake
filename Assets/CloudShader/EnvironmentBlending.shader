// Склейка облаков со сценой.
//
// Рейтрейс отдаёт premultiplied-цвет облаков и прозрачность в альфе,
// композитный проход собирает итог: result = T * цветСцены + цветОблаков.
// Так делается в оригинальном пакете на EnvironmentBlending.shader,
// но без второго блита: сцену берём прямо из буфера камеры.

// Имя сохранено оригинальным, "EnvironmentBlending".
Shader "EnvironmentBlending"
{
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "CloudComposite"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            TEXTURE2D(_CameraColorTexture); SAMPLER(sampler_CameraColorTexture);
            TEXTURE2D(_CloudTexture);        SAMPLER(sampler_CloudTexture);

            float _CloudBlend;

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
                o.positionCS = float4(v.uv * 2 - 1, 0, 1);
                o.uv = v.uv;
                return o;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                float3 scene = SAMPLE_TEXTURE2D_LOD(_CameraColorTexture, sampler_CameraColorTexture, i.uv, 0).rgb;
                float4 cloud = SAMPLE_TEXTURE2D_LOD(_CloudTexture, sampler_CloudTexture, i.uv, 0);

                // Облака непрозрачные ровно настолько, насколько позволяет
                // их альфа-прозрачность; прозрачные участки не трогают сцену.
                return float4(cloud.a * scene + cloud.rgb * _CloudBlend, 1);
            }
            ENDHLSL
        }
    }

    Fallback Off
}