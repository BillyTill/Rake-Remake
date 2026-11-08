ФИНАЛЬНАЯ НАСТРОЙКА ФИЗИЧЕСКОГО ПРИЦЕЛА
=========================================

## ПРОБЛЕМА

Сейчас при ПКМ:
- ❌ Экран становится чёрным
- ❌ Видно только крест (OnGUI оверлей)
- ❌ Нет физического прицела на винтовке
- ❌ Нет RenderTexture на линзе

Нужно:
- ✅ Оружие приближается к камере
- ✅ Видно изображение **через линзу оптики** (RenderTexture)
- ✅ Перекрестие на фоне реального мира
- ✅ Зум через FOV

---

## РЕШЕНИЕ

### ШАГ 1: Отключить OnGUI оверлей

В файле **PlayerWeapons.cs** найти метод `OnGUI()` (строка ~594):

```csharp
private void OnGUI()
{
    if (!_isLocal || _current == null)
        return;

    // ЗАКОММЕНТИРОВАТЬ или УДАЛИТЬ эту часть:
    // DrawScopeOverlay();
    
    // ... остальной код UI
}
```

**Или** полностью заменить `DrawScopeOverlay()` на пустую функцию:

```csharp
private void DrawScopeOverlay()
{
    // Отключено: используем физический прицел (ScopeRenderer)
    return;
}
```

### ШАГ 2: Создать материал для линзы

1. **Assets → Create → Material**
2. Назвать: `Mat_ScopeLens`
3. **Shader:** HDRP/Lit (или Standard)
4. **Параметры:**
   - Base Color (Albedo): чёрный RGB(0, 0, 0)
   - Metallic: 0.95
   - Smoothness: 0.99
   - Emission: включить
   - Emission Color: RGB(0.1, 0.2, 0.3)
   - Emission Intensity: 0.5

### ШАГ 3: Добавить ScopeRenderer в Player

1. Открыть **Player prefab** (Assets/Prefabs/Player.prefab)
2. **Add Component → ScopeRenderer**
3. В инспекторе ScopeRenderer:
   - **Lens Material:** перетащить Mat_ScopeLens
   - Texture Resolution: 1024
   - ADS Approach Distance: 0.10
   - ADS Approach Speed: 6

### ШАГ 4: Убедиться в слоях

1. **Edit → Project Settings → Tags and Layers**
2. Убедиться, что слой **10: Weapon** существует
3. **Все модели оружия должны быть на слое Weapon:**
   - В сцене найти Player → Main Camera → WeaponRoot → WMRifle
   - Select все (WMRifle и все дочерние объекты)
   - В Inspector установить Layer: **Weapon**
   - Нажать "Yes, change children"

### ШАГ 5: Проверить материал окуляра

Винтовка строится процедурно в **WeaponModelBuilder.cs** (строка 54-56):

```csharp
Cylinder(root, new Vector3(0f, 0.058f, 0.012f), 0.021f, 0.17f, _darkMetal);  // Труба
Cylinder(root, new Vector3(0f, 0.058f, 0.105f), 0.028f, 0.032f, _scopeGlass); // Окуляр
Cylinder(root, new Vector3(0f, 0.058f, -0.078f), 0.026f, 0.028f, _scopeGlass); // Объектив
```

**Окуляр** (вторая строка) — это то, на что нужно применить RenderTexture.

Если окуляр использует `_scopeGlass` материал, его нужно заменить или добавить логику назначения lensMaterial.

---

## ПРАВИЛЬНАЯ АРХИТЕКТУРА

```
Player
├── Main Camera (mainCamera)
│   └── WeaponRoot (weaponRoot)
│       └── WMRifle (модель винтовки)
│           ├── Box (ствольная коробка)
│           ├── Cylinder (ствол)
│           ├── Cylinder (мушка)
│           ├── Cylinder (труба оптики)
│           ├── Cylinder (окуляр) ← НА НЕЙ ДОЛЖНА БЫТЬ RenderTexture
│           ├── Cylinder (объектив)
│           └── ...
│
├── ScopeRenderer (новый компонент)
│   ├── lensMaterial: Mat_ScopeLens
│   └── Creates ScopeCamera (отдельная камера)
│
└── ScopeCamera (автоматически создаётся ScopeRenderer)
    └── Рендерит в RenderTexture
```

---

## ЧТО ПРОИСХОДИТ ПРИ ПКМ

1. **PlayerWeapons._adsT увеличивается** от 0 до 1
2. **ScopeRenderer:**
   - Приближает WeaponRoot вперёд (оружие движется к камере)
   - При _adsT > 0.7 активирует ScopeCamera
3. **ScopeCamera:**
   - Рендерит мир в RenderTexture (1024x1024)
   - Исключает слой Weapon (не видим саму модель оружия)
4. **RenderTexture:**
   - Автоматически применяется на Mat_ScopeLens
   - Окуляр оптики теперь показывает RenderTexture
5. **Main Camera FOV:**
   - Уменьшается через `camera.fieldOfView / weapon.adsZoom`
   - Это уже сделано в PlayerWeapons (строка 284)

**Результат:** Игрок смотрит через физический прицел, видит мир через линзу, с увеличением.

---

## ТЕСТИРОВАНИЕ

1. Play в Unity Editor
2. **Без ПКМ:**
   - Винтовка должна быть видна в кадре
   - Видно оптику с синей линзой
3. **При ПКМ (прицеливание):**
   - Оружие приближается к камере
   - На линзе появляется изображение мира (RenderTexture)
   - Перекрестие из OnGUI должно быть видно поверх
   - Всё приближено (зум через FOV)

---

## ЕСЛИ НЕ РАБОТАЕТ

| Проблема | Проверить |
|----------|-----------|
| Прицел всё ещё чёрный | Отключен ли OnGUI оверлей? Назначена ли lensMaterial? |
| RenderTexture не видна | Mat_ScopeLens применена на окуляр? Слой Weapon установлен? |
| Чёрный экран вместо RenderTexture | ScopeCamera не активирована. Проверить условие `adsT > 0.7` |
| Видно саму модель оружия в прицеле | Установить слой Weapon для WMRifle и всех дочерних |
| Лаги при прицеливании | Снизить textureResolution с 1024 до 512 |

---

## ФИНАЛЬНЫЙ ЧЕКЛИСТ

```
[ ] OnGUI оверлей отключен (DrawScopeOverlay() закомментирована)
[ ] Material Mat_ScopeLens создан с параметрами выше
[ ] ScopeRenderer добавлен на Player prefab
[ ] Lens Material назначена в ScopeRenderer
[ ] Слой Weapon создан
[ ] Все модели оружия на слое Weapon
[ ] Play в редакторе
[ ] ПКМ активирует прицеливание
[ ] Видно RenderTexture на линзе
[ ] Перекрестие видно
```

Когда всё готово — система готова к релизу! 🎮

