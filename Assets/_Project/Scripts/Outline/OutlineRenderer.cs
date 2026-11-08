using UnityEngine;
using UnityEngine.Rendering;

namespace Lake.Outline
{
    /// <summary>
    /// Создаёт дубликат меша с увеличенным масштабом и unlit-материалом.
    /// Cull Front даёт эффект обводки (видна только "изнаночная" часть).
    /// Если HDRP/Unlit не поддерживает cull front через _CullMode — 
    /// дубликат рендерится как сплошное свечение вокруг объекта (приемлемо).
    /// </summary>
    public class OutlineRenderer : System.IDisposable
    {
        public Highlightable Target { get; }
        GameObject _clone;
        Renderer[] _originalRenderers;
        Material _outlineMat;

        public OutlineRenderer(Highlightable target) { Target = target; }

        public void Create()
        {
            _originalRenderers = Target.GetComponentsInChildren<Renderer>();
            if (_originalRenderers.Length == 0) { Debug.LogWarning("[Outline] No renderer found"); return; }

            Shader shader = Shader.Find("HDRP/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) { Debug.LogError("[Outline] No unlit shader found"); return; }

            _outlineMat = new Material(shader);
            Color c = Target.HighlightColor;
            // HDRP/Unlit
            if (_outlineMat.HasProperty("_UnlitColor")) _outlineMat.SetColor("_UnlitColor", c);
            if (_outlineMat.HasProperty("_UnlitColorMap")) _outlineMat.SetColor("_UnlitColorMap", c);
            if (_outlineMat.HasProperty("_BaseColor")) _outlineMat.SetColor("_BaseColor", c);
            if (_outlineMat.HasProperty("_Color")) _outlineMat.SetColor("_Color", c);

            // Cull front — инвертированные нормали
            if (_outlineMat.HasProperty("_CullMode")) _outlineMat.SetFloat("_CullMode", 1f);
            if (_outlineMat.HasProperty("_CullModeForward")) _outlineMat.SetFloat("_CullModeForward", 1f);
            if (_outlineMat.HasProperty("_DoubleSidedEnable")) _outlineMat.SetFloat("_DoubleSidedEnable", 1f);

            _outlineMat.renderQueue = 3000; // Transparent

            _clone = new GameObject($"[Outline] {Target.name}");
            _clone.transform.SetParent(Target.transform.parent);
            _clone.transform.position = Target.transform.position;
            _clone.transform.rotation = Target.transform.rotation;
            _clone.transform.localScale = Target.transform.localScale;

            // Копируем только меши (без коллайдеров и других компонентов)
            foreach (var r in _originalRenderers)
            {
                if (r is MeshRenderer mr && mr.TryGetComponent<MeshFilter>(out var mf))
                {
                    var child = new GameObject("outline_mesh");
                    child.transform.SetParent(_clone.transform);
                    child.transform.position = mr.transform.position;
                    child.transform.rotation = mr.transform.rotation;
                    child.transform.localScale = mr.transform.localScale * Target.ScaleMultiplier;
                    var nMF = child.AddComponent<MeshFilter>();
                    nMF.sharedMesh = mf.sharedMesh;
                    var nMR = child.AddComponent<MeshRenderer>();
                    nMR.sharedMaterial = _outlineMat;
                    nMR.shadowCastingMode = ShadowCastingMode.Off;
                    nMR.receiveShadows = false;
                }
                else if (r is SkinnedMeshRenderer smr)
                {
                    // Для скиннованных мешей — копия проще не делается, пропускаем
                    // (в прототипе интерактивные объекты — примитивы, меш-рендереры)
                }
            }
        }

        public void Dispose()
        {
            if (_clone != null) Object.Destroy(_clone);
            if (_outlineMat != null) Object.Destroy(_outlineMat);
            _clone = null;
            _outlineMat = null;
        }
    }
}