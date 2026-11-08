#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Lake.UI
{
    /// <summary>
    /// Программно создаёт Canvas + все панели.
    /// Вызывается из HorrorSceneBuilder или вручную через ContextMenu.
    /// </summary>
    public class HudBuilder : MonoBehaviour
    {
        [ContextMenu("Build HUD")]
        public void Build()
        {
            // Проверка: не создан ли уже
            var existing = GameObject.Find("Generated_HUD");
            if (existing != null) DestroyImmediate(existing);

            var canvasGo = new GameObject("Generated_HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(HudController));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var root = canvasGo.transform;

            // Crosshair
            var crosshair = CreateCrosshair(root);
            // Interaction prompt
            var prompt = CreatePrompt(root);
            // Ammo
            var ammo = CreateAmmo(root);
            // Weapon slots
            var slots = CreateSlots(root);
            // Flashlight
            var fl = CreateFlashlight(root);
            // Health
            var hp = CreateHealth(root);
            // Bodycam overlay
            var overlay = CreateOverlay(root);
            // Key hints
            var hints = CreateHints(root);
            // Death screen
            var death = CreateDeath(root);

            // OutlineManager на отдельном объекте
            var outlineGo = new GameObject("OutlineManager");
            var outline = outlineGo.AddComponent<Outline.OutlineManager>();

            // Назначить ссылки HudController
            var hud = canvasGo.GetComponent<HudController>();
            var so = new UnityEditor.SerializedObject(hud);
            so.FindProperty("crosshair").objectReferenceValue = crosshair;
            so.FindProperty("prompt").objectReferenceValue = prompt;
            so.FindProperty("ammo").objectReferenceValue = ammo;
            so.FindProperty("slots").objectReferenceValue = slots;
            so.FindProperty("flashlight").objectReferenceValue = fl;
            so.FindProperty("health").objectReferenceValue = hp;
            so.FindProperty("overlay").objectReferenceValue = overlay;
            so.FindProperty("keyHints").objectReferenceValue = hints;
            so.FindProperty("deathScreen").objectReferenceValue = death;
            so.FindProperty("outlineManager").objectReferenceValue = outline;
            so.ApplyModifiedProperties();
        }

        // --- Builder helpers ---

        static TMP_Text MakeText(Transform parent, string name, string text, int size,
                                  TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = FontStyles.Bold;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            // Don't set outline - requires a material to be assigned first
            return t;
        }

        static Image MakeImage(Transform parent, string name, Vector2 size, Color color, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchorMin = rt.anchorMax = anchor;
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        CrosshairUI CreateCrosshair(Transform parent)
        {
            var root = new GameObject("Crosshair", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;

            var c = root.AddComponent<CrosshairUI>();

            Color crossColor = new Color(1f, 1f, 1f, 0.7f);
            Image MakeLine(string n, Vector2 size, Vector2 anchor)
            {
                var img = MakeImage(root.transform, n, size, crossColor, anchor);
                img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                return img;
            }
            var top = MakeLine("Top", new Vector2(1, 8), new Vector2(0.5f, 0.5f));
            var bottom = MakeLine("Bottom", new Vector2(1, 8), new Vector2(0.5f, 0.5f));
            var left = MakeLine("Left", new Vector2(8, 1), new Vector2(0.5f, 0.5f));
            var right = MakeLine("Right", new Vector2(8, 1), new Vector2(0.5f, 0.5f));
            var dot = MakeLine("Dot", new Vector2(2, 2), new Vector2(0.5f, 0.5f));

            var so = new UnityEditor.SerializedObject(c);
            so.FindProperty("top").objectReferenceValue = top.rectTransform;
            so.FindProperty("bottom").objectReferenceValue = bottom.rectTransform;
            so.FindProperty("left").objectReferenceValue = left.rectTransform;
            so.FindProperty("right").objectReferenceValue = right.rectTransform;
            so.FindProperty("dot").objectReferenceValue = dot;
            so.ApplyModifiedProperties();
            return c;
        }

        InteractionPromptUI CreatePrompt(Transform parent)
        {
            var go = new GameObject("InteractionPrompt", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 120);
            rt.sizeDelta = new Vector2(600, 40);

            var t = MakeText(go.transform, "Label", "", 22, TextAlignmentOptions.Center, Color.white);
            t.rectTransform.anchorMin = Vector2.zero;
            t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.sizeDelta = Vector2.zero;

            var c = go.AddComponent<InteractionPromptUI>();
            var so = new UnityEditor.SerializedObject(c);
            so.FindProperty("group").objectReferenceValue = go.GetComponent<CanvasGroup>();
            so.FindProperty("label").objectReferenceValue = t;
            so.ApplyModifiedProperties();
            return c;
        }

        AmmoPanelUI CreateAmmo(Transform parent)
        {
            var go = new GameObject("AmmoPanel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-40, 40);
            rt.sizeDelta = new Vector2(300, 120);

            var name = MakeText(go.transform, "WeaponName", "", 14, TextAlignmentOptions.Right, new Color(1, 1, 1, 0.7f));
            name.rectTransform.anchorMin = new Vector2(1, 1);
            name.rectTransform.anchorMax = new Vector2(1, 1);
            name.rectTransform.pivot = new Vector2(1, 1);
            name.rectTransform.anchoredPosition = Vector2.zero;
            name.rectTransform.sizeDelta = new Vector2(300, 20);

            var cur = MakeText(go.transform, "Current", "00", 56, TextAlignmentOptions.Right, Color.white);
            cur.rectTransform.anchorMin = new Vector2(1, 0);
            cur.rectTransform.anchorMax = new Vector2(1, 0);
            cur.rectTransform.pivot = new Vector2(1, 0);
            cur.rectTransform.anchoredPosition = new Vector2(0, 0);
            cur.rectTransform.sizeDelta = new Vector2(200, 80);

            var res = MakeText(go.transform, "Reserve", "000", 24, TextAlignmentOptions.Right, new Color(1, 1, 1, 0.6f));
            res.rectTransform.anchorMin = new Vector2(1, 0);
            res.rectTransform.anchorMax = new Vector2(1, 0);
            res.rectTransform.pivot = new Vector2(1, 0);
            res.rectTransform.anchoredPosition = new Vector2(-2, 30);
            res.rectTransform.sizeDelta = new Vector2(80, 30);

            var c = go.AddComponent<AmmoPanelUI>();
            var so = new UnityEditor.SerializedObject(c);
            so.FindProperty("current").objectReferenceValue = cur;
            so.FindProperty("reserve").objectReferenceValue = res;
            so.FindProperty("weaponName").objectReferenceValue = name;
            so.ApplyModifiedProperties();
            return c;
        }

        WeaponSlotsUI CreateSlots(Transform parent)
        {
            var go = new GameObject("WeaponSlots", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-40, 170);
            rt.sizeDelta = new Vector2(140, 60);

            var c = go.AddComponent<WeaponSlotsUI>();
            var so = new UnityEditor.SerializedObject(c);
            var slots = so.FindProperty("slots");
            slots.arraySize = 2;
            string[] names = { "RIFLE", "SHOTGUN" };
            for (int i = 0; i < 2; i++)
            {
                var slotGo = new GameObject($"Slot{i}", typeof(RectTransform), typeof(Image));
                slotGo.transform.SetParent(go.transform, false);
                var srt = slotGo.GetComponent<RectTransform>();
                srt.anchorMin = srt.anchorMax = new Vector2(1, 0);
                srt.pivot = new Vector2(1, 0);
                srt.anchoredPosition = new Vector2(-i * 68, 0);
                srt.sizeDelta = new Vector2(60, 60);
                var img = slotGo.GetComponent<Image>();
                img.color = new Color(1, 1, 1, 0.1f);

                var key = MakeText(slotGo.transform, "Key", $"[{i + 1}]", 12, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.3f));
                key.rectTransform.anchorMin = new Vector2(0.5f, 1);
                key.rectTransform.anchorMax = new Vector2(0.5f, 1);
                key.rectTransform.pivot = new Vector2(0.5f, 1);
                key.rectTransform.anchoredPosition = new Vector2(0, -2);
                key.rectTransform.sizeDelta = new Vector2(60, 14);

                var lbl = MakeText(slotGo.transform, "Label", names[i], 10, TextAlignmentOptions.Center, Color.white);
                lbl.rectTransform.anchorMin = new Vector2(0.5f, 0);
                lbl.rectTransform.anchorMax = new Vector2(0.5f, 0);
                lbl.rectTransform.pivot = new Vector2(0.5f, 0);
                lbl.rectTransform.anchoredPosition = new Vector2(0, 4);
                lbl.rectTransform.sizeDelta = new Vector2(60, 14);

                var el = slots.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("background").objectReferenceValue = img;
                el.FindPropertyRelative("label").objectReferenceValue = lbl;
                el.FindPropertyRelative("keyLabel").objectReferenceValue = key;
            }
            so.ApplyModifiedProperties();
            return c;
        }

        FlashlightUI CreateFlashlight(Transform parent)
        {
            var go = new GameObject("FlashlightUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(40, 40);
            rt.sizeDelta = new Vector2(240, 40);

            var icon = MakeImage(go.transform, "Icon", new Vector2(20, 20), new Color(1, 1, 1, 0.3f), new Vector2(0, 0.5f));
            icon.rectTransform.anchoredPosition = new Vector2(10, 0);

            var status = MakeText(go.transform, "Status", "FLASHLIGHT OFF", 14, TextAlignmentOptions.Left, new Color(1, 1, 1, 0.3f));
            status.rectTransform.anchorMin = new Vector2(0, 0.5f);
            status.rectTransform.anchorMax = new Vector2(0, 0.5f);
            status.rectTransform.pivot = new Vector2(0, 0.5f);
            status.rectTransform.anchoredPosition = new Vector2(40, 0);
            status.rectTransform.sizeDelta = new Vector2(200, 20);

            var bat = MakeImage(go.transform, "Battery", new Vector2(100, 4), new Color(1f, 0.9f, 0.4f), new Vector2(0, 0.5f));
            bat.rectTransform.anchoredPosition = new Vector2(40, -14);
            bat.type = Image.Type.Filled;
            bat.fillMethod = Image.FillMethod.Horizontal;

            var c = go.AddComponent<FlashlightUI>();
            var so = new UnityEditor.SerializedObject(c);
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("status").objectReferenceValue = status;
            so.FindProperty("batteryFill").objectReferenceValue = bat;
            so.ApplyModifiedProperties();
            return c;
        }

        HealthBarUI CreateHealth(Transform parent)
        {
            var go = new GameObject("HealthBar", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(40, 90);
            rt.sizeDelta = new Vector2(220, 14);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.5f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(go.transform, false);
            var frt = fillGo.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.sizeDelta = Vector2.zero;
            var fill = fillGo.GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.color = new Color(0.2f, 0.9f, 0.3f);

            var label = MakeText(go.transform, "Value", "100 / 100", 10, TextAlignmentOptions.Center, Color.white);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.sizeDelta = Vector2.zero;

            var c = go.AddComponent<HealthBarUI>();
            var so = new UnityEditor.SerializedObject(c);
            so.FindProperty("fill").objectReferenceValue = fill;
            so.FindProperty("valueLabel").objectReferenceValue = label;
            so.ApplyModifiedProperties();
            return c;
        }

        BodyCamOverlayUI CreateOverlay(Transform parent)
        {
            var go = new GameObject("BodyCamOverlay", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            var cg = go.GetComponent<CanvasGroup>();
            cg.alpha = 1f;

            // REC (top-left)
            var rec = MakeText(go.transform, "REC", "● REC", 22, TextAlignmentOptions.Left, Color.red);
            rec.rectTransform.anchorMin = rec.rectTransform.anchorMax = new Vector2(0, 1);
            rec.rectTransform.pivot = new Vector2(0, 1);
            rec.rectTransform.anchoredPosition = new Vector2(40, -40);
            rec.rectTransform.sizeDelta = new Vector2(200, 30);

            var tc = MakeText(go.transform, "Timecode", "00:00:00:00", 16, TextAlignmentOptions.Left, Color.white);
            tc.rectTransform.anchorMin = tc.rectTransform.anchorMax = new Vector2(0, 1);
            tc.rectTransform.pivot = new Vector2(0, 1);
            tc.rectTransform.anchoredPosition = new Vector2(40, -70);
            tc.rectTransform.sizeDelta = new Vector2(300, 24);

            var camId = MakeText(go.transform, "CamId", "CAM-01", 14, TextAlignmentOptions.Right, new Color(1, 1, 1, 0.8f));
            camId.rectTransform.anchorMin = camId.rectTransform.anchorMax = new Vector2(1, 1);
            camId.rectTransform.pivot = new Vector2(1, 1);
            camId.rectTransform.anchoredPosition = new Vector2(-40, -40);
            camId.rectTransform.sizeDelta = new Vector2(300, 20);

            var date = MakeText(go.transform, "Date", "", 12, TextAlignmentOptions.Right, new Color(1, 1, 1, 0.6f));
            date.rectTransform.anchorMin = date.rectTransform.anchorMax = new Vector2(1, 1);
            date.rectTransform.pivot = new Vector2(1, 1);
            date.rectTransform.anchoredPosition = new Vector2(-40, -64);
            date.rectTransform.sizeDelta = new Vector2(300, 18);

            var bat = MakeText(go.transform, "Battery", "100%", 12, TextAlignmentOptions.Right, new Color(1, 1, 1, 0.6f));
            bat.rectTransform.anchorMin = bat.rectTransform.anchorMax = new Vector2(1, 1);
            bat.rectTransform.pivot = new Vector2(1, 1);
            bat.rectTransform.anchoredPosition = new Vector2(-40, -86);
            bat.rectTransform.sizeDelta = new Vector2(300, 18);

            var c = go.AddComponent<BodyCamOverlayUI>();
            var so = new UnityEditor.SerializedObject(c);
            so.FindProperty("group").objectReferenceValue = cg;
            so.FindProperty("recLabel").objectReferenceValue = rec;
            so.FindProperty("timecodeLabel").objectReferenceValue = tc;
            so.FindProperty("dateLabel").objectReferenceValue = date;
            so.FindProperty("camIdLabel").objectReferenceValue = camId;
            so.FindProperty("batteryLabel").objectReferenceValue = bat;
            so.ApplyModifiedProperties();
            return c;
        }

        KeyHintsUI CreateHints(Transform parent)
        {
            var go = new GameObject("KeyHints", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -40);
            rt.sizeDelta = new Vector2(900, 100);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.4f);

            var label = MakeText(go.transform, "Hints", "", 12, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.9f));
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.sizeDelta = new Vector2(-20, -10);

            var c = go.AddComponent<KeyHintsUI>();
            var so = new UnityEditor.SerializedObject(c);
            so.FindProperty("group").objectReferenceValue = go.GetComponent<CanvasGroup>();
            so.FindProperty("label").objectReferenceValue = label;
            so.ApplyModifiedProperties();
            return c;
        }

        DeathScreenUI CreateDeath(Transform parent)
        {
            var go = new GameObject("DeathScreen", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0, 0, 0, 0.9f);
            go.SetActive(false);

            var title = MakeText(go.transform, "Title", "SIGNAL LOST", 72, TextAlignmentOptions.Center, new Color(0.9f, 0.1f, 0.1f));
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            title.rectTransform.anchoredPosition = new Vector2(0, 60);
            title.rectTransform.sizeDelta = new Vector2(900, 100);

            var countdown = MakeText(go.transform, "Countdown", "", 22, TextAlignmentOptions.Center, Color.white);
            countdown.rectTransform.anchorMin = countdown.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            countdown.rectTransform.anchoredPosition = new Vector2(0, -40);
            countdown.rectTransform.sizeDelta = new Vector2(900, 40);

            var c = go.AddComponent<DeathScreenUI>();
            var so = new UnityEditor.SerializedObject(c);
            so.FindProperty("group").objectReferenceValue = go.GetComponent<CanvasGroup>();
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("countdown").objectReferenceValue = countdown;
            so.ApplyModifiedProperties();
            return c;
        }
    }
}
#endif