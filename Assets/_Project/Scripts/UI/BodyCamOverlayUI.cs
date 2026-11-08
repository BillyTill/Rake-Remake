using UnityEngine;
using TMPro;

namespace Lake.UI
{
    public class BodyCamOverlayUI : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text recLabel;
        [SerializeField] TMP_Text timecodeLabel;
        [SerializeField] TMP_Text dateLabel;
        [SerializeField] TMP_Text camIdLabel;
        [SerializeField] TMP_Text batteryLabel;
        [SerializeField] float recBlinkHz = 1.5f;

        bool _active;
        float _t;

        void Start()
        {
            camIdLabel.text = "CAM-01  //  PATROL";
            dateLabel.text = System.DateTime.Now.ToString("yyyy-MM-dd");
        }

        public void SetBodycamActive(bool v)
        {
            _active = v;
            group.alpha = v ? 1f : 0f;
        }

        void Update()
        {
            if (!_active) return;
            _t += Time.deltaTime;

            // REC мигает
            float blink = Mathf.Sin(_t * recBlinkHz * Mathf.PI * 2f);
            recLabel.color = new Color(1f, 0.1f, 0.1f, blink > 0 ? 1f : 0.2f);

            // Timecode HH:MM:SS:FF (FF = кадры @ 30fps)
            int totalSec = Mathf.FloorToInt(Time.time);
            int frames = Mathf.FloorToInt((Time.time - totalSec) * 30f) % 30;
            timecodeLabel.text = $"{totalSec / 3600:D2}:{(totalSec / 60) % 60:D2}:{totalSec % 60:D2}:{frames:D2}";

            batteryLabel.text = $"{Mathf.CeilToInt(SystemInfo.batteryLevel * 100f)}%";
        }
    }
}