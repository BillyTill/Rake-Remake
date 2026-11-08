using System.Collections;
using UnityEngine;
using TMPro;

namespace Lake.UI
{
    public class DeathScreenUI : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text countdown;

        Coroutine _routine;

        public void Show(float respawnDelay)
        {
            gameObject.SetActive(true);
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ShowRoutine(respawnDelay));
        }

        IEnumerator ShowRoutine(float delay)
        {
            title.text = "SIGNAL LOST";
            float t = 0;
            group.alpha = 0;
            while (t < 1f) { t += Time.deltaTime * 2f; group.alpha = t; yield return null; }

            t = delay;
            while (t > 0)
            {
                countdown.text = $"RECONNECTING... {Mathf.CeilToInt(t)}";
                t -= Time.deltaTime;
                yield return null;
            }
        }

        public void Hide()
        {
            if (_routine != null) StopCoroutine(_routine);
            gameObject.SetActive(false);
        }
    }
}