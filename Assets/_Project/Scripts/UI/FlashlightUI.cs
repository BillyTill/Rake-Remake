using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Lake.UI
{
    public class FlashlightUI : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text status;
        [SerializeField] Image batteryFill;
        [SerializeField] Color onColor = new(1f, 0.9f, 0.4f, 1f);
        [SerializeField] Color offColor = new(1f, 1f, 1f, 0.3f);

        public void SetState(bool on)
        {
            status.text = on ? "FLASHLIGHT ON" : "FLASHLIGHT OFF";
            status.color = on ? onColor : offColor;
            icon.color = on ? onColor : offColor;
        }

        public void SetBattery(float level01)
        {
            batteryFill.fillAmount = level01;
            batteryFill.color = level01 > 0.2f ? onColor : new Color(1f, 0.3f, 0.2f);
        }
    }
}