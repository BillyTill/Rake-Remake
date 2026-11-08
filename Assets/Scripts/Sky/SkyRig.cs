using UnityEngine;

namespace Lake.Sky
{
    /// <summary>
    /// Обслуживает ночной небесный риг: купол со звёздами, купол с облаками
    /// и билборд Луны.
    ///
    /// Всё это должно ехать вместе с игроком. Купол — это меш радиусом в
    /// сотни метров, и если он останется в точке спавна, игрок выйдет из-под
    /// него через десять шагов и увидит пустоту. Поэтому риг берёт позицию
    /// камеры каждый кадр и повторяет только её, а поворот не трогает: так
    /// звёзды остаются на своих местах в мире, а купол не «едет» при повороте
    /// головы.
    ///
    /// Облака дополнительно прокручиваются по UV. Скорость задаётся вектором,
    /// потому что в реальности облака над головой и у горизонта смещаются
    /// с разной скоростью из-за перспективы; одно значение на всей карте
    /// выглядит как вращение текстуры, а не как движение воздуха.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkyRig : MonoBehaviour
    {
        [Header("Купола")]
        [Tooltip("Купол со звёздами и Млечным Путём. Должен быть непрозрачным.")]
        public Transform skyDome;

        [Tooltip("Купол с облаками. Полупрозрачный, рисуется поверх звёздного.")]
        public Transform cloudDome;

        [Header("Луна")]
        [Tooltip("Билборд с текстурой Луны. Поворачивается к камере, иначе при взгляде сбоку она схлопнется в линию.")]
        public Transform moon;

        [Tooltip("Насколько Луна видна крупнее настоящей. 1 — реальный угол (0.5°) и её почти не разглядеть.")]
        [Range(1f, 12f)] public float moonSizeScale = 6f;

        [Header("Облака")]
        [Tooltip("Скорость прокрутки облаков по UV, в долях текстуры в секунду. X — по долготе, Y — по высоте.")]
        public Vector2 cloudScrollSpeed = new Vector2(0.0035f, 0.0006f);

        [Tooltip("Смещение по текстуре на старте, чтобы облака не начинались с одного и того же края.")]
        public Vector2 cloudScrollOffset = new Vector2(0.31f, 0.17f);

        [Header("Радиус")]
        [Tooltip("На каком расстоянии от камеры держать купола. Должно быть заметно дальше дальнего отсечения камеры.")]
        public float domeDistance = 600f;

        private Transform _cameraTransform;
        private Material _cloudMaterial;
        private Vector2 _offset;
        private Vector3 _moonOffset;

        private void Awake()
        {
            _offset = cloudScrollOffset;

            if (cloudDome != null)
            {
                var renderer = cloudDome.GetComponent<MeshRenderer>();
                if (renderer != null)
                    _cloudMaterial = renderer.material;
            }

            // Смещение Луны от центра рига. Читается из позиции, заданной автором,
            // чтобы направление на неё задавалось интуитивно перетаскиванием.
            if (moon != null)
                _moonOffset = moon.localPosition;
        }

        private void Start()
        {
            var cam = Camera.main;
            if (cam != null)
                _cameraTransform = cam.transform;
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null)
            {
                var cam = Camera.main;
                if (cam == null)
                    return;
                _cameraTransform = cam.transform;
            }

            float dt = Time.deltaTime;

            // Купола повторяют позицию камеры. Поворот намеренно не копируется:
            // иначе при повороте головы купол крутился бы вместе с камерой и
            // звёзды «стояли на месте» относительно взгляда.
            Vector3 pos = _cameraTransform.position;
            if (skyDome != null)
                skyDome.position = pos;
            if (cloudDome != null)
                cloudDome.position = pos;

            // Облака плывут.
            _offset += cloudScrollSpeed * dt;
            if (_cloudMaterial != null && cloudDome != null)
            {
                Vector2 uv = _offset;
                // По долготе текстура зациклена, и без обёртки появился бы
                // заметный скачок в момент переполнения.
                uv.x = Mathf.Repeat(uv.x, 1f);
                uv.y = Mathf.Repeat(uv.y, 1f);
                _cloudMaterial.mainTextureOffset = uv;
            }

            // Луна всегда лицом к камере.
            if (moon != null)
            {
                moon.position = pos + _moonOffset;
                moon.rotation = Quaternion.LookRotation(pos - moon.position, Vector3.up);
            }
        }

        /// <summary>
        /// Приводит купола к нужному радиусу. Вызывается билдером сцены и вручную
        /// из инспектора: радиус обязан быть дальше отсечения камеры, иначе купол
        /// просто обрежется.
        /// </summary>
        public void ApplyScale(float baseDomeScale)
        {
            if (skyDome != null) skyDome.localScale = Vector3.one * baseDomeScale;
            if (cloudDome != null) cloudDome.localScale = Vector3.one * baseDomeScale * 0.92f;
            if (moon != null) moon.localScale = Vector3.one * moonSizeScale;
        }
    }
}
