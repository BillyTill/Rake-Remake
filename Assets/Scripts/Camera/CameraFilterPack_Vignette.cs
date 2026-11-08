using UnityEngine;

/// <summary>
/// Vignette effect for camera views.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Camera Filter Pack/Vignette")]
public class CameraFilterPack_Vignette : CameraFilterBase
{
    [Range(0f, 1f)]
    [SerializeField] private float _vignette = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float _smoothness = 0.5f;
    [SerializeField] private Color _vignetteColor = Color.black;

    protected override string GetShaderName()
    {
        return "Hidden/CameraFilterPack/Vignette";
    }

    protected override void UpdateMaterialProperties(Material material)
    {
        material.SetFloat("_Vignette", _vignette);
        material.SetFloat("_Smoothness", _smoothness);
        material.SetColor("_VignetteColor", _vignetteColor);
    }
}