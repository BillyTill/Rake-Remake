using UnityEngine;
using TMPro;
using Lake.Core;

namespace Lake.UI
{
    public class KeyHintsUI : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text label;
        [SerializeField] float fadeSpeed = 10f;

        bool _visible = true;

        void Start()
        {
            label.text =
                "<color=#FFD54F>WASD</color> Move   " +
                "<color=#FFD54F>SHIFT</color> Sprint   " +
                "<color=#FFD54F>CTRL</color> Crouch\n" +
                "<color=#FFD54F>E</color> Interact   " +
                "<color=#FFD54F>F</color> Flashlight   " +
                "<color=#FFD54F>B</color> Bodycam\n" +
                "<color=#FFD54F>1/2</color> Weapon   " +
                "<color=#FFD54F>LMB</color> Fire   " +
                "<color=#FFD54F>RMB</color> Aim   " +
                "<color=#FFD54F>R</color> Reload   " +
                "<color=#FFD54F>G</color> Drop\n" +
                "<color=#FFD54F>TAB</color> Toggle hints";

            if (InputProvider.Instance != null)
                InputProvider.Instance.TabPressed += Toggle;
        }

        void OnDestroy()
        {
            if (InputProvider.Instance != null)
                InputProvider.Instance.TabPressed -= Toggle;
        }

        void Toggle() => _visible = !_visible;

        void Update()
        {
            group.alpha = Mathf.MoveTowards(group.alpha, _visible ? 0.85f : 0f, Time.deltaTime * fadeSpeed);
        }
    }
}