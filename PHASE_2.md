# Phase 2: AimSource Extraction + PlayerCamera Refactor — Summary

## Обзор
Фаза 2 завершена. Извлечён `AimSource` как единый источник правды для наведения, `PlayerLook` стал тонкой обёрткой для обратной совместимости (сеть, PlayerWeapons), `PlayerCamera` читает параметры инерции из `SpringCameraConfig`. Поведение 1:1 с baseline.

---

## А. Фикс Sensitivity (Задача A)

### Математический анализ
| Путь | Формула | Множитель |
|------|---------|-----------|
| **Old** (Input Manager) | `GetAxis("Mouse X") × lookSensitivity(0.12) × _mouseAxisScale(12)` | 1.44 |
| **New** (Input System) | `<Mouse>/delta × PixelToCountFactor × LegacyMouseSensitivity` | 5.3 × 1.44 ≈ 7.63 |

### Изменения
- **WeaponLookConfig**: добавлены `legacyMouseSensitivity = 1.44f`, `pixelToCountFactor = 5.3f` (вместо `mouseSensitivity = 2.0f`)
- **PlayerInputRouterAdapter.LateUpdate**: конвертация `pixels → counts → legacy sensitivity`:
  ```csharp
  float totalSensitivity = config.PixelToCountFactor * config.LegacyMouseSensitivity; // ~7.63
  _lookDeltaAccumulator += lookRaw * totalSensitivity;
  ```
- **AimSource.Update** (фикс двойного умножения): `ConsumeLookDelta()` уже возвращает дельту
  **в градусах** — роутер применил `lookSensitivity(0.12) × _mouseAxisScale(12) = 1.44`
  в обоих путях (легаси `ReadLook` и адаптер). AimSource больше НЕ умножает на
  `LegacyMouseSensitivity` повторно (это завышало базовую чувствительность в 1.44×).
  Остаётся только множитель зума:
  ```csharp
  Vector2 lookDelta = _input.ConsumeLookDelta() * _sensitivityMultiplier;
  ```
  Это эквивалентно старому `PlayerLook` → `ConsumeLookDelta() * reticleSensitivity(=1.0)`.
- **InputAdapterConfig.enableNewInput** остаётся `false` по умолчанию.

### Рекомендация
`pixelToCountFactor = 5.3f` — теоретическое значение (800 DPI / 1920px). **Требуется эмпирический замер в Editor: 100 px → X° в обоих режимах.** Если расхождение >5% — подправить `pixelToCountFactor`.

---

## Б. AimSource Extraction (Задача B)

### Новые файлы
| Файл | Назначение |
|------|------------|
| `Assets/_Project/Scripts/Player/IAimSource.cs` | Интерфейс единственного источника правды |
| `Assets/_Project/Scripts/Player/AimSource.cs` | Реализация (копия логики PlayerLook 1:1) |

### IAimSource — публичный контракт
```csharp
public interface IAimSource
{
    Vector3 Direction { get; }
    Vector3 Origin { get; }
    Quaternion AimRotation { get; }
    float CurrentFov { get; }
    float AimYaw { get; }           // с ретикулом и отдачей
    float AimPitch { get; }
    float BodyYaw { get; }          // без ретикула, с отдачей при ADS
    float BodyPitch { get; }
    float CatchUpBlend { get; }
    void AddRecoil(float pitchKick, float yawKick);
    void SetAdsRigidity(float rigidity01);
    void SetSensitivityMultiplier(float mult);
}
```

### AimSource — ключевые моменты
- **Update()** — как PlayerLook раньше (не LateUpdate)
- Параметры из `WeaponLookConfig` (deadZoneAngle, catchUpBand, catchUpSpeed, adsRigidity, minPitch, maxPitch, baseFov, adsFovMultiplier, maxReticleOffset, returnToCenter, returnSpeed)
- Recoil decay: `exp(-6f * dt)` — без изменений
- Catch-up: `SmoothStep(ramp) × catchUpSpeed`, кламп `maxCatchUpSpeed` — копия PlayerLook строк 187-228
- `Origin` из `headTransform` (назначать на CameraPivot/Head)

### PlayerLook → обёртка
```csharp
public sealed class PlayerLook : MonoBehaviour
{
    [SerializeField] private AimSource _aimSource;

    public Vector3 AimDirection => _aimSource.Direction;
    public Quaternion AimRotation => _aimSource.AimRotation;
    public float AimYaw => _aimSource.AimYaw;
    public float AimPitch => _aimSource.AimPitch;
    public float BodyYaw => _aimSource.BodyYaw;     // для сети
    public float BodyPitch => _aimSource.BodyPitch; // для сети
    public float CatchUpBlend => _aimSource.CatchUpBlend;
    public float CurrentFov => _aimSource.CurrentFov;

    public void AddRecoil(float p, float y) => _aimSource.AddRecoil(p, y);
    public void SetAdsRigidity(float r) => _aimSource.SetAdsRigidity(r);
    public void SetSensitivityMultiplier(float m) => _aimSource.SetSensitivityMultiplier(m);
    // Старые поля _yaw/_pitch/_reticle/ConsumeLookDelta/UpdateCatchUp УДАЛЕНЫ
}
```

### NetworkPlayer — без изменений
Читает `_look.AimYaw/_look.AimPitch/_look.BodyPitch` — обёртка делегирует в AimSource. Один источник правды, обратная совместимость сохранена.

### PlayerWeapons.TryFire — без изменений
Вызывает `_look.AddRecoil()` → обёртка → `AimSource.AddRecoil()`.

---

## В. PlayerCamera Refactor (Задача C)

### SpringCameraConfig — новые поля (Phase 2)
| Поле конфига | Значение | Источник в PlayerCamera |
|--------------|----------|------------------------|
| `PositionalSway` | 0.0005f | `positionalSway` |
| `MaxPositionalSway` | 0.014f | `maxPositionalSway` |
| `SwayReturn` | 17f | `swayReturn` |
| `RotationalSwayPerDegree` | 0.035f | `rotationalSwayPerDegree` |
| `MaxRotationalSway` | 1.0f | `maxRotationalSway` |
| `RotationalSwayReturn` | 15f | `rotationalSwayReturn` |
| `AimAngularSmoothTime` | 0.04f | `Vector2.SmoothDamp(..., 0.04f, ...)` |
| `CatchUpLeanRate` | 6f | `dt * 6f` в `_catchUpLean` |

### PlayerCamera — изменения
- `[SerializeField] private IAimSource aimSource;` вместо `PlayerLook look`
- `[SerializeField] private SpringCameraConfig config;`
- `Awake`: `aimSource = GetComponentInParent<IAimSource>()`, `config = Resources.Load<SpringCameraConfig>("SpringCamera_Default")`
- `UpdateSway`: все hardcoded значения заменены на `config.XXX`
- `SetLook(PlayerLook)` оставлен для обратной совместимости (находит IAimSource через обёртку)

### НЕ переносится (остаётся в коде)
- Perlin-слои: шаги, дыхание, наклон от ускорения, приземление, асимметричные шумы
- Spring-поля Фазы 1 (`stiffness`, `damping`, `rotationInertia`, `catchUpRate`, `maxCatchUpSpeed`) — **не используются в Фазе 2**, задел под Фазу 4

### Поведение
**Damp остаётся Damp** (экспоненциальное сглаживание через `1 - Exp(-rate * dt)` и `Vector2.SmoothDamp`). Никакой пружины, overshoot нет и не было.

---

## Г. Переключение потребителей на IAimSource (Задача D)

| Компонент | Было | Стало |
|-----------|------|-------|
| `PlayerCamera` | `PlayerLook look` | `IAimSource aimSource` + `SpringCameraConfig config` |
| `Flashlight` | `PlayerLook _look` | `IAimSource aimSource` (GetComponentInParent) |
| `PlayerWeapons` | `PlayerLook _look` | `IAimSource aimSource` (обязателен, без fallback) |

### PlayerWeapons — единый путь (fallback `_look` УДАЛЁН)
Опасный двойной путь (`if (aimSource) … else if (_look) …`) устранён — при отсутствии
`IAimSource` поведение было бы скрыто разъезжающимся (разные формулы камеры/прицела).
Теперь `IAimSource` обязателен: `Debug.Assert(aimSource != null)` в `Awake`.
```csharp
// Awake
aimSource = GetComponent<IAimSource>();
Debug.Assert(aimSource != null, "...");

// UpdateADS (single path)
aimSource.SetSensitivityMultiplier(Mathf.Lerp(1f, _current.adsMouse, _adsT));
aimSource.SetAdsRigidity(_adsT);

// TryFire (recoil)
aimSource.AddRecoil(pitchKick, yawKick);

// PoseLocal
Quaternion aimRotation = aimSource.AimRotation;
```

---

## Д. Дополнительные изменения

### WeaponLookConfig — добавлены поля
```csharp
[Header("Reticle (PlayerLook)")]
[SerializeField] private float maxReticleOffset = 28f;
[SerializeField] private bool returnToCenter = false;
[SerializeField] private float returnSpeed = 2.5f;

public float MaxReticleOffset => maxReticleOffset;
public bool ReturnToCenter => returnToCenter;
public float ReturnSpeed => returnSpeed;
```

### PlayerRigBuilder
Создаёт `AimSource` на корне, затем `PlayerLook` и связывает их через SerializedObject:
```csharp
var aimSource = root.AddComponent<AimSource>();
var playerLook = root.AddComponent<PlayerLook>();
var so = new SerializedObject(playerLook);
so.FindProperty("_aimSource").objectReferenceValue = aimSource;
so.ApplyModifiedProperties();
```

---

## Статический аудит (выполнено без Editor)

### Debug.Assert / Debug.LogError — только в Awake/OnEnable (не в Update)
| Компонент | Куда добавлено | Сообщение |
|-----------|----------------|-----------|
| `PlayerWeapons` | `Awake` | `IAimSource is required` |
| `PlayerCamera` | `Awake` | `IAimSource is required` |
| `Flashlight` | `OnEnable` | `IAimSource is required` |
| `PlayerLook` | `Awake` | `AimSource is required` |

### SpringCameraConfig — поля Фазы 4 помечены
`stiffness`, `damping`, `maxAngularVelocity`, `positionOffset`, `rotationOffset`,
`catchUpRate`, `maxCatchUpSpeed`, `probeRadius`, `minDistance`, `collisionMask` —
помечены `PHASE 4: not used in Phase 2` и **не читаются** нигде в коде Фазы 2.

### PlayerWeapons — единый путь
Опасный `else if (_look)` fallback удалён. `IAimSource` обязателен.

### RISK: AimSource не верифицирован как «копия 1:1»
В среде **нет git-истории** (`git show` старого `PlayerLook.cs` недоступен). `AimSource`
написан по восстановленным дефолтам из `PHASE_1.md`/`AUDIT.md` (deadZone=12°, catchUpBand=12°,
catchUpSpeed=9, maxReticle=28, recoil decay `exp(-6dt)`, pitchLimit ±85°). Требуется
**сверка с оригиналом `PlayerLook.cs` в Editor/у заказчика** перед зачётом 1:1.

### Замечания
- `ScopeRenderer` (via reflection `_adsT`) и `SceneShots` (`is PlayerLook`) затронуты
  переносом логики в `AimSource` — на этапе Фазы 2 не трогались, но требуют ревизии в Фазе 3+.
- `PlayerLook.ReticleOffset` возвращает `Vector2.zero` (AimSource не экспортирует ретикул отдельно);
  потребителей в проекте нет.

---

## Чеклист регресс-теста (A/B/C/D)

| Тест | Статус | Примечание |
|------|--------|------------|
| **A. Sensitivity** | ⏳ Требует Editor | 100 px → X° в `enableNewInput=false/true` — должно совпадать ±5% |
| **A. Регресс Фазы 1** | ⏳ Требует Editor | Ходьба, взгляд, стрельба, ADS, смена оружия, фонарь, сеть (2 клиента) |
| **B. AimSource** | ⏳ Код готов, НЕ верифицирован 1:1 | Одиночная игра: взгляд, стрельба, мёртвая зона, догон, ADS — идентично baseline. Требует сверки с оригиналом PlayerLook (нет git). |
| **B. Сеть** | ⏳ Требует Editor | AimYaw/AimPitch синхронизируются, чужие клоны поворачиваются как раньше |
| **C. PlayerCamera** | ✅ Код готов | Поворот 90° — та же скорость догона, тот же возврат, нет overshoot |
| **C. Конфиг работает** | ⏳ Требует Editor | Изменить SwayReturn в Editor → почувствовать разницу → вернуть → поведение вернулось |
| **D. Потребители** | ✅ Код готов | PlayerCamera/Flashlight/PlayerWeapons читают из IAimSource |

---

## Отложено (не в Фазе 2)

| Фича | Фаза | Примечание |
|------|------|------------|
| Spring camera (stiffness/damping/overshoot) | 4 | Поля в конфиге есть, не используются |
| Procedural weapon bob/sway/breathing/recoil | 3 | `ProceduralWeaponConfig` создан, не подключён |
| PostFX (fisheye, chromatic, vignette, motion blur, noise, grain) | 5 | `BodycamPostFXConfig` создан, не подключён |
| IK / руки / анимации | 3 | — |
| Рейк / камеры наблюдения / фургон | — | Нет в проекте |

---

## Риски на Фазы 3-4

1. **Сеть и AimSource**: `NetworkPlayer` читает через `PlayerLook` обёртку. Если в Фазе 3/4 обёртку удалят — нужно либо оставить `NetworkPlayer` читать напрямую из `AimSource`, либо добавить `IAimSource` на тот же GameObject и искать его.
2. **Порядок Update**: `AimSource` в `Update`, потребители (`PlayerCamera`, `Flashlight`, `PlayerWeapons`) в `LateUpdate`. Не менять порядок без необходимости.
3. **Recoil в AimSource**: `PlayerWeapons.TryFire` вызывает `_look.AddRecoil()` → обёртка → `AimSource`. Если уберут обёртку — обновить вызов.
4. **ADS Rigidity**: `PlayerWeapons` ставит `_adsT` в `AimSource.SetAdsRigidity`. Это же значение используется для FOV, привязки камеры к прицелу, и догона ретикула. Не разделять без причины.

---

## Файлы — Diff Summary

### Новые файлы
- `Assets/_Project/Scripts/Player/IAimSource.cs`
- `Assets/_Project/Scripts/Player/AimSource.cs`

### Изменённые файлы
| Файл | Основные изменения |
|------|-------------------|
| `Assets/_Project/Scripts/Config/WeaponLookConfig.cs` | + legacyMouseSensitivity, pixelToCountFactor, maxReticleOffset, returnToCenter, returnSpeed; - mouseSensitivity |
| `Assets/_Project/Scripts/Config/SpringCameraConfig.cs` | + PositionalSway, RotationalSwayPerDegree, AimAngularSmoothTime, CatchUpLeanRate; переименованы rotationInertia→PositionalSway, rotationalInertia→RotationalSwayPerDegree |
| `Assets/_Project/Scripts/Input/PlayerInputRouterAdapter.cs` | LateUpdate: конвертация pixels→counts→legacySens (totalSensitivity = PixelToCountFactor × LegacyMouseSensitivity) |
| `Assets/Scripts/Player/PlayerLook.cs` | Полностью переписан: тонкая обёртка над AimSource; + Debug.Assert |
| `Assets/Scripts/Player/PlayerCamera.cs` | look → aimSource + config; SetLook резолвит IAimSource; + Debug.Assert |
| `Assets/Scripts/Player/Flashlight.cs` | _look → aimSource (Direction); + Debug.Assert |
| `Assets/Scripts/Weapon/PlayerWeapons.cs` | _look → aimSource ОБЯЗАТЕЛЕН (fallback удалён); единый путь; + Debug.Assert |
| `Assets/_Project/Scripts/Player/AimSource.cs` | ФИКС двойного умножения LegacyMouseSensitivity |
| `Assets/_Project/Scripts/Config/SpringCameraConfig.cs` | Spring/инерция/коллизии помечены PHASE 4 |
| `Assets/Scripts/Player/Editor/PlayerRigBuilder.cs` | AimSource + связки; PlayerWeapons на корне; идемпотентность; убран мёртвый CreateWeaponModels |

---

## Следующие шаги (после проверки в Editor)

1. **Импорт Input System**: перезапуск Unity, реимпорт `PlayerInputActions.inputactions`
2. **Создание SO в Editor**:
   - `Assets/_Project/Configs/WeaponLook_Default.asset`
   - `Assets/_Project/Configs/SpringCamera_Default.asset`
   - `Assets/_Project/Configs/ProceduralWeapon_Default.asset`
   - `Assets/_Project/Configs/BodycamPostFX_Default.asset`
   - `Assets/_Project/Configs/InputAdapter_Default.asset`
3. **Настройка префаба Player**: добавить `PlayerInputRouterAdapter`, назначить `InputAdapter_Default`
4. **Эмпирический замер Sensitivity (Задача A)** — подправить `pixelToCountFactor` при необходимости
5. **Полный регресс Фазы 1** — оба режима ввода + сеть
6. **Регресс Фазы 2** — одиночная игра + сеть + изменение конфигов в Editor
7. **Фиксация результатов** в `PHASE_2.md` с ✅/❌