АРХИТЕКТУРА СИСТЕМЫ ОРУЖИЯ FPS
==============================

## КОМПОНЕНТЫ И ИХ ВЗАИМОДЕЙСТВИЕ

### 1. WeaponController.cs
Главный контроллер оружия. Управляет:
- Наведением на экранную точку (ScreenPointToRay + Quaternion.Slerp)
- Отдачей с использованием AnimationCurves
- Процедурным покачиванием (sway)
- Позицией оружия в пространстве камеры

**Входные данные:**
- Позиция мыши (Input.mousePosition)
- Текущий WeaponDef для параметров отдачи

**Выходные данные:**
- Трансформация Weapon Pivot (поворот и смещение)

**Key Methods:**
- UpdateWeaponAim() — наведение оружия на точку курсора
- ApplyRecoil() — применить отдачу при выстреле
- SetAiming(bool) — переключить режим прицеливания

### 2. ScopeSystem.cs
Система снайперского прицела с физическим рендерингом:
- Отдельная Scope Camera рендерит в RenderTexture
- RenderTexture отображается на материале линзы
- Сетка прицела отрисовывается поверх
- Камера отключена по умолчанию для оптимизации

**Параметры:**
- scopeResolution: 1024x1024
- scopeFOV: 20 градусов
- scopeCullingMask: слои видимые в прицеле

**Особенности:**
- Включение/выключение по ПКМ (Input.GetMouseButtonDown(1))
- Плавное переключение FOV
- Автоматическое приближение оружия к глазу

### 3. ShotgunController.cs
Процедурная анимация цевья дробовика:
- Анимирует Transform цевья вдоль оси
- Использует AnimationCurve для гладкого движения
- Синхронизирована с циклом перезарядки

**Параметры:**
- pumpDistance: 0.15 (расстояние хода)
- pumpDuration: 0.5 (время цикла)
- pumpAxis: Vector3.forward (направление движения)

### 4. FirearmManager.cs
Центральный контроллер всей системы:
- Управляет переключением оружия (1, 2)
- Обработка стрельбы (ЛКМ)
- Система перезарядки (R)
- События (OnWeaponChanged, OnFire, OnReload)

**Система боеприпасов:**
- ammoInMagazine: текущие патроны в магазине
- totalAmmo: всего патронов в запасе
- Проверка наличия патронов перед выстрелом

**Особенности:**
- Инициализация всех подкомпонентов в Start()
- Синхронизация между всеми системами
- Дебаг-логирование для отладки

### 5. WeaponAnimator.cs
Визуальные эффекты выстрела:
- Управление вспышкой в дуле (Muzzle Flash)
- Интенсивность Light с использованием AnimationCurve
- Запуск Particle System дыма
- Воспроизведение звуков выстрелов

**Параметры:**
- muzzleFlashDuration: 0.1 сек
- muzzleFlashCurve: EaseInOut для натурального затухания

### 6. PlayerCameraController.cs
Контроллер камеры игрока (НЕЗАВИСИМ от оружия):
- Вращение камеры по мыши
- Ограничение угла обзора вверх/вниз
- Блокировка курсора

**Параметры:**
- mouseSensitivity: 2.0
- maxLookUpAngle: 90 градусов
- maxLookDownAngle: 90 градусов

### 7. PlayerMovementController.cs
Движение игрока:
- WASD для движения
- Shift для спринта
- Ctrl для приседания
- Space для прыжка
- Гравитация и физика

### 8. WeaponUIController.cs
UI-отображение информации об оружии:
- Счётчик патронов (текущее / всего)
- Название оружия
- Индикатор прицеливания
- Видимость сетки прицела

### 9. ReticleGenerator.cs
Процедурная генерация текстур прицела:
- GenerateReticleTexture() — создание перекрестия
- GenerateVignetteTexture() — создание виньетки
- Все в коде, не нужны файлы изображений

### 10. WeaponConfig.cs
ScriptableObject для конфигурации:
- Все параметры оружия в одном месте
- Быстрая настройка через Inspector
- Метод ToWeaponDef() для преобразования

---

## ПОТОК ДАННЫХ

```
PlayerCameraController (камера)
    ↓
    ├─→ PlayerMovementController (движение)
    │
Input (мышь, клавиатура)
    ↓
FirearmManager (главный контроллер)
    ├─→ WeaponController (наведение и отдача)
    │   └─→ Weapon Pivot (трансформация)
    │
    ├─→ ScopeSystem (прицел)
    │   └─→ Scope Camera (RenderTexture)
    │
    ├─→ ShotgunController (анимация цевья)
    │
    ├─→ WeaponAnimator (вспышка, звуки)
    │
    └─→ WeaponUIController (UI)
```

---

## СЛОИ (LAYERS) И CULLING MASK

**Обязательные слои:**

| Слой | Назначение | Main Camera | Scope Camera |
|------|-----------|-------------|--------------|
| Default | Окружение, враги | ✓ | ✓ |
| Weapon | Модель оружия, руки | ✗ (видит Player) | ✗ |
| Enemy | Враги | ✓ | ✓ |
| UI | UI элементы | ✓ | ✗ |
| IgnoreRaycast | Триггеры | ✗ | ✗ |

**Настройка Culling Mask:**
```
Main Camera Culling Mask:
- Default ✓
- Enemy ✓
- UI ✓
- IgnoreRaycast ✓
- Weapon ✗ (ИСКЛЮЧИТЬ)

Scope Camera Culling Mask:
- Default ✓
- Enemy ✓
- IgnoreRaycast ✓
- Weapon ✗
- UI ✗
```

---

## ПРОЦЕСС ВЫСТРЕЛА (EXECUTION FLOW)

```
1. Input.GetMouseButton(0) в FirearmManager.HandleInput()
   ↓
2. Fire() проверяет:
   - fireRate (интервал между выстрелами)
   - ammoInMagazine > 0
   ↓
3. Если проверки пройдены:
   - ammoInMagazine--
   - weaponController.ApplyRecoil()
   - weaponAnimator.PlayFireAnimation()
   - OnFire?.Invoke() (событие для баллистики)
   ↓
4. WeaponController.ApplyRecoil():
   - recoilTimer = recoilDuration
   - Применяется в UpdateRecoil() каждый frame
   ↓
5. WeaponAnimator.PlayFireAnimation():
   - Вспышка в дуле на 0.1 сек
   - Звук выстрела (случайный из массива)
   - Дым из ParticleSystem
   ↓
6. Баллистика (отдельная система):
   - Создание проекций снарядов
   - Рейкасты для обнаружения столкновений
```

---

## РЕЖИМЫ ПРИЦЕЛИВАНИЯ

**Hip Fire (наведение от бедра):**
- Оружие на расстоянии от камеры
- Больший разброс
- Нет прицела

**ADS (Aim Down Sights):**
- Оружие приближается к глазу
- Меньший разброс
- Может быть сетка прицела

**Scope (Sniper Scope):**
- Отдельная Scope Camera
- RenderTexture в линзе
- FOV 20 градусов
- Сетка с метками расстояния

---

## АНИМАЦИОННЫЕ КРИВЫЕ (AnimationCurve)

### Recoil Pitch Curve (вертикальная отдача)
```
Start: (0, 1)   → полная отдача в начале
End:   (1, 0)   → полное восстановление в конце
Type:  EaseInOut → естественное ускорение/замедление
```

### Recoil Yaw Curve (боковая отдача)
```
Start: (0, 1)
End:   (1, 0)
Type:  EaseInOut
Примечание: случайное направление (Random.Range(-1, 1))
```

### Muzzle Flash Curve (вспышка в дуле)
```
Start: (0, 1)   → яркая вспышка
Peak:  (0.3, 1) → держит интенсивность
End:   (1, 0)   → быстрое затухание
```

### Pump Curve (цевьё дробовика)
```
Start: (0, 0)     → вперёд
Peak:  (0.5, 1)   → максимум вперёд
End:   (1, 0)     → возврат
Используется: Mathf.Sin(curveValue * Mathf.PI * 2)
```

---

## ОПТИМИЗАЦИЯ

### 1. Камеры
- Scope Camera отключена по умолчанию (culled из процесса рендера)
- Включается только когда игрок смотрит в прицел
- RenderTexture разрешение 1024x1024 (оптимум качества/производительности)

### 2. Объекты
- Weapon Pivot использует Quaternion.Slerp() вместо Lerp() для плавности
- AnimationCurves кешируются (не создаются каждый frame)
- Частицы дыма жёсткие (hardcoded), не генерируются

### 3. Памяти (GC)
- Vector3 переиспользуются, не создаются в Update
- AnimationCurve.Evaluate() кешируется в переменных
- Нет Instantiate/Destroy в основном цикле

### 4. Рендер
- Culling Mask правильно настроен
- Нет лишних слоёв в рендеринге
- RenderTexture переиспользуется

---

## РАСШИРЕНИЯ И ДОРАБОТКИ

### Phase 1 (базовое):
- ✓ Наведение оружия на курсор
- ✓ Отдача и покачивание
- ✓ Прицел для снайперской винтовки
- ✓ Дробовик с анимацией

### Phase 2 (улучшения):
- [ ] Screen shake при отдаче (встряска камеры)
- [ ] Ветер влияет на баллистику
- [ ] Разные типы патронов (зажигательные, БПЗ)
- [ ] Крепления для оружия (прицелы, компенсаторы)
- [ ] Анимация магазина (визуально вставляется в оружие)

### Phase 3 (продвинутое):
- [ ] Разные позы стрельбы (лёжа, сидя)
- [ ] Влияние стабильности на разброс
- [ ] Система прогрева ствола (перегрев)
- [ ] Разные камеры для разных оружий
- [ ] Симуляция отката в камеру игрока

---

## ЗВУКИ И ЭФФЕКТЫ

### Аудиофайлы
```
Assets/Audio/Weapons/
├── rifle_shot_01.wav
├── rifle_shot_02.wav
├── rifle_shot_03.wav
├── shotgun_shot.wav
├── rifle_reload.wav
└── shotgun_pump.wav
```

### Параметры загрузки
- Load Type: Streaming (для больших файлов)
- Compression: Adaptive ADPCM
- Sample Rate: 44100 Hz

### Particle Systems
```
Muzzle Smoke:
- Duration: 0.15 сек
- Emission Rate: 20 частиц/сек
- Shape: Cone (20°)
- Lifetime: 0.5 сек
- Gravity: -0.5
```

---

## ТЕСТОВЫЙ ЧЕКЛИСТ

```
[ ] Оружие следует за мышью плавно
[ ] Нет рывков при быстрых движениях мыши
[ ] Отдача срабатывает при каждом выстреле
[ ] Отдача плавно восстанавливается
[ ] Покачивание есть в неподвижности
[ ] ПКМ активирует/деактивирует прицел
[ ] В прицеле видна сетка перекрестия
[ ] FOV изменяется плавно при прицеливании
[ ] R перезаряжает оружие
[ ] Звук перезарядки воспроизводится
[ ] 1 переключает на винтовку
[ ] 2 переключает на дробовик
[ ] Патроны уменьшаются при стрельбе
[ ] Перезарядка восполняет магазин
[ ] UI показывает правильное количество патронов
[ ] Дробовик имеет более сильную отдачу
[ ] Цевьё дробовика анимируется при перезарядке
[ ] Камера вращается независимо от оружия
[ ] WASD работает для движения
[ ] Shift работает для спринта
```

---

## ДЕБАГ КОМАНДЫ

Добавить в FirearmManager или консоль:

```csharp
// Бесконечные патроны
if (Input.GetKeyDown(KeyCode.F1))
    totalAmmo = 999;

// Мгновенная перезарядка
if (Input.GetKeyDown(KeyCode.F2))
    isReloading = false;

// Показать параметры оружия
if (Input.GetKeyDown(KeyCode.F3))
    Debug.Log($"Weapon: {currentWeapon.displayName}\n" +
        $"ROF: {fireRate}\nDamage Spread: {currentWeapon.spreadHipDeg}");
```

---

## СОВМЕСТИМОСТЬ С БАЛЛИСТИКОЙ

Система оружия выдаёт события OnFire:

```csharp
firearmManager.OnFire += () => {
    // Баллистика: создать снаряд(ы)
    Vector3 muzzlePos = weaponModel.GetMuzzlePosition();
    Vector3 muzzleDir = weaponModel.GetMuzzleDirection();
    int pelletCount = currentWeapon.pellets;
    
    // Применить разброс
    // Симулировать траекторию
    // Дождаться попадания
};
```

Баллистика работает независимо и получает данные через события.

