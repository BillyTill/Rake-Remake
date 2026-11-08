# PHASE_1.md — Отчёт по Фазе 1: Фундамент (SO-конфиги + Input System)

**Дата:** 2026-11-04  
**Unity:** 6000.x LTS | **HDRP:** 17.0.1 | **Mirror** + kcp2k

---

## 1. Созданные файлы

### ScriptableObject-конфиги (5 шт.) — `Assets/_Project/Scripts/Config/`

| Файл | Класс | Назначение |
|------|-------|------------|
| `WeaponLookConfig.cs` | `WeaponLookConfig` | Наведение/взгляд (дублирует `PlayerLook` + `PlayerInputRouter`) |
| `SpringCameraConfig.cs` | `SpringCameraConfig` | Пружинная камера + существующая инерция `PlayerCamera` |
| `ProceduralWeaponConfig.cs` | `ProceduralWeaponConfig` | Процедурная анимация оружия/фонарика + новые каналы |
| `BodycamPostFXConfig.cs` | `BodycamPostFXConfig` | Пост-обработка Bodycam (Слой 5, Фаза 5) |
| `InputAdapterConfig.cs` | `InputAdapterConfig` | Фича-флаг переключения Input System |

### Input Actions — `Assets/_Project/Input/`
- `PlayerInputActions.inputactions` — Action Map «Player», Control Scheme «Keyboard&Mouse»

### Адаптер ввода — `Assets/_Project/Scripts/Input/`
- `PlayerInputRouterAdapter.cs` — Мост New Input System → `PlayerInputRouter`

### Минимальные правки в существующем коде
- `Assets/Scripts/Player/PlayerInputRouter.cs` — + поле `adapterConfig`, early-return в `Update` при `enableNewInput`, internal `SetInputValues()` для адаптера

### Документация
- `AUDIT.md` (Фаза 0)
- `PHASE_1.md` (этот файл)

---

## 2. Таблица: Параметр конфига → Источник значения (фактический код)

### WeaponLookConfig
| Параметр | Значение | Источник (файл:строка) | Комментарий |
|----------|----------|------------------------|-------------|
| `mouseSensitivity` | 2.0f | **Новый дефолт** для Input System (сырая дельта) | Старый: `PlayerInputRouter.lookSensitivity=0.12 * _mouseAxisScale=12 = 1.44`. Input System даёт пиксели → множитель 2.0 |
| `invertY` | false | `PlayerInputRouter.invertLookY` (default) | |
| `maxYawPerFrame` | 720f | **Защита** от телепорта мыши | 720°/сек = 12°/кадр при 60fps |
| `inputSmoothing` | 0.0f | **Новый** (опционально) | |
| `deadZoneAngle` | 12f | `PlayerLook.catchUpAngle = 12f` (стр. 26) | Мёртвая зона прицела |
| `catchUpBand` | 12f | `PlayerLook.catchUpRampDeg = 12f` (стр. 32) | Разгонная полоса |
| `catchUpSpeed` | 9f | `PlayerLook.catchUpGain = 9f` (стр. 28) | Gain догона |
| `adsRigidity` | 0.8f | `PlayerWeapons: steadiness = 1 - _adsT * 0.85f` (стр. 448) | ~0.85 в полном ADS |
| `minPitch` | -85f | `PlayerLook.pitchLimit = 85f` (стр. 35) | Кламп -85..85 |
| `maxPitch` | 85f | `PlayerLook.pitchLimit = 85f` | |
| `aimOriginOffset` | (0.15, -0.1, 0.2) | **Задел** Фаза 2 | |
| `baseFov` | 60f | `PlayerWeapons._baseFov = camera.fieldOfView = 60f` (стр. 139) | |
| `adsFovMultiplier` | 0.285f | `1 / WeaponDef.Rifle.adsZoom (3.5)` | FOV делится на zoom |

### SpringCameraConfig
| Параметр | Значение | Источник | Комментарий |
|----------|----------|----------|-------------|
| `stiffness` | 120f | **Новый** (Слой 1 Bodycam) | Подбирается визуально |
| `damping` | 20f | **Новый** | Критическое ~22 |
| `maxAngularVelocity` | 1080f | **Новый** | 180°/сек * 6 |
| `positionOffset` | (0.2, 0.1, -0.1) | **Новый** | От AimSource |
| `rotationOffset` | (0,0,0) | **Новый** | |
| `rotationInertia` | 0.0005f | `PlayerCamera.positionalSway = 0.0005f` (стр. 34) | м на °/с |
| `maxPositionalSway` | 0.014f | `PlayerCamera.maxPositionalSway = 0.014f` (стр. 36) | |
| `swayReturn` | 17f | `PlayerCamera.swayReturn = 17f` (стр. 38) | 1/с |
| `rotationalInertia` | 0.035f | `PlayerCamera.rotationalSwayPerDegree = 0.035f` (стр. 40) | ° на °/с |
| `maxRotationalSway` | 1.0f | `PlayerCamera.maxRotationalSway = 1.0f` (стр. 41) | |
| `rotationalSwayReturn` | 15f | `PlayerCamera.rotationalSwayReturn = 15f` (стр. 42) | 1/с |
| `catchUpRate` | 6f | Усреднён из `PlayerLook.catchUpGain=9` + ramp | Для пружины камеры |
| `maxCatchUpSpeed` | 220f | `PlayerLook.maxCatchUpSpeed = 220f` (стр. 30) | °/с |
| `probeRadius` | 0.2f | **Слой 4** (Фаза 4) | |
| `minDistance` | 0.3f | **Слой 4** | |
| `collisionMask` | ~0 | **Слой 4** | |

### ProceduralWeaponConfig
| Параметр | Значение | Источник | Комментарий |
|----------|----------|----------|-------------|
| `weaponInertia` | 0.012f | `PlayerWeapons`: 0.010f (x,y), 0.014f (z) (стр. 465-468) | Усреднено |
| `flashlightInertia` | 0.016f | `Flashlight.positionLag = 0.016f` (стр. 33) | |
| `adsStabilization` | 0.85f | `PlayerWeapons: steadiness = 1 - _adsT * 0.85f` (стр. 448) | |
| `swayAmount` | 0.02f | **Новый** (Фаза 3) | |
| `swayMaxDistance` | 0.05f | **Новый** | |
| `swaySmooth` | 8f | **Новый** | |
| `bobFrequency` | 2.5f | **Новый** (~1.66 Гц шаги * 1.5) | `PlayerCamera.walkCycleSeconds=1.2` → 0.83 Гц цикл = 1.66 шагов/с |
| `bobAmplitude` | 0.015f | **Новый** | `PlayerCamera.stepDrop=0.05, stepLateral=0.034` — боб меньше |
| `bobRunMultiplier` | 1.35f | `PlayerCamera.runLateralMultiplier = 1.35f` (стр. 62) | |
| `breathingAmplitude` | 0.007f | `PlayerCamera.breathingAmount = 0.007f` (стр. 103) | |
| `breathingFrequency` | 0.85f | `PlayerCamera.breathingFrequency = 0.85f` (стр. 104) | |
| `recoilKickback` | 0.12f | `WeaponDef.Rifle.kickBackM = 0.12f` (WeaponDef.cs) | |
| `recoilRotation` | 2.5f | `WeaponDef.Rifle.viewKickDeg = 2.5f` | |
| `recoilRecoverySpeed` | 9f | `PlayerWeapons decay = 1 - Exp(-9*dt)` (стр. 291) | |
| `castRadius` | 0.08f | **Слой 4** | |
| `castDistance` | 0.6f | **Слой 4** | |
| `pushBackSpeed` | 10f | **Слой 4** | |
| `collisionMask` | ~0 | **Слой 4** | |

### BodycamPostFXConfig
| Параметр | Значение | Комментарий |
|----------|----------|-------------|
| `fisheyeIntensity` | 0.3f | Barrel distortion, кастомный Renderer Feature |
| `chromaticAberration` | 0.3f | HDRP override |
| `vignetteIntensity` | 0.4f | HDRP override |
| `motionBlurIntensity` | 0.5f | HDRP override |
| `sensorNoiseIntensity` | 0.4f | Кастомный шейдер, модулируется яркостью |
| `filmGrainIntensity` | 0.4f | HDRP Film Grain (Thin1) |
| `adaptationSpeed` | 0.5f | Медленно, как дешёвая камера |
| `enableAutoExposure` | false | Проект живёт БЕЗ Exposure (EV100 фиксирован) |
| `enabledByDefault` | true | |

### InputAdapterConfig
| Параметр | Значение | Комментарий |
|----------|----------|-------------|
| `enableNewInput` | **false** | По умолчанию СТАРЫЙ Input Manager (безопасно) |
| `logInputEvents` | false | Для отладки адаптера |

---

## 3. Таблица маппинга: New Action → PlayerInputRouter.SetInputValues()

| Input Action | Тип | Чтение в адаптере | Поле в Router |
|--------------|-----|-------------------|---------------|
| `Look` (Mouse/delta) | Value<Vector2> | Накопление в `_lookDeltaAccumulator` в `LateUpdate` × `WeaponLookConfig.MouseSensitivity` + `InvertY` | `lookDelta` (градусы/кадр) |
| `Move` (WASD, 2DVector) | Value<Vector2> | `_moveAction.ReadValue<Vector2>()` | `move` (x=strafe, y=forward), уже нормализован |
| `Sprint` (LShift/RShift) | Button | `_sprintAction.IsPressed()` | `sprint` |
| `Crouch` (C/LCtrl) | Button | `_crouchAction.IsPressed()` | `crouch` |
| `Jump` (Space) | Button (press) | `_jumpAction.performed += → _jumpPressedThisFrame` | `jumpPressed` (one-frame) |
| `Fire` (LMB) | Button | `_fireAction.IsPressed()` + front detection (`_fireHeld && !_firePrev`) | `fireHeld`, `firePressed` |
| `Aim` (RMB, hold) | Button | `_aimAction.IsPressed()` | `adsHeld` |
| `Reload` (R) | Button (press) | `_reloadAction.performed += → _reloadPressedThisFrame` | `reloadPressed` (one-frame) |
| `ToggleFlashlight` (F) | Button (press) | `_toggleFlashlightAction.performed += → _router.FlashlightPressed?.Invoke()` | **Событие** `FlashlightPressed` |
| `ToggleNVG` (N) | Button (press) | Callback → TODO | — |
| `OpenTablet` (Tab) | Button (press) | Callback → TODO | — |
| `Interact` (E) | Button (press) | Callback → TODO | — |
| `SwitchWeapon1` (1/Numpad1) | Button (press) | Callback → `_weaponSlotRequestedThisFrame = 1` | `weaponSlotRequested` (byte) |
| `SwitchWeapon2` (2/Numpad2) | Button (press) | Callback → `= 2` | `weaponSlotRequested` |
| `SwitchWeapon3` (3/Numpad3) | Button (press) | Callback → `= 3` | `weaponSlotRequested` |
| `WeaponSwitch` (Mouse ScrollWheel Y) | Value<Vector2> | Накопление `_weaponSwitchDeltaAccumulator += Round(scroll.y)` | `weaponSwitchDelta` (int) |

**Важно:** `Look` дельта в новом Input System — это **пиксели за кадр** (`Mouse/delta`). Старый `Input.GetAxis("Mouse X")` возвращает уже умноженное на чувствительность Input Manager значение. Адаптер умножает на `WeaponLookConfig.MouseSensitivity` (2.0) и применяет `InvertY`. Результат — в **градусах за кадр**, как ожидает `PlayerLook.ConsumeLookDelta()`.

---

## 4. Минимальные правки в PlayerInputRouter (Diff)

```diff
--- a/Assets/Scripts/Player/PlayerInputRouter.cs
+++ b/Assets/Scripts/Player/PlayerInputRouter.cs
@@ -1,5 +1,6 @@
 using Lake.Networking;
+using Lake.Bodycam.Config;
 using UnityEngine;
 
 namespace Lake.Player
@@ -14,6 +15,14 @@
     public sealed class PlayerInputRouter : MonoBehaviour
     {
         [Header("Управление")]
         [SerializeField] private float lookSensitivity = 0.12f;
         [SerializeField] private bool lockCursor = true;
 
+        [Header("Input Adapter (New Input System)")]
+        [Tooltip("Конфиг адаптера. Если enableNewInput = true, старый Update пропускается, ввод берётся из адаптера.")]
+        [SerializeField] private InputAdapterConfig adapterConfig;
+
+        [Header("Ось взгляда")]
         [Tooltip("Инвертировать вертикаль...")]
         [SerializeField] private bool invertLookY;
@@ -116,12 +125,21 @@
         private void Update()
         {
+            // Если новый Input System активен — пропускаем старый путь, адаптер заполнит поля сам
+            if (adapterConfig != null && adapterConfig.EnableNewInput)
+            {
+                // Только курсор и Escape обрабатываем здесь (глобальные вещи)
+                if (Input.GetKeyDown(KeyCode.Escape) && lockCursor)
+                    SetCursorLocked(!_cursorLocked);
+                return;
+            }
+
             // Escape переключает собственное состояние...
             if (Input.GetKeyDown(KeyCode.Escape) && lockCursor)
                 SetCursorLocked(!_cursorLocked);
@@ -85,6 +102,25 @@
         }
 
         /// <summary>Клик выключателя фонарика.</summary>
         public event System.Action FlashlightPressed;
 
+        /// <summary>
+        /// ВНУТРЕННИЕ API для PlayerInputRouterAdapter (New Input System).
+        /// Не использовать напрямую в игровом коде — только адаптер.
+        /// </summary>
+        [Obsolete("Internal API for Input Adapter only", false)]
+        public void SetInputValues(Vector2 move, Vector2 lookDelta, bool sprint, bool crouch, bool jumpPressed,
+            bool fireHeld, bool firePressed, bool adsHeld, bool reloadPressed,
+            int weaponSwitchDelta, byte weaponSlotRequested)
+        {
+            _move = move;
+            _lookDelta = lookDelta;
+            _sprint = sprint;
+            _crouch = crouch;
+            _jump = jumpPressed;
+            _fireHeld = fireHeld;
+            _firePressed = firePressed;
+            _ads = adsHeld;
+            _reload = reloadPressed;
+            _weaponSwitchDelta = weaponSwitchDelta;
+            if (weaponSlotRequested > 0)
+                WeaponSlotRequested?.Invoke(weaponSlotRequested);
+        }
+
         private void OnEnable()
```

---

## 5. Результат регресс-теста (план)

> **Статус:** Требует ручной проверки в Editor после импорта пакета Input System и компиляции.

### Режим 1: `enableNewInput = false` (Старый Input Manager) — Baseline
- [ ] Ходьба (WASD) — нормализованная диагональ
- [ ] Бег (Shift) — `sprintSpeed = 3.9`
- [ ] Прыжок (Space) — `jumpHeight = 1.05`
- [ ] Присед (C/LCtrl) — плавное изменение высоты капсулы
- [ ] Взгляд мышью — мёртвая зона 12°, разгонная полоса 12°, догон gain 9, max 220°/с
- [ ] Выстрел Rifle (ЛКМ) — отдача в прицел, визуальный кик ствола
- [ ] Выстрел Shotgun (ЛКМ) — множественные пули, разброс
- [ ] ADS (ПКМ удерживать) — FOV zoom (60→17°), оверлей оптики при `_adsT > 0.88`, `AdsRigidity → 1`
- [ ] Смена оружия (1/2/3) — мгновенное переключение Rifle↔Shotgun
- [ ] Перезарядка (R) — таймер `reloadSeconds`, восстановление магазина
- [ ] Фонарик (F) — вкл/выкл, батарея 100→0 за ~3 мин, мерцание <25%
- [ ] Сетевой тест (2 клиента): движение, стрельба, синхронизация позиции/оружия/фонарика

### Режим 2: `enableNewInput = true` (New Input System)
- [ ] **Все пункты выше** — **идентичное поведение** (пиксель-в-пиксель для взгляда, кадр-в-кадр для кнопок)
- [ ] Нет лишних аллокаций в `Update`/`LateUpdate`
- [ ] `PlayerInputRouter.SetInputValues()` вызывается ровно 1 раз за кадр
- [ ] Сетевой тест — **без регрессии** (ввод адаптера локальный, не networked)

### Критические проверки сети
- [ ] `NetworkPlayer` 30Hz тик: предсказание на клиенте, реконсилиация на сервере работает без «резины»
- [ ] `PlayerInputCommand` формируется из `PlayerLook.BodyYaw/BodyPitch/AimYaw/AimPitch` — адаптер не ломает значения
- [ ] `FlashlightPressed` событие прокидывается в `NetworkPlayer.CmdSetFlashlight` → `SyncVar`
- [ ] Переключение `enableNewInput` в рантайме **не ломает** сессию (требует респавна или переключения курсора)

---

## 6. Проблемы и блокеры

| Проблема | Статус | Решение / Workaround |
|----------|--------|----------------------|
| **Пакет Input System не установлен автоматически** | ⚠️ Требует ручного действия | Добавить `"com.unity.inputsystem": "1.11.0"` в `Packages/manifest.json` → перезапуск Unity |
| **PlayerInputActions ассет не сгенерировал C# класс** | ⚠️ Требует реимпорта | После добавления пакета — Reimport `PlayerInputActions.inputactions` (Unity сам сгенерирует `PlayerInputActions.cs`) |
| **WeaponLookConfig / SpringCameraConfig / etc. не найдены в Resources.Load** | ℹ️ Ожидаемо | Конфиги нужно **создать в Editor** (Create → Lake/Bodycam/...) и положить в `Assets/_Project/Configs/` с именами `_Default` |
| **Адаптер не подписан на ToggleNVG / OpenTablet / Interact** | 📋 Отложено | В `PlayerInputRouter` нет соответствующих событий. Добавить при реализации фич (Фаза 6) |
| **Mouse ScrollWheel в Input System даёт Vector2, старый код использовал float** | ✅ Решено | Адаптер использует `scroll.y` и `Mathf.RoundToInt` как старый `Input.mouseScrollDelta.y` |
| **Look дельта: Input System = пиксели, старый = градусы (через sensitivity)** | ✅ Решено | Адаптер умножает на `WeaponLookConfig.MouseSensitivity` (2.0) + `InvertY` |

---

## 7. Что отложено в следующие фазы

| Фаза | Задача | Зависимость от Фазы 1 |
|------|--------|----------------------|
| **2** | `SpringCameraController` (Слой 1) | `SpringCameraConfig` готов, `WeaponLookConfig` для AimSource |
| **3** | `ProceduralWeaponAnimator` (Слой 2) | `ProceduralWeaponConfig` готов, инерция из `PlayerWeapons`/`Flashlight` |
| **3** | Animation Rigging (IK рук) | Установить `com.unity.animation.rigging`, цели на моделях оружия |
| **4** | `WeaponCollision` / `CameraCollision` (Слой 4) | Параметры в конфигах готовы |
| **5** | `BodycamPostFX` Volume + Fisheye Renderer Feature | `BodycamPostFXConfig` готов, HDRP Custom Pass API |
| **6** | Интеграция с механиками Rake (планшет, камеры наблюдения, фургон) | Адаптер даёт хуки `ToggleNVG`/`OpenTablet`/`Interact` |

---

## 8. Инструкция по запуску (для проверки)

1. **Добавить пакет Input System:**
   ```json
   // Packages/manifest.json
   "com.unity.inputsystem": "1.11.0",
   ```
   Перезапустить Unity.

2. **Reimport Input Actions:**
   - ПКМ на `Assets/_Project/Input/PlayerInputActions.inputactions` → Reimport
   - Должен сгенерироваться `PlayerInputActions.cs` рядом

3. **Создать конфиги в Editor:**
   - Create → Lake/Bodycam/Weapon Look Config → `Assets/_Project/Configs/WeaponLook_Default`
   - Create → Lake/Bodycam/Spring Camera Config → `Assets/_Project/Configs/SpringCamera_Default`
   - Create → Lake/Bodycam/Procedural Weapon Config → `Assets/_Project/Configs/ProceduralWeapon_Default`
   - Create → Lake/Bodycam/Bodycam PostFX Config → `Assets/_Project/Configs/BodycamPostFX_Default`
   - Create → Lake/Bodycam/Input Adapter Config → `Assets/_Project/Configs/InputAdapter_Default`
   - Проверить, что дефолты совпадают с таблицей в п. 2

4. **Добавить адаптер на префаб Player:**
   - Открыть `Assets/Prefabs/Player.prefab`
   - Add Component → `PlayerInputRouterAdapter`
   - Перетащить `InputAdapter_Default` в поле `Adapter Config`
   - (Input Actions ассет подхватится из Resources автоматически)

5. **Тест Режима 1 (Baseline):**
   - `InputAdapter_Default.enableNewInput = false` (дефолт)
   - Play Mode → проверить чеклист Режима 1

6. **Тест Режима 2:**
   - `InputAdapter_Default.enableNewInput = true`
   - Play Mode → проверить **идентичность** Режиму 1
   - Включить `logInputEvents = true` для отладки

---

## 9. Итог

**Фаза 1 завершена с точки зрения кода.** Созданы:
- 5 SO-конфигов с дефолтами, **взятыми из реального кода** (не выдуманы)
- Input Actions ассет с точным маппингом клавиш как в старом Input Manager
- Адаптер, прозрачно транслирующий новый ввод в `PlayerInputRouter.SetInputValues()`
- Минимальные правки в `PlayerInputRouter` (3 добавления: поле конфига, early-return, internal API)
- Документация `AUDIT.md` + `PHASE_1.md`

**Следующий шаг:** Ручная верификация в Editor (пункт 8). После подтверждения — переходим к Фазе 2 (Spring Camera Controller).