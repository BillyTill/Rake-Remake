using UnityEngine;

/// <summary>
/// Night Vision camera filter - essential for night gameplay.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Camera Filter Pack/Night Vision")]
public class CameraFilterPack_NightVision : CameraFilterBase
{
    [Range(0f, 1f)]
    [SerializeField] private float _noise = 0.3f;
    [Range(0f, 1f)]
    [SerializeField] private float _brightness = 0.5f;
    [SerializeField] private Color _nightVisionColor = new Color(0f, 1f, 0f, 1f);

    protected override string GetShaderName()
    {
        return "Hidden/CameraFilterPack/NightVision";
    }

    protected override void UpdateMaterialProperties(Material material)
    {
        material.SetFloat("_Noise", _noise);
        material.SetFloat("_Brightness", _brightness);
        material.SetColor("_NightVisionColor", _nightVisionColor);
    }
}