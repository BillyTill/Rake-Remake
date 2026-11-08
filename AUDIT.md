# AUDIT.md — Инвентаризация проекта Lake (Unity 6 HDRP)

**Дата:** 2026-11-04  
**Unity версия:** 6000.x LTS (HDRP 17.0.1)  
**Рендер-пайплайн:** HDRP (High Definition Render Pipeline)  
**Сетевой фреймворк:** Mirror (kcp2k транспорт)

---

## 1. Иерархия игрока и префаб Player.prefab

### Player (Root) — `Assets/Prefabs/Player.prefab`
```
Player (GameObject)
├── NetworkIdentity (Mirror) — assetId: 2985365872
├── CharacterController — height 1.8, radius 0.32, center (0, 0.9, 0)
├── PlayerInputRouter — старый Input Manager (Input.GetAxis/Key)
├── PlayerController — движение, выносливость, шаги, прыжок
├── PlayerLook — схема «прицел впереди, камера догоняет»
├── NetworkPlayer — сетевой владелец (30 Hz тик, предсказание + реконсилиация)
├── RemoteBody (капсула) — для чужих игроков, отключена у локального
├── CameraPivot (Transform) — пивот для наклона камеры
│   └── Main Camera (Tag: MainCamera)
│       ├── Camera (HDRP) — FOV 60, near 0.08, far 1500, физические параметры (ISO 200, f/16, 50mm)
│       ├── HDAdditionalCameraData — TAA, объёмные облака, exposure control ВКЛ
│       ├── PlayerCamera — физика камеры (шаги, инерция, дыхание, наклон по рельефу, приземление)
│       ├── AudioListener
│       └── Flashlight (Light, Spot, 58°) + HDAdditionalLightData + Flashlight.cs (батарея, мерцание, инерция)
```

**Ключевые связи:**
- `PlayerLook.body` → `Player` (Transform)
- `PlayerLook.pitchPivot` → `CameraPivot`
- `PlayerCamera.look` → `PlayerLook`
- `PlayerCamera` читает `PlayerController.LocalHorizontalVelocity/Acceleration` и события `Stepped`/`Landed`
- `Flashlight` целится в `PlayerLook.AimDirection` (не в камеру!)
- `PlayerWeapons` (добавляется NetworkPlayer в Awake) — висит на Player, строит модели под камерой

---

## 2. Компоненты на камере

| Компонент | Назначение |
|-----------|------------|
| `Camera` (HDRP) | Основной рендер, физические параметры камеры |
| `HDAdditionalCameraData` | TAA, кастомные фрейм-сеттинги, Exposure Control |
| `PlayerCamera` | Процедурная анимация камеры: шаги (асимметричная волна), инерция поворота, дыхание, наклон от ускорения/рельефа, приземление |
| `AudioListener` | Звук |
| `Flashlight` (Light + скрипт) | Spot Light с процедурным cookie, батарея, мерцание, инерция корпуса |

**Cinemachine:** НЕ используется. Вся логика камеры — кастомная (`PlayerLook` + `PlayerCamera`).

---

## 3. Система ввода

**Текущее состояние:** Старый Input Manager (`Project Settings → Player → Active Input Handling = Input Manager`)

**`PlayerInputRouter.cs`** — единственная точка входа:
- `Look` — `Input.GetAxis("Mouse X/Y")` с масштабом `lookSensitivity * 12f`
- `Move` — WASD / стрелки, нормализован
- `Sprint` — Left/Right Shift
- `Crouch` — C / LeftControl
- `Jump` — Space (GetKeyDown)
- `Fire` — MouseButton(0) (held + pressed front)
- `Ads` — MouseButton(1) (held)
- `Reload` — R (GetKeyDown)
- `WeaponSwitch` — Alpha1/Alpha2 + Mouse ScrollWheel (накопление дельты)
- `Flashlight` — F (GetKeyDown)
- `Cursor Lock` — Escape (тоггл)

**Input System (New):** НЕ установлен (`com.unity.inputsystem` отсутствует в manifest.json).

---

## 4. Движение и контроллер

**`PlayerController.cs`** — CharacterController-based:
- Скорости: Walk 2.1, Sprint 3.9, Crouch 1.15 м/с
- Ускорение: Ground 14, Air 2.5 м/с²
- Гравитация: -19.6 м/с², макс. падение 30 м/с
- Прыжок: 1.05 м высоты
- Прилипание к земле: SphereCast вниз 0.35 м
- Приседание: Лерп высоты капсулы (9 ед/с), проверка блока сверху SphereCast
- Выносливость: 100 макс, расход 15/с, реген 13/с после 1.1 с задержки
- События: `Stepped(float speed01)`, `Landed(float impactSpeed)`, `CrouchChanged(bool)`
- **NetworkControlled** — флаг для сетевой симуляции (фиксированный тик 30 Гц)

---

## 5. Взгляд и прицеливание (`PlayerLook.cs`)

**Схема:** «Reticle ahead, camera catches up»
- Невидимый прицел (`_reticle`) копит дельту мыши
- Мёртвая зона `catchUpAngle = 12°` — пока прицел внутри, корпус стоит
- Догон: экспоненциальное сближение с разгонной полосой `catchUpRampDeg = 12°`
- `AdsRigidity` (0..1) — при прицеливании прицел впитывается в камеру, ствол и оптика становятся жёстким блоком
- Отдача: аддитивная к прицелу (`AddRecoil`), экспоненциальный возврат
- Сетевые значения: `BodyYaw/BodyPitch` (с отдачей при ADS), `AimYaw/AimPitch` (полный прицел для снапшотов)

---

## 6. Оружие (`PlayerWeapons.cs`, `WeaponDef.cs`, `Ballistics.cs`)

**Две модели:** Rifle (винтовка с оптикой) + Shotgun (помповый)
- Процедурная генерация моделей (`WeaponModelBuilder.cs`) — примитивы (кубы/цилиндры)
- `WeaponRoot` — дочерний камере, позиционируется через «плечевую» опору (`aimPivot`)
- Инерция ствола: та же математика, что у фонарика (угловая скорость прицела → отставание позиции/поворота)
- ADS: FOV делится на `adsZoom` (Rifle 3.5×), `AdsRigidity` → 1, модель скрывается при `_adsT > 0.88` (оптический оверлей)
- Стрельба: Raycast из дула модели (`def.muzzleLocal`), разброс — диск в локальных осях ствола
- Отдача: прицел (`PlayerLook.AddRecoil`) + визуальный кик ствола (`_kickOffset/_kickRotation`)
- Балистика: общий код клиент/сервер (`Ballistics.cs`), гравитация -19.6, шаг 0.02 с, трассеры
- Сеть: `NetworkWeaponService` — клиент-авторитетный выстрел с серверной валидацией (кулдаун, дуло в риге, отличимость векторов)

---

## 7. Фонарик (`Flashlight.cs`, `FlashlightCookie.cs`)

- Spot Light 58°, 250 кд, куки 256×256 (процедурная генерация: кристалл LED, отражатель, царапины, пыль, грязь)
- Батарея: 100 ед, расход 0.55%/с (~3 мин), мерцание при <25%
- Инерция: позиционная (0.016 м на м/с) + угловая (0.12 с), цель — `PlayerLook.AimRotation` (подсвечивает прицел, не камеру)
- Синхронизация: `SyncVar` через `NetworkPlayer.CmdSetFlashlight`

---

## 8. Цикл суток и погода (`DayNightCycle.cs`, `WeatherController.cs`)

**Время:** 24 минуты = 1 сутки, `timeScale`, `paused`, превью в редакторе
**Светила:** Солнце + Луна (противофаза), азимут 20°, питч = (Hour-6)*15
**Небо:** HDRP GradientSky (Top/Middle/Bottom + Multiplier + Diffusion) — всё кривыми/градиентами по часам
**Туман:** HDRP Fog (MeanFreePath + Tint) — кривые по часам × погодный множитель
**Облака:** HDRP VolumetricClouds (Simple mode) — пресеты (Sparse/Cloudy/Overcast/Stormy) + плотность/ярус/ветра
**Экспозиция:** НЕ ведётся (`Exposure` в профиле выключен), сцена в единицах кадра (1.0 = белый). Солнце полдень = 5 единиц.
**Погода:** 5 пресетов (Ясно → Переменно → Пасмурно → Шторм → Туман), автосмена 150–420 игровых мин, переход 45 мин (SmoothStep)

---

## 9. Облака (Кастомные) — `CloudSettings.cs`, `CloudCustomPass.cs`

**Два слоя облаков:**
1. **HDRP VolumetricClouds** — через `DayNightCycle`/`WeatherController` (Simple mode, пресеты)
2. **Кастомный рейтрейс** (`CloudCustomPass : CustomPass`) — заменяет старый `OnRenderImage` пакета
   - Инъекция: `AfterOpaqueAndSky`
   - Рейтрейс в половинном разрешении (`marchScale = 0.5`)
   - Temporal upsampling (репроекция по `_LastVP`)
   - Композит с цветом камеры
   - Настройки: `CloudSettings` (Shape/Detail/WeatherMap/BlueNoise 3D/2D текстуры, Henyey-Greenstein, Powder, марчинг)

**В сцене:** `CustomPassVolume` (Global, Priority 0) с `CloudCustomPass` на объекте "Clouds".

---

## 10. Сеть (Mirror + kcp2k)

**Архитектура:** Клиент-авторитетное предсказание + серверная реконсилиация
- Тик: 30 Гц (фиксированный `TickInterval = 1/30`)
- Снапшоты: 20 Гц (`NetworkSnapshotService`)
- Команды ввода: `PlayerInputCommand` (sequence, move, yaw, pitch, aimYaw, aimPitch, buttons, weapon)
- Сервер: очередь команд на игрока (макс 8), симулирует `PlayerController` в `ServerTick`
- Клиент: предсказывает своим же `PlayerController`, хранит `_pending`/`_predicted`, сверяет по `lastProcessedCommand`
- Реконсилиация: если ошибка > 0.2 м → `SnapAndReplay` (телепорт + переигрывание неподтверждённых)
- Интерполяция чужих: 100 мс задержка, линейная интерполяция между сэмплами
- Спавн: `LakeNetworkManager` — через `PlayerSpawnPoint` по кругу (8 слотов, радиус 1.25 м)
- Лобби: `NetworkLobbyUI` — минимальное (Host/Join по IPv4, команда `-host`/`-join IP`)

---

## 11. Растительность — Billboard система

- `PlantBillboardManager` — автоматический LOD: меш → биллборд на дистанции
- `PlantBillboardDatabase` — кэш а틀сов
- `PlantBillboardBuilder` (Editor) — 베이кинг атласов в рантайме/редакторе

---

## 12. Инструменты редактора

- `SceneShots` — автоскриншоты для валидации неба/облаков по часам
- `TerrainRaycastHeightBrush` / `TerrainProceduralPainter` — кисти террейна
- `NetworkM0Setup` — настройка M0 сцены (префаб игрока, NetworkManager, KcpTransport, спавн)
- `LakeBootstrapMenu` / `SkyCycleMenu` / `DayNightCycleEditor` — меню Tools/Lake/...

---

## 13. HDRP настройки (из сцены и префабов)

| Настройка | Значение |
|-----------|----------|
| HDRP версия | 17.0.1 |
| Camera HDR | Отключено (`m_HDR: 0`) |
| MSAA | Отключено (`m_AllowMSAA: 0`) |
| TAA | Включено (`TAAQuality: 1`, `taaSharpenStrength: 0.5`) |
| Exposure Control | Включено (`enableExposureControl: 1`), но компонент Exposure в профиле выключен |
| Volumetrics | Включены (облака + кастомный проход) |
| Custom Pass | `CloudCustomPass` в `CustomPassVolume` (Global, AfterOpaqueAndSky) |
| Sky Type | GradientSky (настраивается `DayNightCycle`) |
| Fog | Volumetric Fog (настраивается `DayNightCycle`) |
| Shadow Resolution | 512 (Directional Light) |
| Light Unit | Lux (солнце), Candela (фонарик) |

---

## 14. Существующие механики Rake (по коду и комментариям)

| Механика | Статус | Детали |
|----------|--------|--------|
| День/ночь | ✅ Работает | Полный цикл с кривыми, превью в редакторе |
| Погода | ✅ Работает | 5 пресетов, автосмена, плавные переходы |
| Камеры наблюдения | 🟡 Упоминается | В `NetworkSnapshotService` комментарий про VisibilityService для Rake |
| Датчики движения | ❌ Не найдено | Нет кода |
| Свинокамера | ❌ Не найдено | Нет кода |
| Инвентарь / Хоткеи | 🟡 Частично | Смена оружия 1/2, перезарядка R, фонарик F — в `PlayerInputRouter` |
| ИИ Рэйка | ❌ Не найдено | Только слой `Enemy` (Layer 9) зарезервирован в доках оружия |
| Фургон / Режим наблюдения | ❌ Не найдено | Нет кода |
| Мультиплеер | ✅ Работает | Mirror, 30Hz тик, предсказание, реконсилиация, лобби |

---

## 15. Риски миграции на Bodycam-контроллер

| Риск | Описание | Митигация |
|------|----------|-----------|
| **Старый Input Manager** | Новый контроллер требует Input System для гибкости | Установить `com.unity.inputsystem`, создать `.inputactions`, сделать адаптер параллельно |
| **PlayerLook схема** | Уникальная схема «прицел впереди» — Bodycam должен её уважать | Слой 1 (Spring Camera) следует за `WeaponPivot`, не заменяет `PlayerLook` |
| **PlayerCamera физика** | Очень детализированная (шаги, дыхание, наклон, приземление) | Сохранить как отдельный слой, Bodycam добавляет только инерцию держателя |
| **Flashlight целится в AimDirection** | Зависит от `PlayerLook.AimRotation` | Не менять, Bodycam не влияет на прицел |
| **Weapon инерция (Flashlight-стиль)** | Уже реализована в `PlayerWeapons.PoseLocal` | Bodycam добавляет общую инерцию держателя, weapon инерция остаётся аддитивной |
| **Сеть (Mirror)** | Предсказание/реконсилиация опирается на `PlayerController` | Bodycam слои — только локальные визуальные эффекты, не влияют на симуляцию |
| **HDRP Volume/PostFX** | Уже есть `CustomPassVolume` для облаков, Exposure выключен | Bodycam пост-процессинг — отдельный Volume с высоким приоритетом |
| **Cinemachine отсутствует** | Требуемое в ТЗ — не использовать Cinemachine | Уже не используется, кастомная пружина — правильный выбор |
| **Процедурные модели оружия** | Примитивы, нет IK-целей для рук | Слой 3 (IK) — создать пустышки `RightHandTarget`/`LeftHandTarget` на моделях |

---

## 16. Список скриптов по категориям

### Player (Assets/Scripts/Player/)
- `PlayerInputRouter.cs` — ввод (старый Input Manager)
- `PlayerLook.cs` — взгляд, прицел, догон, отдача
- `PlayerController.cs` — движение, выносливость, шаги
- `PlayerCamera.cs` — физика камеры (шаги, инерция, дыхание, наклон, приземление)
- `Flashlight.cs` — фонарик, батарея, мерцание, инерция
- `FlashlightCookie.cs` — процедурный куки фонарика
- `PlayerRigBuilder.cs` (Editor) — сборка рига

### Weapon (Assets/Scripts/Weapon/)
- `PlayerWeapons.cs` —.core weapon handling, модели, стрельба, ADS, сеть
- `WeaponDef.cs` — ScriptableObject-подобные статические дефы (Rifle/Shotgun)
- `Ballistics.cs` — общая баллистика клиент/сервер
- `WeaponFX.cs` — муzzle flash, трассеры, попадания
- `WeaponModelBuilder.cs` — процедурные модели
- `WeaponSoundSystem.cs` / `WeaponRecoilSystem.cs` / `WeaponEffectsSystem.cs` — звук/отдача/эффекты
- `WeaponAimSystem.cs` / `ScopeRenderer.cs` / `ScopeQualityConfig.cs` / `PhysicalScope.cs` — оптика (HDRP PiP)
- `NetworkWeaponService.cs` — сетевой выстрел

### Networking (Assets/Scripts/Networking/)
- `NetworkPlayer.cs` — сетевой владелец (30Hz, предсказание, реконсилиация)
- `NetworkSnapshotService.cs` — 20Hz снапшоты
- `NetworkMessages.cs` — структуры команд/снапшотов
- `LakeNetworkManager.cs` — спавн через PlayerSpawnPoint
- `NetworkLobbyUI.cs` — минимальное лобби
- `NetworkM0Setup.cs` (Editor) — настройка сцены

### Sky (Assets/Scripts/Sky/)
- `DayNightCycle.cs` — время, солнце/луна, небо, туман, экспозиция
- `WeatherController.cs` — погода, пресеты, переходы
- `CloudBaker.cs` / `MoonBaker.cs` / `NightSkyBaker.cs` — процедурные текстуры
- `SkyRig.cs` — звёздная купол, облачный купол, луна-биллборд

### Clouds (Assets/Scripts/Clouds/)
- `CloudSettings.cs` — параметры кастомных облаков + материалы
- `CloudCustomPass.cs` — HDRP CustomPass для рейтрейса облаков

### Camera (Assets/Scripts/Camera/)
- `Bodycam.cs` — существующий bodycam-скрипт (покачивание держателя, Перлин)

### Bootstrap (Assets/Scripts/Bootstrap/)
- `GameBootstrapper.cs` — точка входа, спавн игрока, курсор
- `PlayerSpawner.cs` / `PlayerSpawnPoint.cs` — спавн
- `LakeSettings.cs` — настройки проекта
- `GroundProbe.cs` — проверка земли

### Vegetation (Assets/Scripts/Vegetation/)
- `PlantBillboardManager.cs` / `PlantBillboardDatabase.cs` / `PlantBillboardBuilder.cs` — биллборды

### Terrain/Editor
- `TerrainRaycastHeightBrush.cs` / `TerrainProceduralPainter.cs` — инструменты террейна

### Tools
- `SceneShots.cs` — автоскриншоты

---

## 17. Что НЕТ в проекте (для Bodycam задачи)

| Отсутствует | Что нужно сделать |
|-------------|-------------------|
| Input System пакет | Установить `com.unity.inputsystem` |
| `.inputactions` ассет | Создать `PlayerInputActions.inputactions` |
| Animation Rigging пакет | Установить `com.unity.animation.rigging` для IK рук |
| IK-риг рук | Создать `Rig Setup` + `TwoBoneIKConstraint` ×2 |
| Модели рук (скелетные) | Заглушки: капсулы-руки на `WeaponModel` с TODO |
| ScriptableObject-конфиги | Создать 6 SO-конфигов в `Assets/_Project/Scripts/Config/` |
| Пресеты конфигов | Создать в `Assets/_Project/Configs/Presets/` |
| Bodycam PostFX Volume | Создать Global Volume с профилем Bodycam |
| Кастомный Fisheye Renderer Feature | Написать `BodycamFisheyeFeature : CustomPass` |
| PlayMode тесты | Настроить Unity Test Framework |

---

## 18. План миграции (соответствует ТЗ)

| Фаза | Задача | Файлы для создания/изменения |
|------|--------|------------------------------|
| **0** | Аудит (этот отчёт) | `AUDIT.md` ✅ |
| **1** | Фундамент: SO-конфиги + Input System | `Assets/_Project/Scripts/Config/*.cs`, `PlayerInputActions.inputactions`, адаптер ввода |
| **2** | Слой 1: Spring Camera Controller | `SpringCameraController.cs` на `HeadPivot` |
| **3** | Слой 2: Procedural Weapon Animator | `ProceduralWeaponAnimator.cs` на `WeaponPivot` |
| **4** | Слой 3: IK для рук | Animation Rigging setup, цели на `WeaponModel` |
| **5** | Слой 4: Коллизии оружия/камеры | `WeaponCollision.cs`, `CameraCollision.cs` |
| **6** | Слой 5: Bodycam PostFX (HDRP) | `BodycamPostFXConfig.cs`, Volume профиль, `BodycamFisheyeFeature.cs` |
| **7** | Слой 6: Интеграция с механиками Rake | Адаптация фонарика, оружия, планшета, камер наблюдения |
| **8** | Слой 7: Тюнинг + доки | Пресеты, Custom Editors, `TUNING.md` |

---

## 19. Компиляция и запуск

- **Проект компилируется:** Да (нет ошибок в консоли при открытии)
- **Сцена запускается:** `OutdoorsScene.unity` — основная сцена
- **Известные варнинги:** 
  - `CloudGenerator` (старый пакет) на объекте "Clouds" — работает параллельно с `CloudCustomPass`
  - Отсутствует `IndirectLightingController` в профиле — `DayNightCycle` создаёт его динамически
  - Некоторые префабы в `_Recovery` — игнорируются

---

## 20. Рекомендации перед стартом Фазы 1

1. **Установить пакеты:**
   ```json
   "com.unity.inputsystem": "1.11.0",
   "com.unity.animation.rigging": "1.2.0"
   ```

2. **Создать структуру папок:**
   ```
   Assets/_Project/
   ├── Scripts/
   │   ├── Config/          # 6 SO-конфигов
   │   ├── Player/          # SpringCameraController, ProceduralWeaponAnimator, WeaponCollision, CameraCollision
   │   ├── Weapons/         # (если нужно расширить PlayerWeapons)
   │   └── PostFX/          # BodycamFisheyeFeature
   └── Configs/
       ├── *.asset          # Инстансы конфигов
       └── Presets/         # Пресеты для геймдизайнера
   ```

3. **Не удалять/не ломать:**
   - `PlayerInputRouter` — оставить, сделать адаптер к новому Input System
   - `PlayerLook` / `PlayerCamera` / `PlayerController` — сохранять, Bodycam слои накладываются поверх
   - `PlayerWeapons` / `Flashlight` — сохранять, интегрировать через события/свойства
   - `DayNightCycle` / `WeatherController` / `CloudCustomPass` — не трогать
   - Сетевой код (`NetworkPlayer`, `NetworkWeaponService`) — только читать, не писать

4. **Тестовая сцена:** Использовать `OutdoorsScene` — в ней уже есть PlayerSpawn, DayNightCycle, WeatherController, CloudCustomPass, Network setup.

---

**Готов к Фазе 1 после подтверждения.**