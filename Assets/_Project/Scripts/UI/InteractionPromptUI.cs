using UnityEngine;
using TMPro;
using Lake.Core;

namespace Lake.UI
{
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text label;
        [SerializeField] string keyLabel = "E";
        [SerializeField] float fadeSpeed = 8f;

        IInteractable _current;
        float _alpha;

        public void SetTarget(IInteractable target, string promptText)
        {
            _current = target;
            if (target != null && !string.IsNullOrEmpty(promptText))
                label.text = $"[<color=#FFD54F>{keyLabel}</color>] {promptText}";
        }

        void Update()
        {
            float target = _current != null ? 1f : 0f;
            _alpha = Mathf.MoveTowards(_alpha, target, Time.deltaTime * fadeSpeed);
            group.alpha = _alpha;
        }
    }
}