using UnityEngine;
using Lake.Weapons;
using Lake.Player;
using Lake.Health;
using Lake.Core;

namespace Lake.Core
{
    /// <summary>
    /// Простой IMGUI debug UI: здоровье, патроны, текущее оружие, spread, состояние.
    /// Находится на плеере, рисуется в левом верхнем углу.
    /// </summary>
    public class DebugUI : MonoBehaviour
    {
        [SerializeField] PlayerSpawner spawner;
        [SerializeField] bool show = true;
        [SerializeField] KeyCode toggleKey = KeyCode.BackQuote; // ` ~

        HealthComponent _health;
        WeaponController _wc;
        WeaponRuntime _current;

        void Update()
        {
            if (Input.GetKeyDown(toggleKey)) show = !show;

            // Находим живого игрока
            if (_health == null || _health.IsDead)
            {
                var player = spawner != null ? spawner.transform.Find("BodycamPlayer(Clone)") : null;
                if (player == null) player = GameObject.Find("BodycamPlayer(Clone)")?.transform;
                if (player != null)
                {
                    _health = player.GetComponent<HealthComponent>();
                    _wc = player.GetComponentInChildren<WeaponController>();
                }
            }

            if (_wc != null)
                _current = _wc.Current;
        }

        void OnGUI()
        {
            if (!show || _health == null) return;

            GUI.skin = null; // используем дефолтный
            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13,
                richText = true,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(10, 10, 10, 10)
            };

            float w = 280;
            float h = 220;
            Rect rect = new Rect(10, 10, w, h);

            // Фон
            GUI.Box(rect, "", new GUIStyle(GUI.skin.box) { normal = { background = MakeTex(2, 2, new Color(0, 0, 0, 0.7f)) } });

            // Контент
            GUILayout.BeginArea(new Rect(rect.x + 5, rect.y + 5, w - 10, h - 10));
            GUILayout.Label("<b><color=#ffffff>DEBUG INFO</color></b>", style);

            // Health
            Color hpColor = _health.Current > 50 ? Color.green : (_health.Current > 20 ? Color.yellow : Color.red);
            GUILayout.Label($"<color=#{ColorUtility.ToHtmlStringRGB(hpColor)}>HP: {_health.Current:0} / {_health.MaxHealth:0}</color>", style);

            // Weapon
            if (_current != null && _current.Data != null)
            {
                GUILayout.Label($"<color=#00ffff>Weapon: {_current.Data.displayName}</color>", style);
                GUILayout.Label($"Ammo: <b>{_current.CurrentAmmo}</b> / {_current.ReserveAmmo}", style);
                GUILayout.Label($"Spread: {_current.GetType().GetField("_currentSpread", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_current) ?? 0:F2}°", style);
                GUILayout.Label($"Reloading: {(_current.IsReloading ? "<color=#ffaa00>YES</color>" : "<color=#00ff00>NO</color>")}", style);
                GUILayout.Label($"ADS: {(_current.GetType().GetField("_isAds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_current) ?? false)}", style);
            }
            else
            {
                GUILayout.Label("<color=#ff4444>Weapon: NONE</color>", style);
            }

            // Slots
            if (_wc != null)
            {
                var slots = _wc.GetType().GetField("_instances", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_wc) as GameObject[];
                if (slots != null)
                {
                    string s0 = slots[0] != null ? slots[0].name : "empty";
                    string s1 = slots[1] != null ? slots[1].name : "empty";
                    GUILayout.Label($"Slots: [1] {s0} | [2] {s1}", style);
                }
            }

            // Position
            if (_health.transform != null)
            {
                var pos = _health.transform.position;
                GUILayout.Label($"Pos: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})", style);
            }

            GUILayout.EndArea();
        }

        static Texture2D _bgTex;
        static Texture2D MakeTex(int w, int h, Color c)
        {
            if (_bgTex == null) _bgTex = new Texture2D(w, h);
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = c;
            _bgTex.SetPixels(pix);
            _bgTex.Apply();
            return _bgTex;
        }
    }
}