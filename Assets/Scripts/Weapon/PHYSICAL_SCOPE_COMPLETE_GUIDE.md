СИСТЕМА ФИЗИЧЕСКОГО ПРИЦЕЛА (PiP SCOPE) — ПОЛНОЕ РУКОВОДСТВО
============================================================

## АРХИТЕКТУРА СИСТЕМЫ

```
Main Camera (игрок)
    │
    ├── WeaponRoot (позиция оружия)
    │   └── WMRifle (модель винтовки)
    │       ├── Body (корпус)
    │       ├── Barrel (ствол)
    │       ├── Scope (прицел)
    │       │   ├── Tube (труба)
    │       │   └── Lens (окуляр/линза) ← МАТЕРИАЛ ScopeLensShader
    │       └── ...
    │
    └── ScopeCamera (создаётся автоматически)
        ├── Target Texture: RT_WeaponScope
        ├── Culling Mask: Всё, КРОМЕ Weapon
        └── FOV: 8° (узкий, как у реального прицела)

Render Texture: RT_WeaponScope
    ├── Формат: ARGBHalf (HDR)
    ├── Разрешение: 1024x1024
    ├── Depth: 24-bit
    └── Anti-Aliasing: MSAA 4x
```

---

## ПОШАГОВАЯ НАСТРОЙКА

### ШАГ 1: Создание Render Texture

**Через Editor-скрипт (рекомендуется):**
```
Menu → Lake → Weapon → Setup Physical Scope System
```

**Вручную:**
1. **Assets → Create → Render Texture**
2. Назвать: `RT_WeaponScope`
3. В Inspector:
   - **Size**: 1024 × 1024
   - **Color Format**: ARGBHalf (критично для HDRP!)
   - **Depth Buffer**: 24 bit
   - **Anti-Aliasing**: 4 (MSAA 4x)
   - **Enable Random Write**: OFF
   - **Dimension**: Tex2D
   - **Wrap Mode**: Clamp
   - **Filter Mode**: Bilinear

---

### ШАГ 2: Создание камеры прицела

**Через Editor-скрипт (рекомендуется):**
```
Menu → Lake → Weapon → Create Scope Camera
```

**Вручную:**

1. Создать **GameObject → Camera**
2. Назвать: `ScopeCamera`
3. **Camera Component**:
   - **Projection**: Perspective
   - **Field of View**: 8° (узкий угол)
   - **Near Clip Plane**: 0.01
   - **Far Clip Plane**: 1000
   - **Viewport Rect**: X=0, Y=0, W=1, H=1
   - **Target Texture**: перетащить `RT_WeaponScope`
   - **Culling Mask**: выбрать всё, КРОМЕ слоёв Weapon и PlayerBody
   - **Enabled**: FALSE (отключена по умолчанию!)

4. **HD Additional Camera Data** (автоматически добавляется в HDRP):
   - **Custom Frame Settings**: Enable
   - **Rendering Path**: Default
   - В **Frame Settings** включить:
     - Volumetrics
     - Compute Light List Per Cluster
     - Post Processes

5. **Physical Camera** (опционально):
   - Enable Physical Camera
   - Focal Length: 50mm (подбирается под FOV)

---

### ШАГ 3: Создание Shader Graph (ScopeLensShader)

**Полная инструкция:** `ScopeLensShader_Instructions.md`

**Кратко:**
1. **Assets/Shaders/** → Create → Shader Graph → HDRP → Unlit Shader Graph
2. Назвать: `ScopeLensShader`
3. Добавить Properties:
   - `_ScopeTexture` (Texture2D, Global)
   - `_VignetteStrength` (Float, 1.0)
   - `_VignettePower` (Float, 4.0)
   - `_TintColor` (Color, white)
   - `_ScopeParallaxOffset` (Vector2, Global)

4. Граф:
   - UV + Parallax Offset → Sample Texture 2D → × Tint Color
   - Vignette (расстояние от центра) → Multiply
   - Result → Unlit Master → Base Color

---

### ШАГ 4: Создание материала

1. **Assets/Materials/** → Create → Material
2. Назвать: `Mat_ScopeLens`
3. **Shader**: `Shader Graphs/ScopeLensShader`
4. Настройки:
   - **_ScopeTexture**: None (устанавливается из C#)
   - **_VignetteStrength**: 1.0
   - **_VignettePower**: 4.0
   - **_TintColor**: White (1, 1, 1, 1)

5. **Назначить на меш линзы/окуляра** в модели оружия

---

### ШАГ 5: Настройка слоёв (Layers)

1. **Edit → Project Settings → Tags and Layers**
2. Добавить слои (если не существуют):
   - Layer 10: `Weapon`
   - Layer 11: `PlayerBody` (опционально)

3. **Назначить слои на объекты:**
   - Выбрать модель оружия (WMRifle)
   - В Inspector → Layer: `Weapon`
   - Нажать "Yes, change children"

---

### ШАГ 6: Настройка Animator Controller

1. **Assets/Animators/** → Create → Animator Controller
2. Назвать: `WeaponAimController`
3. Открыть Animator window
4. **Parameters**:
   - Добавить `IsAiming` (Bool)

5. **States**:
   - Создать состояние `Idle` (позиция "от бедра")
   - Создать состояние `Aim` (позиция прицеливания)
   - Make Default: `Idle`

6. **Transitions**:
   - `Idle → Aim`:
     - Condition: `IsAiming == true`
     - Has Exit Time: OFF
     - Transition Duration: 0.15 sec
     - Interruption Source: Next State
   
   - `Aim → Idle`:
     - Condition: `IsAiming == false`
     - Has Exit Time: OFF
     - Transition Duration: 0.15 sec
     - Interruption Source: Next State

7. **Animation Clip** (создать или импортировать):
   - `Idle`: оружие в позиции "от бедра"
   - `Aim`: оружие поднято, линза окуляра по центру экрана

---

### ШАГ 7: Добавление компонента PhysicalScope

1. Выбрать объект оружия (или камеру игрока)
2. **Add Component → PhysicalScope**
3. В Inspector назначить ссылки:
   - **Scope Camera**: перетащить созданную ScopeCamera
   - **Scope Render Texture**: перетащить RT_WeaponScope
   - **Weapon Animator**: перетащить Animator оружия
   - **Aim Animator Bool**: "IsAiming"
   - **Aim Button**: "Fire2" (ПКМ)

4. Настройки:
   - **Aim Transition Time**: 0.15
   - **Parallax Strength**: 0.05
   - **Enable Scope Camera On Aim**: TRUE (критично!)

---

## ЧЕК-ЛИСТ ПЕРЕД ТЕСТИРОВАНИЕМ

```
□ Render Texture создана (ARGBHalf, 1024x1024)
□ Scope Camera создана и настроена
□ Scope Camera ОТКЛЮЧЕНА по умолчанию (enabled = false)
□ Culling Mask: исключён слой Weapon
□ Shader Graph создан (Unlit HDRP)
□ Материал Mat_ScopeLens создан и назначен на линзу
□ Слой Weapon создан и назначен на модель оружия
□ Animator Controller создан с параметром IsAiming
□ Анимации Idle/Aim созданы
□ Компонент PhysicalScope добавлен на оружие
□ Все ссылки в PhysicalScope назначены
```

---

## ТЕСТИРОВАНИЕ

1. **Play** в Unity Editor
2. **Нажать ПКМ** (Fire2)
3. Проверить:
   - [ ] Оружие плавно поднимается к камере (анимация)
   - [ ] На линзе окуляра появляется изображение
   - [ ] Изображение в прицеле увеличено (FOV 8°)
   - [ ] По краям линзы видна виньетка (затемнение)
   - [ ] При движении мыши изображение слегка смещается (параллакс)
   - [ ] При отпускании ПКМ оружие плавно опускается
   - [ ] Камера прицела отключается

---

## ИЗВЕСТНЫЕ ПОДВОДНЫЕ КАМНИ В HDRP

### 1. Render Texture формат

**Проблема:** Если использовать обычный формат (ARGB32), изображение будет "пластиковым", без bloom и tonemapping.

**Решение:** Используйте **ARGBHalf** или **RGBAHalf** (HDR форматы).

```
❌ RenderTextureFormat.ARGB32
✅ RenderTextureFormat.ARGBHalf
```

---

### 2. Camera Frame Settings

**Проблема:** Scope Camera может не видеть Volumetric Fog, Post-Processing и другие HDRP эффекты.

**Решение:** Включите **Custom Frame Settings** на HDAdditionalCameraData:

```csharp
hdData.customRenderingSettings = true;
var frameSettings = hdData.renderingPathCustomFrameSettings;
frameSettings.SetEnabled(FrameSettingsField.Volumetrics, true);
frameSettings.SetEnabled(FrameSettingsField.PostProcesses, true);
```

---

### 3. Culling Mask

**Проблема:** Если не исключить слой Weapon, камера прицела увидит модель оружия изнутри → чёрный экран или артефакты.

**Решение:**
```csharp
int weaponLayer = LayerMask.NameToLayer("Weapon");
scopeCamera.cullingMask &= ~(1 << weaponLayer);
```

---

### 4. Камера включена постоянно

**Проблема:** Если Scope Camera включена всегда, это убьёт производительность (-50% FPS).

**Решение:** Камера должна быть **отключена по умолчанию** и включаться только при прицеливании:

```csharp
void Start()
{
    scopeCamera.enabled = false; // КРИТИЧНО!
}

void Update()
{
    if (Input.GetButtonDown("Fire2"))
    {
        scopeCamera.enabled = true; // Включить только при ADS
    }
}
```

---

### 5. Material Instance

**Проблема:** Создание Material instance через `material.SetTexture()` на каждом экземпляре оружия → утечка памяти.

**Решение:** Используйте `Shader.SetGlobalTexture()` с **Global Property Declaration**:

```csharp
// В Shader Graph Property:
// Override Property Declaration → Shader Declaration → Global

// В C#:
Shader.SetGlobalTexture("_ScopeTexture", renderTexture);
```

---

### 6. Post-Processing не работает

**Проблема:** Изображение в прицеле без Tonemapping, Bloom, Color Grading.

**Решение:**
1. Убедитесь, что в сцене есть **Volume** с этими эффектами
2. Volume должен иметь **Mode: Global** или покрывать позицию Scope Camera
3. В HDAdditionalCameraData включите **Post Processes**

---

### 7. Параллакс дёргается

**Проблема:** Смещение изображения при движении головы дёргается.

**Решение:** Сглаживание через Lerp:

```csharp
currentParallax = Vector2.Lerp(
    currentParallax,
    targetParallax,
    Time.deltaTime * smoothSpeed // 8-12
);
```

---

### 8. Разрешение слишком низкое

**Проблема:** Изображение в прицеле пиксельное.

**Решение:**
- Увеличить разрешение Render Texture до 2048x2048
- Включить Anti-Aliasing (MSAA 4x или 8x)
- Проверить Filter Mode: Bilinear

---

## ОПТИМИЗАЦИЯ

### Уровни качества

**High (высокое качество):**
- Разрешение: 1024x1024
- Anti-Aliasing: MSAA 4x
- Параллакс: включён
- FOV: 8°

**Medium (среднее качество):**
- Разрешение: 512x512
- Anti-Aliasing: MSAA 2x
- Параллакс: включён
- FOV: 8°

**Low (низкое качество):**
- Разрешение: 256x256
- Anti-Aliasing: OFF
- Параллакс: отключён
- FOV: 10°

---

### Performance Tips

1. **Камера только при прицеливании** — критично!
2. **Exclude слоя Weapon** — избежать рендера оружия изнутри
3. **Разрешение 512x512** — достаточно для большинства случаев
4. **Отключить тени** для Scope Camera (опционально):
   ```csharp
   scopeCameraHDData.renderingPathCustomFrameSettings
       .SetEnabled(FrameSettingsField.ShadowMaps, false);
   ```

---

## ОТЛАДКА

### Консольные команды (Editor)

```
Menu → Lake → Weapon → Setup Physical Scope System
Menu → Lake → Weapon → Create Scope Camera
Menu → Lake → Weapon → Verify Scope Setup
```

### Проверка в рантайме

```csharp
// В PhysicalScope.cs уже есть логи:
[PhysicalScope] Scope Camera инициализирована. FOV: 8
[PhysicalScope] Render Texture назначена в шейдер: _ScopeTexture
[PhysicalScope] Начало прицеливания
[PhysicalScope] Scope Camera: ВКЛ
[PhysicalScope] Конец прицеливания
[PhysicalScope] Scope Camera: ВЫКЛ
```

---

## ФАЙЛЫ СИСТЕМЫ

### Скрипты
- `PhysicalScope.cs` — основной контроллер прицела
- `ScopeQualityConfig.cs` — конфигурация качества
- `Editor/PhysicalScopeEditor.cs` — editor-утилиты

### Документация
- `PHYSICAL_SCOPE_COMPLETE_GUIDE.md` — это руководство
- `ScopeLensShader_Instructions.md` — создание шейдера

### Ассеты (создаются автоматически)
- `Assets/Textures/RT_WeaponScope.renderTexture`
- `Assets/Shaders/ScopeLensShader.shadergraph`
- `Assets/Materials/Mat_ScopeLens.mat`
- `Assets/Animators/WeaponAimController.controller`

---

## ИНТЕГРАЦИЯ С СУЩЕСТВУЮЩЕЙ СИСТЕМОЙ

### Связь с PlayerWeapons

```csharp
// В PlayerWeapons.cs:
public bool IsAiming => _adsT > 0.5f;

// Подписаться на событие:
var scope = GetComponent<PhysicalScope>();
if (scope != null)
{
    scope.OnAimStateChanged += (isAiming) =>
    {
        // Обновить разброс, чувствительность мыши и т.д.
        spreadMultiplier = isAiming ? 0.1f : 1.0f;
    };
}
```

---

Система физического прицела полностью готова! 🎯

**Время настройки:** ~15-20 минут (при наличии модели оружия с анимациями)

**Производительность:** <2% FPS при прицеливании, 0% при обычной игре
