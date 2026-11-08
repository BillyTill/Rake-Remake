ПОЛНАЯ ИНСТРУКЦИЯ ПО НАСТРОЙКЕ СИСТЕМЫ ОРУЖИЯ FPS
================================================

## 1. АРХИТЕКТУРА СЦЕНЫ

Структура иерархии GameObject в Unity:

```
Player (Root)
├── Main Camera (Camera)
│   └── Camera Script (Camera Controller)
│
├── Weapon Pivot (empty GameObject, WeaponController.cs)
│   ├── Weapon Model (Rifle or Shotgun mesh)
│   │   ├── Muzzle Flash (Quad with emissive material)
│   │   ├── Muzzle Light (Point Light)
│   │   └── Muzzle Smoke (Particle System)
│   │
│   ├── Scope System (empty, ScopeSystem.cs)
│   │   ├── Scope Camera (Camera, отдельная камера)
│   │   ├── Scope Mesh (Quad с материалом для RenderTexture)
│   │   └── Reticle Overlay (Quad с сеткой прицела)
│   │
│   └── Shotgun Pump (Transform, для анимации цевья)
│
├── Firearm Manager (empty, FirearmManager.cs)
└── Weapon UI (Canvas с UI элементами)
```

## 2. СЛОИ (LAYERS) И CULLING MASK

Создать слои в Unity: Edit > Project Settings > Tags and Layers

Нужные слои:
- `Default` (0) — обычные объекты
- `Player` (8) — модель оружия и руки игрока
- `Enemy` (9) — враги и объекты окружения
- `Weapon` (10) — модель оружия (для основной камеры)
- `Scope` (11) — видимо только для камеры прицела

Настройка Culling Mask:
- Main Camera: видит всё КРОМЕ `Weapon` слоя
- Scope Camera: видит ТОЛЬКО слои, которые должны быть видны в прицеле (обычно `Enemy` и `Default`)

## 3. ПОШАГОВАЯ НАСТРОЙКА

### ШАГ 1: Создание иерархии

1. Создать пустой GameObject "Player" (корень сцены)
2. Добавить Main Camera как дочерний объект Player
3. Создать пустой GameObject "Weapon Pivot" как дочерний Player
4. В Weapon Pivot поместить модель оружия (Rifle или Shotgun)

### ШАГ 2: Настройка Main Camera

1. Выбрать Main Camera
2. Установить параметры:
   - Position: (0, 0, 0) — в центре головы игрока
   - Field of View: 60
   - Culling Mask: всё КРОМЕ слоя "Weapon"
   - Near Clip Plane: 0.01 (чтобы видеть оружие близко)

3. Добавить компонент Camera Script (если есть)

### ШАГ 3: Настройка Weapon Pivot

1. Выбрать "Weapon Pivot" GameObject
2. Добавить компонент WeaponController.cs
3. В Inspector назначить:
   - Main Camera: drag Main Camera сюда
   - Weapon Model: drag модель оружия сюда
   - Raycast Distance: 1000
   - Sway Amount: 0.02
   - Sway Smoothing: 5
   - Recoil Pitch Curve: AnimationCurve (EaseInOut)
   - Recoil Yaw Curve: AnimationCurve (EaseInOut)
   - Recoil Duration: 0.15

4. Установить Position Weapon Pivot: (0, -0.1, 0.3) — немного вперёд от камеры

### ШАГ 4: Настройка модели оружия

1. Выбрать "Weapon Model" (меш оружия)
2. Установить слой: "Weapon"
3. Дочерние объекты музля:
   - Создать Quad "Muzzle Flash"
   - Создать Point Light "Muzzle Light"
   - Создать Particle System "Muzzle Smoke"
4. Позиция Muzzle Flash: (0, 0.005, 0.52) относительно модели

### ШАГ 5: Настройка Scope System

1. Создать пустой GameObject "Scope System" внутри Weapon Pivot
2. Добавить компонент ScopeSystem.cs
3. Создать Scope Camera:
   - New GameObject "Scope Camera" внутри Scope System
   - Добавить Camera компонент
   - Position: совпадает с позицией Main Camera (за счёт родительства)
   - Field of View: 20 (для увеличения)
   - Depth: 1 (выше, чем Main Camera)
   - Target Texture: создать новую RenderTexture (1024x1024, Depth 24)
   - Culling Mask: слои, которые видны в прицеле (например, "Enemy" + "Default")

4. Создать Scope Mesh (quad для вывода RenderTexture):
   - New Quad "Scope Mesh" внутри Scope System
   - Scale: (2, 2, 1) — заполнить большую часть экрана
   - Position: (0, 0, 0.5) — перед камерой
   - Материал: создать материал с шейдером Standard
   - В материале установить текстуру из RenderTexture

5. Создать Reticle Overlay:
   - New Quad "Reticle Overlay"
   - Position: (0, 0, 0.4) — чуть перед Scope Mesh
   - Материал: создать материал с прозрачным перекрестием
   - Или использовать ReticleGenerator для генерации текстуры

6. В Inspector ScopeSystem назначить:
   - Scope Camera: Scope Camera
   - Scope Render Texture: созданная RenderTexture
   - Scope Material: материал Scope Mesh
   - Reticle Mesh Renderer: Renderer компонент Reticle Overlay
   - Weapon Model: модель оружия
   - Scope Resolution: 1024
   - Scope FOV: 20

### ШАГ 6: Настройка Shotgun (если используется дробовик)

1. Создать пустой GameObject "Shotgun Pump" внутри модели дробовика
2. Это будет цевьё (передвигающаяся часть)
3. Добавить компонент ShotgunController.cs на Weapon Pivot
4. В Inspector назначить:
   - Pump Transform: Shotgun Pump
   - Pump Curve: AnimationCurve
   - Pump Distance: 0.15
   - Pump Duration: 0.5
   - Pump Axis: (1, 0, 0) для движения вперёд

### ШАГ 7: Настройка Firearm Manager

1. Создать пустой GameObject "Firearm Manager" в корне сцены
2. Добавить компонент FirearmManager.cs
3. В Inspector назначить:
   - Weapon Controller: Weapon Pivot (WeaponController)
   - Scope System: Scope System
   - Shotgun Controller: Weapon Pivot (ShotgunController)
   - Weapon Animator: (создать ниже)
   - Fire Rate: 0.1
   - Ammo In Magazine: 30
   - Total Ammo: 120

### ШАГ 8: Настройка Weapon Animator

1. Создать пустой GameObject "Weapon Animator" как дочерний Firearm Manager
2. Добавить компонент WeaponAnimator.cs
3. В Inspector назначить:
   - Muzzle Flash: Transform музля
   - Muzzle Light: Point Light
   - Muzzle Smoke: Particle System
   - Fire Sound: AudioSource
   - Fire Sound Variants: массив звуков выстрелов
   - Reload Sound: AudioSource для звука перезарядки
   - Muzzle Flash Curve: AnimationCurve (EaseInOut)
   - Muzzle Flash Duration: 0.1

### ШАГ 9: Настройка UI

1. Создать Canvas "Weapon UI"
2. Добавить Text элементы:
   - "Ammo Text" — текущее количество патронов
   - "Weapon Name" — название оружия
3. Создать Image "Aiming Indicator" — индикатор прицеливания
4. Добавить компонент WeaponUIController.cs на Canvas
5. В Inspector назначить все UI элементы

## 4. МАТЕРИАЛЫ И ШЕЙДЕРЫ

### Материал вспышки музля (Muzzle Flash)
- Shader: Unlit/Transparent
- Главная текстура: жёлто-оранжевый градиент или плазма
- Emission: включить и максимизировать
- Alpha: 0.8

### Материал RenderTexture для прицела
- Shader: Standard или Unlit/Transparent
- Main Texture: из RenderTexture
- Emission: 0.5 для видимости в любом освещении

### Материал сетки прицела
- Shader: Unlit/Transparent
- Текстура: тонкие линии перекрестия на прозрачном фоне
- Alpha: 0.6-0.8

## 5. СОЗДАНИЕ ТЕКСТУР

### Способ 1: Использовать ReticleGenerator

```csharp
ReticleGenerator generator = GetComponent<ReticleGenerator>();
Texture2D reticle = generator.GenerateReticleTexture(512);
Texture2D vignette = generator.GenerateVignetteTexture(512);
```

### Способ 2: В Photoshop/GIMP

1. Создать изображение 512x512 или 1024x1024
2. На прозрачный слой нарисовать:
   - Перекрестие: тонкие белые линии (2-3 px)
   - Центральная точка: маленький квадрат или точка
   - Метки расстояния: маленькие штрихи на расстояниях
3. Сохранить как PNG с альфа-каналом
4. Импортировать в Unity, установить:
   - Texture Type: Sprite (2D and UI)
   - Filter Mode: Trilinear
   - Compression: High Quality

## 6. ЗВУКИ

### Звуки выстрелов
- Создать несколько вариантов (для разнообразия)
- Обычная винтовка: "rifle_shot.wav"
- Дробовик: "shotgun_shot.wav"
- Импортировать в Assets/Audio/Weapons/

### Звуки перезарядки
- "rifle_reload.wav"
- "shotgun_pump.wav"

### Настройка AudioClips
- Compression Format: адаптивный
- Load Type: Streaming (для больших файлов)

## 7. PARTICLE SYSTEMS

### Дым в дуле
1. Создать Particle System "Muzzle Smoke"
2. Duration: 0.15
3. Looping: OFF
4. Emission:
   - Rate over Time: 20
5. Shape: Cone (20 градусов)
6. Velocity over Lifetime: небольшой разброс
7. Size over Lifetime: от 0.5 до 1.0
8. Lifetime: 0.5 секунды

## 8. КЛЮЧЕВЫЕ КОМПОНЕНТЫ И ТЕГИ

Убедиться, что объекты помечены:

```
Player — Tag: Player
Main Camera — Tag: MainCamera
Weapon Model — Layer: Weapon
Muzzle Flash — Layer: Default
Scope Camera — Layer: Default
```

## 9. ТЕСТИРОВАНИЕ И ОТЛАДКА

1. Запустить сцену (Play)
2. Проверить:
   - ✓ Оружие следует за мышью плавно
   - ✓ Отдача после выстрела
   - ✓ Покачивание оружия в покое
   - ✓ Правая кнопка мыши активирует прицел
   - ✓ В прицеле видна сетка и ограничение FOV
   - ✓ R перезаряжает оружие
   - ✓ 1 и 2 переключают оружие
   - ✓ Патроны уменьшаются при стрельбе
   - ✓ Дробовик имеет более сильную отдачу

## 10. ОПТИМИЗАЦИЯ

1. Scope Camera по умолчанию отключена (включается только при ADS)
2. RenderTexture 1024x1024 — оптимальный баланс
3. Culling Mask правильно настроен для избежания рендера лишних объектов
4. AnimationCurves кешируются при старте
5. Minimal GC: избегать выделения памяти в Update()

## 11. РАСШИРЕНИЯ И УЛУЧШЕНИЯ

Возможные дальнейшие добавления:
- Screen shake при отдаче (трясутся камера и оружие)
- Ветер влияет на траекторию (для баллистики)
- Разные типы патронов (разная отдача и скорость)
- Крепления для оружия (прицелы, компенсаторы отдачи)
- Анимация магазина при перезарядке
- Звуковые эффекты окружающей среды

================================================
КОНЕЦ ИНСТРУКЦИИ
================================================
