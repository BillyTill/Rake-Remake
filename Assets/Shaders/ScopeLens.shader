Shader "HDRP/ScopeLens"
{
    Properties
    {
        _ScopeRenderTexture("Scope Render Texture", 2D) = "black" {}
        _Reticle("Reticle Texture", 2D) = "white" {}
        _VignetteStrength("Vignette Strength", Range(0, 1)) = 0.6
        _ChromaticAberration("Chromatic Aberration", Range(0, 1)) = 0.15
        _LensDarkening("Lens Darkening", Range(0.5, 1)) = 0.92
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "HDRenderPipeline"
            "UniversalMaterialType" = "Unlit"
        }
        
        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            TEXTURE2D(_ScopeRenderTexture);
            SAMPLER(sampler_ScopeRenderTexture);
            
            TEXTURE2D(_Reticle);
            SAMPLER(sampler_Reticle);

            float _VignetteStrength;
            float _ChromaticAberration;
            float _LensDarkening;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 center = float2(0.5, 0.5);
                float dist = length(uv - center);

                // Маска для круглого окуляра прицела
                float circleMask = 1.0 - smoothstep(0.48, 0.52, dist);

                // Базовый цвет из RenderTexture
                float4 scopeColor = SAMPLE_TEXTURE2D(_ScopeRenderTexture, sampler_ScopeRenderTexture, uv);

                // Хроматическая аберрация (радужное смещение на краях)
                float aberrationAmount = _ChromaticAberration * dist * 0.1;
                float4 chroma;
                chroma.r = SAMPLE_TEXTURE2D(_ScopeRenderTexture, sampler_ScopeRenderTexture, uv + float2(aberrationAmount, 0)).r;
                chroma.g = scopeColor.g;
                chroma.b = SAMPLE_TEXTURE2D(_ScopeRenderTexture, sampler_ScopeRenderTexture, uv - float2(aberrationAmount, 0)).b;
                chroma.a = scopeColor.a;
                scopeColor = lerp(scopeColor, chroma, _ChromaticAberration);

                // Виньетка (затемнение по краям)
                float vignette = 1.0 - (dist * dist) * _VignetteStrength;
                scopeColor.rgb *= vignette;

                // Перекрестие из текстуры
                float4 reticleColor = SAMPLE_TEXTURE2D(_Reticle, sampler_Reticle, uv);
                scopeColor = lerp(scopeColor, reticleColor, reticleColor.a);

                // Общее затемнение линзы (слегка запотевшее стекло)
                scopeColor.rgb *= _LensDarkening;

                // Чёрная шторка за пределами окуляра
                scopeColor.rgb = lerp(float3(0, 0, 0), scopeColor.rgb, circleMask);

                return scopeColor;
            }
            ENDHLSL
        }
    }

    Fallback "Hidden/InternalErrorShader"
}
