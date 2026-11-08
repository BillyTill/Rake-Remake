using UnityEngine;
using TMPro;
using Lake.Weapons;

namespace Lake.UI
{
    public class AmmoPanelUI : MonoBehaviour
    {
        [SerializeField] TMP_Text current;
        [SerializeField] TMP_Text reserve;
        [SerializeField] TMP_Text weaponName;

        public void SetAmmo(int cur, int res, WeaponDataSO data)
        {
            current.text = cur.ToString("D2");
            reserve.text = res.ToString("D3");
            if (data != null) weaponName.text = data.displayName.ToUpper();

            current.color = cur == 0 ? Color.red :
                            cur <= data.magazineSize * 0.25f ? new Color(1f, 0.6f, 0.2f) :
                            Color.white;
        }
    }
}