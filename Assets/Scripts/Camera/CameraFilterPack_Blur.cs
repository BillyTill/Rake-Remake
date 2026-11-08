using UnityEngine;

/// <summary>
/// Blur effect for camera views (motion blur, depth of field, etc).
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Camera Filter Pack/Blur")]
public class CameraFilterPack_Blur : CameraFilterBase
{
    public enum BlurMode
    {
        Gaussian,
        Motion,
        Radial,
        Box
    }

    [SerializeField] private BlurMode _mode = BlurMode.Gaussian;
    [Range(0f, 10f)]
    [SerializeField] private float _blurSize = 2f;
    [Range(1, 8)]
    [SerializeField] private int _downsample = 2;

    protected override string GetShaderName()
    {
        return "Hidden/CameraFilterPack/Blur";
    }

    protected override void UpdateMaterialProperties(Material material)
    {
        material.SetFloat("_BlurSize", _blurSize);
        material.SetInt("_Mode", (int)_mode);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_shader != null && _blurSize > 0f)
        {
            if (_material == null)
            {
                _material = new Material(_shader);
                _material.hideFlags = HideFlags.HideAndDontSave;
            }

            int rtW = source.width / _downsample;
            int rtH = source.height / _downsample;

            RenderTexture temp1 = RenderTexture.GetTemporary(rtW, rtH, 0, source.format);
            temp1.filterMode = FilterMode.Bilinear;

            Graphics.Blit(source, temp1, _material, (int)_mode);

            if (_mode == BlurMode.Gaussian || _mode == BlurMode.Box)
            {
                RenderTexture temp2 = RenderTexture.GetTemporary(rtW, rtH, 0, source.format);
                temp2.filterMode = FilterMode.Bilinear;

                Graphics.Blit(temp1, temp2, _material, (int)_mode + 4); // Second pass
                Graphics.Blit(temp2, destination);

                RenderTexture.ReleaseTemporary(temp2);
            }
            else
            {
                Graphics.Blit(temp1, destination);
            }

            RenderTexture.ReleaseTemporary(temp1);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}