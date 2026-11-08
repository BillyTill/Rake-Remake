ФИЗИЧЕСКИЙ ПРИЦЕЛ С RENDERTEXTURE
==================================

## АРХИТЕКТУРА

Система добавляет физический прицел к существующему PlayerWeapons:

```
Player (NetworkPlayer)
├── Main Camera
│   ├── WeaponRoot (создаётся PlayerWeapons)
│   │   ├── WMRifle (винтовка)
│   │   │   ├── Cylinder (труба оптики)
│   │   │   ├── Cylinder (окуляр — ЛИНЗА, получает RenderTexture)
│   │   │   └── ...
│   │   └── WMShotgun (дробовик)
│   │
│   └── ScopeCamera (создаётся ScopePhysicsIntegration)
│       └── Рендерит в RenderTexture
│
└── ScopePhysicsIntegration (новый компонент)
    ├── Управляет RenderTexture
    ├── Приближает оружие при ADS
    └── Синхронизирует камеру прицела
```

## УСТАНОВКА

### ШАГ 1: Добавить компонент на игрока

1. Открыть сцену с игроком
2. Выбрать Player GameObject (тот, что имеет NetworkPlayer)
3. В Inspector нажать "Add Component"
4. Найти и добавить: **ScopePhysicsIntegration**

### ШАГ 2: Создать материал для линзы

1. Создать новый Material:
   - Assets → Create → Material
   - Назвать: "Mat_ScopeLens"
   - Shader: HDRP/Lit (или Standard)

2. В Inspector этого материала:
   - Base Color (или Albedo): чёрный (0, 0, 0)
   - Metallic: 0.8
   - Smoothness: 0.9
   - НЕ назначать текстуру вручную — система сделает это автоматически

### ШАГ 3: Назначить материал в ScopePhysicsIntegration

1. Выбрать Player (у которого добавили компонент)
2. В компоненте ScopePhysicsIntegration:
   - Lens Material: перетащить Mat_ScopeLens
   - Texture Resolution: 1024 (или 2048 для качества)
   - ADS Approach Distance: 0.08 (на 8 см приблизить оружие)
   - ADS Approach Speed: 8 (скорость приближения)

### ШАГ 4: Проверить Layer'ы

1. Edit → Project Settings → Tags and Layers
2. Убедиться, что слой "Weapon" существует (Layer 10)
3. Установить слой для моделей оружия:
   - Выбрать любую модель оружия в сцене
   - В Inspector установить Layer: Weapon

## КАК ЭТО РАБОТАЕТ

### При нажатии ПКМ (ADS):

1. **PlayerWeapons** увеличивает `_adsT` от 0 до 1
2. **ScopePhysicsIntegration**:
   - Приближает WeaponRoot вперёд (оружие движется к камере)
   - Активирует ScopeCamera когда _adsT > 0.6
3. **ScopeCamera**:
   - Рендерит мир в RenderTexture (1024x1024)
   - Исключает слой Weapon (не видим саму модель оружия)
4. **RenderTexture**:
   - Применяется на материал окуляра оптики
   - Игрок видит сквозь прицел реальное изображение мира

### Визуально:

```
ПКМ не нажата:
- Оружие на расстоянии от камеры (0.34m)
- Оптический оверлей в центре экрана (из PlayerWeapons.OnGUI)

ПКМ нажата (_adsT > 0.6):
- Оружие приближается к камере (+0.08m)
- ScopeCamera активирована
- Камера оружия смотрит туда же, куда PlayerLook.AimRotation
- На окуляре оптики видно изображение из ScopeCamera
```

## ПАРАМЕТРЫ НАСТРОЙКИ

| Параметр | Значение | Описание |
|----------|----------|---------|
| textureResolution | 1024 | Качество изображения в прицеле. 2048 = ещё чётче, но медленнее |
| adsApproachDistance | 0.08 | На сколько метров приблизить оружие к камере |
| adsApproachSpeed | 8 | Плавность приближения (выше = быстрее) |

## TROUBLESHOOTING

### Проблема: Прицел чёрный/не видно изображения

**Решение:**
1. Проверить, что Material назначена: ScopePhysicsIntegration → Lens Material
2. Убедиться, что Material имеет шейдер HDRP/Lit
3. В Inspector Material увеличить Metallic (0.8-1.0) и Smoothness (0.8-1.0)

### Проблема: Видно саму модель оружия в прицеле

**Решение:**
1. Установить слой "Weapon" для всех моделей оружия
2. Проверить, что слой существует в Project Settings → Layers
3. Перезагрузить сцену

### Проблема: Система работает у себя, но не видна у других

**Решение:**
1. ScopePhysicsIntegration создаёт локальную камеру и RenderTexture
2. Это **локальный** визуальный эффект — видно только владельцу
3. Для других PlayerWeapons работает с оптическим оверлеем (из OnGUI)
4. Это **правильное** поведение (как в CS:GO)

### Проблема: Лаги/снижение FPS при прицеливании

**Решение:**
1. Снизить textureResolution с 1024 до 512
2. Убедиться, что ScopeCamera не видит слишком много объектов
3. Отключить тени для ScopeCamera: 
   - Выбрать ScopeCamera в иерархии
   - Shadow Casting Mode → Off

## ИНТЕГРАЦИЯ С СЕТЬЮ (Mirror)

**Важно:** ScopePhysicsIntegration работает только для локального игрока!

### Локальный игрок (владелец):
- ✅ Видит физический прицел с RenderTexture
- ✅ Оружие приближается к камере
- ✅ Смотрит сквозь оптику

### Удалённые игроки (остальные):
- ✅ Видят оптический оверлей (OnGUI из PlayerWeapons)
- ✅ Видят позицию оружия с инерцией
- ✅ Видят отдачу и анимацию
- ❌ НЕ видят внутренность прицела (это вычислительно дорого)

Это нормально и соответствует архитектуре игры.

## РАСШИРЕНИЯ

### Добавить виньетку (чёрные края в прицеле):

Отредактировать материал Mat_ScopeLens:
1. Добавить текстуру виньетки (чёрный градиент по краям)
2. Материал автоматически её покажет при RenderTexture

### Добавить метки расстояния:

В ScopePhysicsIntegration можно добавить OnGUI для рисования рисок:
```csharp
private void OnGUI()
{
    if (!scopeCamera.enabled)
        return;
    
    // Рисовать риски в центре экрана
    GUI.Label(new Rect(Screen.width/2-20, Screen.height/2+100, 40, 20), "100м");
    GUI.Label(new Rect(Screen.width/2-20, Screen.height/2+150, 40, 20), "200м");
}
```

### Зависимость FOV от оптики:

В PlayerWeapons уже есть зум через `camera.fieldOfView / weapon.adsZoom`.
ScopePhysicsIntegration автоматически синхронизирует это с ScopeCamera.

## ОТЛАДКА

Добавить в ScopePhysicsIntegration.LateUpdate():

```csharp
if (Input.GetKeyDown(KeyCode.F1))
{
    Debug.Log($"ADS: {GetAdsT():F2}, Camera Active: {scopeCamera.enabled}");
    Debug.Log($"Weapon Pos: {weaponRoot.localPosition}");
}
```

Нажимать F1 для проверки состояния.

## ПРИМЕЧАНИЯ

- **LocalPlayer only**: ScopePhysicsIntegration работает только если `isLocalPlayer == true`
- **Материал**: Создавается 1 RenderTexture на каждого локального игрока (2 в splitscreen)
- **Производительность**: RenderTexture 1024x1024 = ~2-5% от бюджета GPU (зависит от сложности сцены)
- **Мультиплеер**: Каждый клиент создаёт свою ScopeCamera локально

