using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Lake.Clouds
{
    /// <summary>
    /// Проход объёмных облаков для HDRP 17.
    ///
    /// Заменяет OnRenderImage из оригинального пакета: в Unity 6.3 этого
    /// обратного вызова нет. Типы ScriptableRenderPass и RenderPipelineFeature
    /// в HDRP 17 тоже удалены, а вместе с ними — HDAdditionalRendererData.
    /// Реестр кастом-проходов теперь живёт в компоненте
    /// <see cref="CustomPassVolume"/>: он сам держит список пассов и точку
    /// внедрения, и пассы находятся поиском по всем томам сцены.
    ///
    /// Схема отрисовки повторяет оригинальную:
    ///   1. рейтрейс в собственный RT в половинном разрешении;
    ///   2. temporal-блендинг с предыдущим кадром (репроекция по _LastVP);
    ///   3. апсэмпл и склейка с цветом камеры.
    ///
    /// Внедряется в AfterOpaqueAndSky: небо и непрозрачная геометрия уже
    /// нарисованы, буфер глубины доступен, прозрачные объекты ещё не начались.
    /// </summary>
    [System.Serializable]
    public sealed class CloudCustomPass : CustomPass
    {
        [Tooltip("Настройки облаков: плотность, свет, текстуры шума")]
        public CloudSettings settings;

        [Range(0.25f, 1f)]
        [Tooltip("Доля полного разрешения для рейтрейса. Ниже — быстрее и мыльнее.")]
        public float marchScale = 0.5f;

        [Tooltip("Смешивать с предыдущим кадром для устранения мыла при движении")]
        public bool temporalUpsampling = true;

        [Range(0.5f, 1f)]
        [Tooltip("Доля нового кадра. 1 — без сглаживания, 0.5 — сильное размытиеmotion")]
        public float temporalBlendFactor = 0.9f;

        private RTHandle _cloudRT;
        private RTHandle _historyRT;
        private Matrix4x4 _lastViewProjection;
        private bool _hasLastFrame;

        private static readonly int LastVpId = Shader.PropertyToID("_LastVP");
        private static readonly int LastCloudTexId = Shader.PropertyToID("_LastCloudTex");
        private static readonly int UseTemporalId = Shader.PropertyToID("_useTemporalUpsampling");
        private static readonly int TemporalBlendId = Shader.PropertyToID("_temporalBlendFactor");
        private static readonly int CloudTexId = Shader.PropertyToID("_CloudTexture");
        private static readonly int CameraColorId = Shader.PropertyToID("_CameraColorTexture");
        private static readonly int CloudBlendId = Shader.PropertyToID("_CloudBlend");

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
        }

        protected override void Execute(CustomPassContext ctx)
        {
            if (settings == null || !enabled)
                return;

            var raymarchMaterial = settings.RenderingMaterial;
            var compositeMaterial = settings.BlendingMaterial;
            if (raymarchMaterial == null || compositeMaterial == null)
                return;

            var camera = ctx.hdCamera.camera;
            if (camera == null)
                return;

            settings.ApplyToMaterial(raymarchMaterial);

            // ── Буферы облаков в уменьшенном разрешении ─────────────────────
            int width = Mathf.Max(1, (int)(ctx.cameraColorBuffer.rt.width * marchScale));
            int height = Mathf.Max(1, (int)(ctx.cameraColorBuffer.rt.height * marchScale));
            EnsureBuffers(width, height);

            // ── Данные камеры для рейтрейса ─────────────────────────────────
            // GPU-проекция переводит проекцию в соглашение, где дальняя
            // плоскость — z = 1; иначе луч восстанавливается в другую сторону.
            var gpuProjection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
            var viewProjection = gpuProjection * camera.worldToCameraMatrix;

            raymarchMaterial.SetMatrix("_CloudInvViewProj", viewProjection.inverse);
            raymarchMaterial.SetVector("_CloudCameraPosition", camera.transform.position);
            raymarchMaterial.SetVector("_CloudZBufferParams", Shader.GetGlobalVector("_ZBufferParams"));
            raymarchMaterial.SetVector("_CloudTime",
                new Vector4(Time.time, Time.timeSinceLevelLoad, Time.deltaTime, Time.frameCount));
            raymarchMaterial.SetVector("_CloudScreenSize",
                new Vector4(width, height, 1f / width, 1f / height));

            // Глубина сцены: без неё облака рисуются поверх рельефа.
            raymarchMaterial.SetTexture("_CloudDepthTex", ctx.cameraDepthBuffer);
            raymarchMaterial.SetFloat("_UseDepth", ctx.cameraDepthBuffer != null ? 1f : 0f);

            // ── Temporal-блендинг ────────────────────────────────────────────
            bool useTemporal = temporalUpsampling && _hasLastFrame;
            raymarchMaterial.SetFloat(UseTemporalId, useTemporal ? 1f : 0f);
            raymarchMaterial.SetFloat(TemporalBlendId, temporalBlendFactor);
            if (useTemporal)
            {
                raymarchMaterial.SetMatrix(LastVpId, _lastViewProjection);
                raymarchMaterial.SetTexture(LastCloudTexId, _historyRT);
            }

            // ── Проход 1: рейтрейс в RT ─────────────────────────────────────
            CoreUtils.SetRenderTarget(ctx.cmd, _cloudRT, ClearFlag.Color, Color.clear);
            CoreUtils.DrawFullScreen(ctx.cmd, raymarchMaterial, shaderPassId: 0);

            // ── Проход 2: апсэмпл и склейка с цветом камеры ─────────────────
            // Камера в HDRP может быть с MSAA, и читать цвет буфера до
            // разрешения MSAA нельзя — иначе получится мусор.
            ResolveMSAAColorBuffer(ctx.cmd, ctx.hdCamera);
            SetRenderTargetAuto(ctx.cmd);

            compositeMaterial.SetTexture(CameraColorId, ctx.cameraColorBuffer);
            compositeMaterial.SetTexture(CloudTexId, _cloudRT);
            compositeMaterial.SetFloat(CloudBlendId, settings.cloudIntensity);
            CoreUtils.DrawFullScreen(ctx.cmd, compositeMaterial, shaderPassId: 0);

            // ── Сохраняем кадр для следующего temporal-блендинга ────────────
            // Blitter, а не CoreUtils.CopyTexture: копия между RTHandle
            // идёт через блиттер SRP Core, прямого метода копирования там нет.
            Blitter.BlitCameraTexture(ctx.cmd, _cloudRT, _historyRT);
            _lastViewProjection = viewProjection;
            _hasLastFrame = true;
        }

        protected override void Cleanup()
        {
            _cloudRT?.Release();
            _cloudRT = null;
            _historyRT?.Release();
            _historyRT = null;
            _hasLastFrame = false;
        }

        private void EnsureBuffers(int width, int height)
        {
            if (_cloudRT != null && _cloudRT.rt.width == width && _cloudRT.rt.height == height)
                return;

            _cloudRT?.Release();
            _historyRT?.Release();

            _cloudRT = RTHandles.Alloc(width, height,
                colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat,
                filterMode: FilterMode.Bilinear, wrapMode: TextureWrapMode.Clamp, name: "_CloudRT");

            _historyRT = RTHandles.Alloc(width, height,
                colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat,
                filterMode: FilterMode.Bilinear, wrapMode: TextureWrapMode.Clamp, name: "_CloudHistoryRT");

            _hasLastFrame = false;
        }

        /// <summary>
        /// Ставит проход в сцену. Создаёт или дополняет CustomPassVolume —
        /// в HDRP 17 это единственный способ зарегистрировать кастом-пасс.
        /// </summary>
        public static CloudCustomPass Install(CustomPassVolume volume, CloudSettings settings, float scale)
        {
            if (volume == null || settings == null)
                return null;

            for (int i = 0; i < volume.customPasses.Count; i++)
                if (volume.customPasses[i] is CloudCustomPass existing)
                {
                    existing.settings = settings;
                    existing.marchScale = scale;
                    return existing;
                }

            volume.injectionPoint = CustomPassInjectionPoint.AfterOpaqueAndSky;
            volume.isGlobal = true;

            var pass = volume.AddPassOfType<CloudCustomPass>();
            if (pass == null)
                return null;

            pass.settings = settings;
            pass.marchScale = scale;
            return pass;
        }
    }
}