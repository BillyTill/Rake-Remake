ИНСТРУКЦИЯ ПО СОЗДАНИЮ SHADER GRAPH: ScopeLensShader
====================================================

## ТИП ШЕЙДЕРА
- **Unlit Shader Graph** (HDRP)
- Причина: линза должна показывать изображение "как есть", без влияния освещения сцены

---

## ПОШАГОВАЯ ИНСТРУКЦИЯ

### ШАГ 1: Создание Shader Graph

1. В Project Window: **Assets/Shaders/** (создать папку если нет)
2. **Right Click → Create → Shader Graph → HDRP → Unlit Shader Graph**
3. Назвать: `ScopeLensShader`

---

### ШАГ 2: Настройка Properties (Properties Node)

Откройте Shader Graph и добавьте следующие свойства:

#### Property 1: _ScopeTexture
- **Name**: `_ScopeTexture`
- **Type**: Texture2D
- **Default**: None (устанавливается из C#)
- **Override Property Declaration**: YES
- **Shader Declaration**: Global

**ВАЖНО**: Global declaration позволяет задавать текстуру через `Shader.SetGlobalTexture`, без создания Material instance на каждый экземпляр оружия.

#### Property 2: _VignetteStrength
- **Name**: `_VignetteStrength`
- **Type**: Float
- **Default**: 1.0
- **Range**: 0.0 - 2.0
- **Tooltip**: Сила затемнения по краям линзы

#### Property 3: _VignettePower
- **Name**: `_VignettePower`
- **Type**: Float
- **Default**: 4.0
- **Range**: 1.0 - 10.0
- **Tooltip**: Резкость виньетки

#### Property 4: _TintColor
- **Name**: `_TintColor`
- **Type**: Color
- **Default**: White (1, 1, 1, 1)
- **HDR**: No
- **Tooltip**: Тонировка стекла линзы

#### Property 5: _ScopeParallaxOffset
- **Name**: `_ScopeParallaxOffset`
- **Type**: Vector2
- **Default**: (0, 0)
- **Override Property Declaration**: YES
- **Shader Declaration**: Global
- **Tooltip**: Смещение UV при параллаксе

---

### ШАГ 3: Основной граф (Main Graph)

#### БЛОК 1: Сэмплинг Render Texture

```
[UV Node] → [Add Node]
              ↑
[Property: _ScopeParallaxOffset]

→ [Sample Texture 2D Node]
   - Texture Property: _ScopeTexture
   - Type: Default
   - Space: Tangent
   
→ [Multiply Node] ×
   ↑
[Property: _TintColor]
```

**Объяснение:**
1. UV координаты меша линзы
2. Добавляем смещение параллакса (от камеры игрока)
3. Сэмплируем Render Texture
4. Умножаем на цвет тонировки

---

#### БЛОК 2: Виньетка (Vignette)

```
[UV Node] → [Subtract Node] - Vector2(0.5, 0.5)
         → [Length Node] (длина вектора)
         → [Multiply Node] × Property: _VignettePower
         → [Power Node] ^ 2
         → [Subtract Node] - 1.0
         → [Negate Node] (умножить на -1)
         → [Saturate Node] (clamp 0-1)
         → [Multiply Node] × Property: _VignetteStrength
         → [One Minus Node] (инверсия)
         
→ [Lerp Node] с Color(0, 0, 0, 1) по этому значению
```

**Объяснение:**
1. Вычисляем расстояние от центра UV (0.5, 0.5)
2. Возводим в степень для резкости
3. Инвертируем (чтобы центр был ярким, края тёмными)
4. Интерполируем между цветом текстуры и чёрным

---

#### БЛОК 3: Финальный вывод

```
[БЛОК 1: Sample Texture 2D] 
         ↓
[Multiply Node] × [БЛОК 2: Виньетка]
         ↓
[Unlit Master Node → Base Color]
```

---

### ШАГ 4: Полный граф (Node Connections)

```
┌─────────────────┐
│ UV Node         │
└────────┬────────┘
         │
         ├──────────────────────────────────────┐
         │                                      │
         ▼                                      ▼
┌─────────────────┐                    ┌────────────────┐
│ Add Node        │ ← Vector2(0,0)     │ Subtract Node  │ ← Vector2(0.5, 0.5)
│ (UV + Parallax) │   [Parallax Prop]  │ (UV - Center)  │
└────────┬────────┘                    └────────┬───────┘
         │                                      │
         ▼                                      ▼
┌─────────────────┐                    ┌────────────────┐
│ Sample Texture  │                    │ Length Node    │
│ 2D (_ScopeTex)  │                    │ (Distance)     │
└────────┬────────┘                    └────────┬───────┘
         │                                      │
         ▼                                      ▼
┌─────────────────┐                    ┌────────────────┐
│ Multiply        │ ← [TintColor Prop] │ Multiply       │ ← [_VignettePower]
│ (× Tint)        │                    │ (× Power)      │
└────────┬────────┘                    └────────┬───────┘
         │                                      │
         │                                      ▼
         │                             ┌────────────────┐
         │                             │ Power Node     │
         │                             │ (^2)           │
         │                             └────────┬───────┘
         │                                      │
         │                                      ▼
         │                             ┌────────────────┐
         │                             │ Subtract       │ ← 1.0
         │                             │ (dist - 1)     │
         │                             └────────┬───────┘
         │                                      │
         │                                      ▼
         │                             ┌────────────────┐
         │                             │ Negate         │
         │                             │ (× -1)         │
         │                             └────────┬───────┘
         │                                      │
         │                                      ▼
         │                             ┌────────────────┐
         │                             │ Saturate       │
         │                             │ (Clamp 0-1)    │
         │                             └────────┬───────┘
         │                                      │
         │                                      ▼
         │                             ┌────────────────┐
         │                             │ Multiply       │ ← [_VignetteStrength]
         │                             │ (× Strength)   │
         │                             └────────┬───────┘
         │                                      │
         │                                      ▼
         │                             ┌────────────────┐
         │                             │ One Minus      │
         │                             │ (Invert)       │
         │                             └────────┬───────┘
         │                                      │
         └──────────────────────────────────────┤
                                                ▼
                                       ┌────────────────┐
                                       │ Multiply       │
                                       │ (Tex × Vignette)│
                                       └────────┬───────┘
                                                │
                                                ▼
                                       ┌────────────────┐
                                       │ Unlit Master   │
                                       │ → Base Color   │
                                       └────────────────┘
```

---

### ШАГ 5: Настройка Output (Unlit Master Node)

В **Unlit Master Node**:
- **Base Color**: подключить результат Multiply (Tex × Vignette)
- **Alpha**: 1.0 (или подключить альфа-канал текстуры для прозрачности)
- **Alpha Clip Threshold**: оставить пустым

---

## СОЗДАНИЕ МАТЕРИАЛА

1. **Assets/Materials/** (создать папку если нет)
2. **Right Click → Create → Material**
3. Назвать: `Mat_ScopeLens`
4. **Shader**: выбрать `Shader Graphs/ScopeLensShader`
5. Настройки:
   - **_ScopeTexture**: оставить None (устанавливается из C#)
   - **_VignetteStrength**: 1.0
   - **_VignettePower**: 4.0
   - **_TintColor**: White (1, 1, 1, 1)

---

## НАЗНАЧЕНИЕ НА МЕШ ЛИНЗЫ

1. Найти в иерархии модели оружия меш линзы/окуляра
2. В **Mesh Renderer** компоненте:
   - **Material**: назначить `Mat_ScopeLens`
   - Убедиться, что меш имеет правильные UV координаты

---

## ПРОВЕРКА

После настройки:
1. Запустите игру
2. Нажмите ПКМ — камера прицела должна включиться
3. На линзе должно появиться изображение из Render Texture
4. По краям линзы должна быть видна виньетка (затемнение)

---

## ОТЛАДКА

Если изображение не видно:
- Проверьте, что `_ScopeTexture` property имеет **Global** declaration
- Убедитесь, что Render Texture назначена через `Shader.SetGlobalTexture`
- Проверьте UV координаты меша линзы (должны быть 0-1 по всей поверхности)
- Проверьте, что камера прицела включена (`scopeCamera.enabled = true`)

Если изображение "пластиковое" без HDR эффектов:
- Убедитесь, что Render Texture имеет формат **ARGBHalf** или **RGBAHalf**
- Проверьте, что HD Additional Camera Data настроен корректно

---

## ОПТИМИЗАЦИЯ

Для слабых машин:
- Уменьшите `_VignettePower` (меньше вычислений)
- Упростите граф (уберите параллакс)
- Используйте меньшее разрешение Render Texture (512x512)
