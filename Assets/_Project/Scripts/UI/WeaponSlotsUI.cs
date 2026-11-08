using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Lake.UI
{
    public class WeaponSlotsUI : MonoBehaviour
    {
        [System.Serializable]
        public class Slot
        {
            public Image background;
            public TMP_Text label;
            public TMP_Text keyLabel;
        }

        [SerializeField] Slot[] slots = new Slot[2];
        [SerializeField] Color activeColor = new(1f, 0.85f, 0.3f, 0.9f);
        [SerializeField] Color occupiedColor = new(1f, 1f, 1f, 0.4f);
        [SerializeField] Color emptyColor = new(1f, 1f, 1f, 0.1f);

        public void SetSlotOccupied(int index, bool occupied)
        {
            if (index < 0 || index >= slots.Length) return;
            slots[index].background.color = occupied ? occupiedColor : emptyColor;
            slots[index].label.color = occupied ? Color.white : new Color(1, 1, 1, 0.3f);
        }

        public void SetActiveSlot(int index)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                bool active = i == index;
                slots[i].background.color = active ? activeColor :
                                            slots[i].background.color.a > 0.05f ? occupiedColor : emptyColor;
                slots[i].background.rectTransform.localScale = active ? Vector3.one * 1.1f : Vector3.one;
            }
        }
    }
}