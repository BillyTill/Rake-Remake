using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Lake.UI
{
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] Image fill;
        [SerializeField] TMP_Text valueLabel;

        public void SetHealth(float current, float max)
        {
            float pct = max > 0 ? current / max : 0f;
            fill.fillAmount = pct;
            fill.color = Color.Lerp(new Color(0.8f, 0.1f, 0.1f), new Color(0.2f, 0.9f, 0.3f), pct);
            valueLabel.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }
}