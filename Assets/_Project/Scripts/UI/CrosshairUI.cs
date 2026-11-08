using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Lake.UI
{
    public class CrosshairUI : MonoBehaviour
    {
        [SerializeField] RectTransform top, bottom, left, right;
        [SerializeField] Image dot;
        [SerializeField] float baseGap = 6f;
        [SerializeField] float gapPerDegree = 2f;

        public void SetVisible(bool v)
        {
            gameObject.SetActive(v);
        }

        public void SetSpread(float spreadDegrees)
        {
            float gap = baseGap + spreadDegrees * gapPerDegree;
            if (top != null) top.anchoredPosition = new Vector2(0, gap);
            if (bottom != null) bottom.anchoredPosition = new Vector2(0, -gap);
            if (left != null) left.anchoredPosition = new Vector2(-gap, 0);
            if (right != null) right.anchoredPosition = new Vector2(gap, 0);
        }

        public void SetDimmed(bool dimmed)
        {
            float alpha = dimmed ? 0.15f : 0.7f;
            foreach (var img in GetComponentsInChildren<Image>())
                img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
        }
    }
}