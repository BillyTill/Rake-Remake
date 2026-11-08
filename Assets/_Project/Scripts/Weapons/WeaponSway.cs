using UnityEngine;
using Lake.Core;

namespace Lake.Weapons
{
    /// <summary>Визуальный sway модели. НЕ влияет на raycast.</summary>
    public class WeaponSway : MonoBehaviour
    {
        [SerializeField] float swayAmount = 0.04f;
        [SerializeField] float swaySmoothing = 6f;

        Vector3 _basePos;
        InputProvider _input;

        void Awake() => _basePos = transform.localPosition;
        void Start() => _input = InputProvider.Instance;

        void Update()
        {
            if (_input == null) return;
            Vector3 target = _basePos + new Vector3(
                -_input.LookInput.x * swayAmount,
                -_input.LookInput.y * swayAmount, 0f);
            transform.localPosition = Vector3.Lerp(transform.localPosition, target,
                Time.deltaTime * swaySmoothing);
        }
    }
}