using UnityEngine;

/// <summary>
/// Base class for Camera Filter Pack effects.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public abstract class CameraFilterBase : MonoBehaviour
{
    [SerializeField] protected Shader _shader;
    [SerializeField] protected float _intensity = 1f;

    protected Material _material;
    protected float _timeX = 1f;

    protected virtual void Start()
    {
        if (_shader == null)
        {
            _shader = Shader.Find(GetShaderName());
        }
        
        if (!SystemInfo.supportsImageEffects)
        {
            enabled = false;
            return;
        }
    }

    protected virtual void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_shader != null)
        {
            _timeX += Time.deltaTime;
            if (_timeX > 100f) _timeX = 0f;

            if (_material == null)
            {
                _material = new Material(_shader);
                _material.hideFlags = HideFlags.HideAndDontSave;
            }

            _material.SetFloat("_TimeX", _timeX);
            _material.SetFloat("_Intensity", _intensity);
            UpdateMaterialProperties(_material);

            Graphics.Blit(source, destination, _material);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    protected virtual void OnDisable()
    {
        if (_material != null)
        {
            DestroyImmediate(_material);
        }
    }

    protected abstract string GetShaderName();
    protected abstract void UpdateMaterialProperties(Material material);
}