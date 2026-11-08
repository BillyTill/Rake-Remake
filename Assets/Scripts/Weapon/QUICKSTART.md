БЫСТРЫЙ СТАРТ — УСТАНОВКА ЗА 10 МИНУТ
=======================================

##準備物 (Что вам нужно)

**3D модели:**
- Rifle mesh (винтовка)
- Shotgun mesh (дробовик)
- Sight scope mesh (корпус прицела)
- Reticle plane (плоскость для сетки)

**Текстуры:**
- Muzzle flash (жёлто-оранжевый гредиент)
- Reticle overlay (перекрестие на прозрачном фоне)
- Vignette (чёрный градиент)

**Звуки:**
- rifle_shot.wav (выстрел винтовки)
- shotgun_shot.wav (выстрел дробовика)
- reload_sound.wav (звук перезарядки)

---

## ШАГ 1: СОЗДАНИЕ СЛОЁВ

1. Edit → Project Settings → Tags and Layers
2. Добавить слои:
   - Layer 8: `Weapon`
   - Layer 9: `Enemy`
   - Layer 10: `Scope`
   - Layer 11: `ScopeOnly`

---

## ШАГ 2: ИЕРАРХИЯ СЦЕНЫ

```
Scene
├── Player (empty)
│   ├── Main Camera
│   │   └── (PlayerCameraController.cs)
│   │
│   ├── Character Controller (компонент)
│   │   └── (PlayerMovementController.cs)
│   │
│   └── Weapon Pivot (empty)
│       ├── (WeaponController.cs)
│       │
│       ├── Rifle Model
│       │   ├── Muzzle Flash (Quad, Layer: Weapon)
│       │   ├── Muzzle Light (Point Light)
│       │   └── Muzzle Smoke (ParticleSystem)
│       │
│       ├── Shotgun Model (отключен изначально)
│       │   ├── Pump (empty, для анимации)
│       │   ├── Muzzle Flash
│       │   └── Muzzle Light
│       │
│       ├── Scope System (empty)
│       │   ├── (ScopeSystem.cs)
│       │   ├── Scope Camera
│       │   ├── Scope Mesh (Quad)
│       │   └── Reticle Overlay (Quad)
│       │
│       └── (ShotgunController.cs)
│
├── Firearm Manager (empty)
│   ├── (FirearmManager.cs)
│   └── Weapon Animator (empty)
│       └── (WeaponAnimator.cs)
│
└── Canvas (UI)
    ├── (WeaponUIController.cs)
    ├── Ammo Text
    ├── Weapon Name Text
    └── Aiming Indicator (Image)
```

---

## ШАГ 3: НАЗНАЧЕНИЕ СКРИПТОВ И КОМПОНЕНТОВ

### Player (корневой объект)
- Добавить компонент: **CharacterController**
  - Height: 1.8
  - Radius: 0.3
  - Центр: (0, 0.9, 0)

### Main Camera
- Добавить скрипт: **PlayerCameraController.cs**
  - Mouse Sensitivity: 2
  - Max Look Up Angle: 90
  - Max Look Down Angle: 90

### Player (компонент движения)
- Добавить скрипт: **PlayerMovementController.cs**
  - Move Speed: 5
  - Sprint Speed: 8
  - Crouch Speed: 2.5
  - Ground Drag: 5
  - Jump Force: 5

### Weapon Pivot
- Добавить скрипт: **WeaponController.cs**
  - Main Camera: (drag Main Camera)
  - Weapon Model: (drag Rifle Model)
  - Raycast Distance: 1000
  - Sway Amount: 0.02
  - Sway Smoothing: 5
  - Recoil Duration: 0.15
  - Recoil Pitch Curve: AnimationCurve (EaseInOut)
  - Recoil Yaw Curve: AnimationCurve (EaseInOut)

### Rifle Model / Shotgun Model
- Установить Layer: **Weapon**
- Mesh Renderer должен быть включен
- Mesh Filter с вашей моделью

### Muzzle Flash
- Создать Quad
- Position: (0, 0.005, 0.52) относительно модели
- Scale: (0.1, 0.1, 0.1)
- Material: создать новый материал
  - Shader: Unlit/Transparent
  - Текстура: вспышка в дуле (жёлто-оранжевая)
  - Emission Color: (255, 200, 0)

### Muzzle Light
- Тип: Point Light
- Intensity: 2
- Range: 15
- Color: (255, 200, 0)
- Shadows: Off (для производительности)

### Muzzle Smoke
- Создать пустой объект
- Добавить ParticleSystem
- Главные параметры:
  - Duration: 0.15
  - Looping: OFF
  - Start Lifetime: 0.5
  - Emission Rate: 20

### Scope System
- Добавить скрипт: **ScopeSystem.cs**
  - Scope Camera: (drag Scope Camera)
  - Scope Resolution: 1024
  - Scope FOV: 20
  - Scope Culling Mask: слои видимые в прицеле

### Scope Camera
- Новый GameObject с компонентом Camera
- Depth: 1
- Field of View: 20
- Culling Mask: всё КРОМЕ "Weapon"
- Target Texture: создать RenderTexture (1024×1024, Format: Default, Depth: 24)
- Изначально ОТКЛЮЧЕНА (Enable: OFF)

### Scope Mesh
- Quad
- Position: (0, 0, 0.35)
- Scale: (4, 4, 1)
- Material: новый
  - Shader: Unlit/Texture
  - Main Texture: из RenderTexture
  - Alpha: 0.9

### Reticle Overlay
- Quad
- Position: (0, 0, 0.34)
- Scale: (4, 4, 1)
- Material: новый
  - Shader: Unlit/Transparent
  - Текстура: перекрестие (сгенерировать через ReticleGenerator)
  - Alpha: 0.7

### Shotgun Pump (для анимации цевья)
- Пустой GameObject
- Родитель: модель дробовика
- Position: (0, 0, 0.2) (начальная позиция цевья)

### Shotgun Controller
- Добавить на Weapon Pivot скрипт: **ShotgunController.cs**
  - Pump Transform: (drag Shotgun Pump)
  - Pump Distance: 0.15
  - Pump Duration: 0.5
  - Pump Axis: (1, 0, 0)
  - Pump Curve: AnimationCurve (EaseInOut)

### Firearm Manager
- Добавить скрипт: **FirearmManager.cs**
  - Weapon Controller: (drag Weapon Pivot компонент WeaponController)
  - Scope System: (drag Scope System)
  - Shotgun Controller: (drag Shotgun Controller из Weapon Pivot)
  - Weapon Animator: (drag Weapon Animator)
  - Fire Rate: 0.1
  - Ammo In Magazine: 30
  - Total Ammo: 120

### Weapon Animator
- Добавить скрипт: **WeaponAnimator.cs**
  - Muzzle Flash: (drag Muzzle Flash)
  - Muzzle Light: (drag Muzzle Light)
  - Muzzle Smoke: (drag ParticleSystem)
  - Fire Sound: новый AudioSource
  - Fire Sound Variants: массив из 2-3 звуков выстрелов
  - Reload Sound: AudioSource для перезарядки
  - Muzzle Flash Duration: 0.1

### Canvas (UI)
- Добавить скрипт: **WeaponUIController.cs**
  - Firearm Manager: (drag Firearm Manager)
  - Weapon Controller: (drag Weapon Pivot)
  - Scope System: (drag Scope System)
  - Ammo Text: (drag Text элемент)
  - Weapon Name Text: (drag Text элемент)
  - Aiming Indicator: (drag Image элемент)

---

## ШАГ 4: UI ЭЛЕМЕНТЫ

В Canvas создать:

### Ammo Text
- Новый Text (TextMeshPro)
- Position: (0, -40, 0)
- Rotation: (0, 0, 0)
- Size Delta: (200, 50)
- Text: "30 / 120"
- Font Size: 36
- Alignment: Bottom Right

### Weapon Name Text
- Новый Text (TextMeshPro)
- Position: (0, 0, 0)
- Size Delta: (200, 50)
- Text: "ВИНТОВКА"
- Font Size: 24
- Alignment: Top Left

### Aiming Indicator
- Новый Image
- Position: (0, 0, 0)
- Size Delta: (100, 100)
- Image: создать маленький квадрат или точку (красного цвета)
- Enabled: OFF (включается при прицеливании)

---

## ШАГ 5: ПРОВЕРКА И ТЕСТИРОВАНИЕ

1. Нажать Play
2. Проверить:
   - [ ] Мышь вращает камеру
   - [ ] Оружие следует за курсором мыши
   - [ ] ЛКМ стреляет
   - [ ] Видна отдача и восстановление
   - [ ] Есть покачивание оружия
   - [ ] ПКМ активирует прицел
   - [ ] R перезаряжает
   - [ ] 1 и 2 переключают оружие
   - [ ] UI показывает патроны
   - [ ] Звуки воспроизводятся

---

## ШАГ 6: НАСТРОЙКА ПАРАМЕТРОВ

Если что-то не так:

**Оружие не следует за мышью:**
- Проверить, что Main Camera назначена в WeaponController
- Убедиться, что Cursor.lockState = CursorLockMode.Locked в PlayerCameraController

**Отдача не видна:**
- Увеличить recoilPitchDeg в FirearmManager
- Проверить, что recoilDuration > 0

**Прицел не работает:**
- Убедиться, что Scope Camera имеет Target Texture
- Проверить, что Scope Mesh имеет материал с этой текстурой

**Звуки не слышны:**
- Добавить AudioListener на Main Camera
- Убедиться, что AudioClips назначены в WeaponAnimator

---

## ПРИМЕРЫ КОДА ДЛЯ ИНТЕГРАЦИИ С БАЛЛИСТИКОЙ

### Получить позицию и направление ствола:

```csharp
public class BallisticsFirer : MonoBehaviour
{
    [SerializeField] private FirearmManager firearmManager;
    [SerializeField] private Transform muzzleTransform;
    
    private void Start()
    {
        firearmManager.OnFire += FireProjectile;
    }
    
    private void FireProjectile()
    {
        Vector3 muzzlePos = muzzleTransform.position;
        Vector3 muzzleDir = muzzleTransform.forward;
        float muzzleSpeed = firearmManager.CurrentWeapon.muzzleSpeed;
        int pelletCount = firearmManager.CurrentWeapon.pellets;
        
        // Создать снаряды с разбросом
        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 spreadDirection = ApplySpread(muzzleDir);
            SpawnProjectile(muzzlePos, spreadDirection, muzzleSpeed);
        }
    }
    
    private Vector3 ApplySpread(Vector3 baseDirection)
    {
        float spread = firearmManager.CurrentWeapon.spreadHipDeg * Mathf.Deg2Rad;
        Vector3 randomDir = Random.onUnitSphere * spread;
        return (baseDirection + randomDir).normalized;
    }
    
    private void SpawnProjectile(Vector3 pos, Vector3 dir, float speed)
    {
        // Ваш код создания проекции
    }
}
```

### Применить отдачу к баллистике:

```csharp
// В скрипте баллистики:
public void ApplyRecoilEffect()
{
    // Камера слегка смещается от отдачи
    // Это уже обработано в WeaponController.ApplyRecoil()
    // Просто используйте OnFire событие для синхронизации
}
```

---

## ФАЙЛЫ И СТРУКТУРА ПРОЕКТА

```
Assets/
├── Scripts/Weapon/
│   ├── WeaponController.cs
│   ├── ScopeSystem.cs
│   ├── ShotgunController.cs
│   ├── FirearmManager.cs
│   ├── WeaponAnimator.cs
│   ├── PlayerCameraController.cs
│   ├── PlayerMovementController.cs
│   ├── WeaponUIController.cs
│   ├── ReticleGenerator.cs
│   ├── WeaponConfig.cs
│   ├── WeaponDef.cs (уже существует)
│   ├── SETUP_GUIDE_RU.md
│   ├── ARCHITECTURE.md
│   └── QUICKSTART.md (этот файл)
│
├── Audio/Weapons/
│   ├── rifle_shot_01.wav
│   ├── rifle_shot_02.wav
│   ├── shotgun_shot.wav
│   └── reload.wav
│
├── Textures/Weapons/
│   ├── muzzle_flash.png
│   ├── reticle.png
│   ├── vignette.png
│   └── weapon_model.png
│
└── Scenes/
    └── WeaponTestScene.unity
```

---

## ВОЗМОЖНЫЕ ПРОБЛЕМЫ И РЕШЕНИЯ

| Проблема | Решение |
|----------|---------|
| Оружие застревает за камерой | Уменьшить Weapon Pivot Position Z |
| Отдача слишком слабая | Увеличить recoilPitchDeg в WeaponDef |
| Отдача слишком сильная | Уменьшить recoilPitchDeg и kickBackM |
| Прицел размыт | Увеличить RenderTexture разрешение до 2048 |
| Мышь не работает | Проверить Cursor.lockState и Input settings |
| UI не видно | Проверить Canvas RenderMode и sorting order |
| Звуки потеряны | Убедиться, что AudioListener есть на Main Camera |
| Дробовик не анимируется | Проверить, что pumpTransform назначен в ShotgunController |

---

## ФИНАЛЬНЫЙ ЧЕКЛИСТ

```
✓ Созданы все слои (Weapon, Enemy, Scope, ScopeOnly)
✓ Иерархия сцены соответствует структуре
✓ PlayerCameraController назначен на Main Camera
✓ PlayerMovementController назначен на Player
✓ WeaponController назначен на Weapon Pivot
✓ FirearmManager назначен и сконфигурирован
✓ Все компоненты имеют назначенные ссылки
✓ AnimationCurves созданы и настроены
✓ Звуки добавлены в Audio/Weapons/
✓ Текстуры добавлены и импортированы
✓ RenderTexture создана (1024x1024)
✓ Culling Mask правильно настроен на камерах
✓ UI Canvas создан с необходимыми элементами
✓ Все скрипты откомпилированы без ошибок
✓ Сцена сохранена
✓ Готово к Play!
```

После выполнения всех шагов система полностью работоспособна.

