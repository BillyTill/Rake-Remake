using UnityEngine;

namespace Lake.BodyCam
{
    [CreateAssetMenu(fileName = "BodyCamConfig", menuName = "Lake/BodyCam Config")]
    public class BodyCamConfigSO : ScriptableObject
    {
        public float lensDistortion = 0.25f;
        public float lensScale = 0.95f;
        public float chromaticAberration = 0.15f;
        public float vignette = 0.35f;
        public float filmGrain = 0.4f;
        public float saturation = -12f;
        public float contrast = 5f;
        public float motionBlur = 0.15f;
        public float bloom = 0.1f;
    }
}