using UnityEngine;

/// <summary>
/// Vintage film effect for camera views.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class Vintage : MonoBehaviour
{
    [SerializeField] private Material _vintageMaterial;
    [SerializeField] private float _vignetteIntensity = 0.5f;
    [SerializeField] private float _grainIntensity = 0.3f;
    [SerializeField] private float _scratchIntensity = 0.2f;

    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_vintageMaterial != null)
        {
            _vintageMaterial.SetFloat("_Vignette", _vignetteIntensity);
            _vintageMaterial.SetFloat("_Grain", _grainIntensity);
            _vintageMaterial.SetFloat("_Scratch", _scratchIntensity);
            _vintageMaterial.SetFloat("_Time", Time.time);
            
            Graphics.Blit(source, destination, _vintageMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}