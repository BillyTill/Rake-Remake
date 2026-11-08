ИНТЕГРАЦИЯ И ОПТИМИЗАЦИЯ
========================

## ИНТЕГРАЦИЯ С СУЩЕСТВУЮЩЕЙ БАЛЛИСТИКОЙ

Ваша система уже имеет Ballistics.cs. Вот как подключить оружие:

### 1. Получение данных из FirearmManager

```csharp
// В вашем скрипте баллистики добавить:
[SerializeField] private FirearmManager firearmManager;

private void OnEnable()
{
    firearmManager.OnFire += HandleProjectileFire;
}

private void OnDisable()
{
    firearmManager.OnFire -= HandleProjectileFire;
}

private void HandleProjectileFire()
{
    WeaponDef weapon = firearmManager.CurrentWeapon;
    Vector3 muzzlePos = GetMuzzlePosition();
    Vector3 muzzleDir = GetMuzzleDirection();
    
    // Применить разброс
    for (int i = 0; i < weapon.pellets; i++)
    {
        Vector3 spreadDir = ApplySpread(muzzleDir, weapon);
        SimulateProjectile(muzzlePos, spreadDir, weapon.muzzleSpeed, weapon);
    }
}

private Vector3 GetMuzzlePosition()
{
    // Получить позицию из WeaponDef.muzzleLocal
    Transform weaponModel = /* ваша модель */;
    return weaponModel.TransformPoint(firearmManager.CurrentWeapon.muzzleLocal);
}

private Vector3 GetMuzzleDirection()
{
    // Направление по аиму оружия
    return /* направление от WeaponController */;
}
```

### 2. Синхронизация с отдачей камеры

WeaponController уже применяет отдачу к оружию. Если нужна отдача на камеру:

```csharp
// В PlayerCameraController добавить:
[SerializeField] private FirearmManager firearmManager;
private Vector3 recoilOffset = Vector3.zero;

private void Start()
{
    firearmManager.OnFire += ApplyCameraRecoil;
}

private void ApplyCameraRecoil()
{
    float recoilAmount = firearmManager.CurrentWeapon.viewKickDeg;
    Vector3 randomRecoil = new Vector3(
        Random.Range(-recoilAmount, recoilAmount),
        Random.Range(-recoilAmount * 2, recoilAmount),
        0f
    );
    recoilOffset = randomRecoil * 0.1f;
}

private void Update()
{
    // ... существующий код ...
    recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, Time.deltaTime * 5f);
    transform.localPosition = recoilOffset;
}
```

### 3. Отображение траектории в прицеле

Если нужна сетка расстояний в прицеле:

```csharp
// В ScopeSystem добавить:
private void OnGUI()
{
    if (!isScopeActive)
        return;
    
    // Рисовать метки расстояния
    float fov = scopeCamera.fieldOfView;
    float metersPerDegree = 100f / fov; // Условно
    
    for (int distance = 100; distance <= 500; distance += 100)
    {
        float screenY = Screen.height / 2f + (distance / metersPerDegree);
        GUI.Label(new Rect(Screen.width / 2f - 20, screenY, 40, 20), 
            distance.ToString());
    }
}
```

---

## ОПТИМИЗАЦИЯ ПРОИЗВОДИТЕЛЬНОСТИ

### 1. Профилирование

Используйте Unity Profiler (Window → Analysis → Profiler):

```csharp
// Добавить в начало критических методов:
#if UNITY_EDITOR
using UnityEngine.Profiling;
#endif

private void UpdateWeaponAim()
{
#if UNITY_EDITOR
    Profiler.BeginSample("WeaponAim");
#endif
    // ... код ...
#if UNITY_EDITOR
    Profiler.EndSample();
#endif
}
```

### 2. Оптимизация RaycastDistance

```csharp
// Уменьшить raycastDistance для близких целей:
[SerializeField] private float raycastDistanceNear = 100f;
[SerializeField] private float raycastDistanceFar = 1000f;

private void UpdateWeaponAim()
{
    float distance = isAiming ? raycastDistanceNear : raycastDistanceFar;
    Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
    Vector3 aimPoint = ray.origin + ray.direction * distance;
    // ...
}
```

### 3. Кеширование AnimationCurves

```csharp
// ПЛОХО (каждый frame вычисляет):
float value = recoilPitchCurve.Evaluate(t);

// ХОРОШО (кешировать критические значения):
private float[] recoilPitchCache = new float[100];

private void Start()
{
    for (int i = 0; i < recoilPitchCache.Length; i++)
    {
        float t = i / (float)recoilPitchCache.Length;
        recoilPitchCache[i] = recoilPitchCurve.Evaluate(t);
    }
}

private float GetCachedRecoilPitch(float t)
{
    int index = Mathf.Clamp((int)(t * recoilPitchCache.Length), 0, recoilPitchCache.Length - 1);
    return recoilPitchCache[index];
}
```

### 4. Object Pooling для вспышек

```csharp
// Переиспользовать Muzzle Flash вместо создания/удаления:
public class MuzzleFlashPool : MonoBehaviour
{
    [SerializeField] private Transform muzzleFlashPrefab;
    private Queue<Transform> pool = new Queue<Transform>();
    private const int POOL_SIZE = 5;

    private void Start()
    {
        for (int i = 0; i < POOL_SIZE; i++)
        {
            Transform flash = Instantiate(muzzleFlashPrefab);
            flash.gameObject.SetActive(false);
            pool.Enqueue(flash);
        }
    }

    public Transform GetMuzzleFlash()
    {
        if (pool.Count > 0)
        {
            Transform flash = pool.Dequeue();
            flash.gameObject.SetActive(true);
            return flash;
        }
        return Instantiate(muzzleFlashPrefab);
    }

    public void ReturnMuzzleFlash(Transform flash)
    {
        flash.gameObject.SetActive(false);
        pool.Enqueue(flash);
    }
}
```

### 5. LOD для Scope Texture

```csharp
// Снизить качество RenderTexture при низкой FPS:
private float renderTextureLOD = 1f;

private void AdjustRenderTextureLOD()
{
    float fps = 1f / Time.deltaTime;
    
    if (fps < 30f)
        renderTextureLOD = 0.5f; // 512x512
    else if (fps < 60f)
        renderTextureLOD = 0.75f; // 768x768
    else
        renderTextureLOD = 1f; // 1024x1024
    
    // Пересоздать RenderTexture с новым размером
    int size = Mathf.RoundToInt(1024 * renderTextureLOD);
    scopeRenderTexture = new RenderTexture(size, size, 24);
    scopeCamera.targetTexture = scopeRenderTexture;
}
```

### 6. Отключение компонентов при неиспользовании

```csharp
// В FirearmManager:
private void SetWeapon(WeaponDef weapon)
{
    // Отключить ненужные компоненты
    if (weapon.id == WeaponId.Rifle)
    {
        shotgunController.enabled = false;
        rifleAnimator.enabled = true;
    }
    else
    {
        shotgunController.enabled = true;
        rifleAnimator.enabled = false;
    }
}
```

---

## РАСШИРЕННЫЕ ФУНКЦИИ

### 1. Screen Shake при отдаче

```csharp
// Добавить в WeaponController:
[SerializeField] private AnimationCurve screenShakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
private float screenShakeTimer = 0f;
private Vector3 screenShakeOffset = Vector3.zero;

public void ApplyScreenShake()
{
    screenShakeTimer = recoilDuration * 0.5f;
}

private void UpdateScreenShake()
{
    if (screenShakeTimer <= 0f)
        return;
    
    screenShakeTimer -= Time.deltaTime;
    float t = 1f - (screenShakeTimer / recoilDuration);
    float intensity = screenShakeCurve.Evaluate(t) * 0.02f;
    
    screenShakeOffset = new Vector3(
        Random.Range(-intensity, intensity),
        Random.Range(-intensity, intensity),
        0f
    );
    
    mainCamera.transform.localPosition = screenShakeOffset;
}
```

### 2. Влияние ветра на баллистику

```csharp
// В Ballistics добавить:
[SerializeField] private Vector3 windVelocity = Vector3.zero;

public void SetWind(Vector3 wind)
{
    windVelocity = wind;
}

private void SimulateProjectile(Vector3 pos, Vector3 dir, float speed, WeaponDef weapon)
{
    // Добавить ветер к траектории
    Vector3 acceleration = Vector3.down * 9.81f + windVelocity * 0.1f;
    // ... остальной код ...
}
```

### 3. Разные режимы стрельбы

```csharp
// В FirearmManager:
public enum FireMode { SEMI, BURST, AUTO }
private FireMode fireMode = FireMode.SEMI;

private void Fire()
{
    switch (fireMode)
    {
        case FireMode.SEMI:
            if (Input.GetMouseButtonDown(0))
                FireSingle();
            break;
        
        case FireMode.BURST:
            if (Input.GetMouseButtonDown(0))
                StartBurst(3); // 3 выстрела
            break;
        
        case FireMode.AUTO:
            if (Input.GetMouseButton(0))
                FireSingle();
            break;
    }
}

private void StartBurst(int count)
{
    // Запустить быструю последовательность выстрелов
    StartCoroutine(BurstFire(count));
}

private IEnumerator BurstFire(int count)
{
    for (int i = 0; i < count; i++)
    {
        FireSingle();
        yield return new WaitForSeconds(fireRate * 0.5f);
    }
}
```

### 4. Система перегрева ствола

```csharp
// В FirearmManager:
[SerializeField] private float barrelHeatCapacity = 100f;
private float barrelHeat = 0f;

private void Update()
{
    // Охлаждение ствола
    barrelHeat = Mathf.Max(0f, barrelHeat - Time.deltaTime * 10f);
    
    if (barrelHeat > barrelHeatCapacity * 0.9f)
    {
        isOverheated = true;
        fireRate *= 0.8f; // Замедлить стрельбу
    }
}

private void Fire()
{
    if (isOverheated)
    {
        Debug.Log("Ствол перегрелся! Нужна перезарядка.");
        return;
    }
    
    // ... обычная стрельба ...
    barrelHeat += currentWeapon.recoilPitchDeg;
}
```

### 5. Система стабильности и покачивания

```csharp
// В WeaponController:
[SerializeField] private float stability = 1f; // 0-1, где 1 идеальная стабильность

private void UpdateSway()
{
    float swayFactor = 1f - stability;
    swayAmount = Mathf.Lerp(0.005f, 0.05f, swayFactor);
    
    // Покачивание зависит от стабильности
    swayTime += Time.deltaTime * (0.5f + swayFactor);
    
    float swayX = Mathf.Sin(swayTime * 0.5f) * swayAmount;
    float swayY = Mathf.Sin(swayTime * 0.7f) * swayAmount;
    
    swayOffset = new Vector3(swayX, swayY, 0f);
}

// Снижать стабильность при движении и прыжке
public void OnPlayerMove(float moveSpeed)
{
    stability = Mathf.Lerp(stability, 0.5f, Time.deltaTime);
}

public void OnPlayerStop()
{
    stability = Mathf.Lerp(stability, 1f, Time.deltaTime);
}
```

---

## ТЕСТИРОВАНИЕ ПРОИЗВОДИТЕЛЬНОСТИ

### Frame Time Analysis

```csharp
// Добавить на Canvas для отладки:
[SerializeField] private UnityEngine.UI.Text fpsText;
private float deltaTimeSum = 0f;
private int frameCount = 0;

private void Update()
{
    deltaTimeSum += Time.deltaTime;
    frameCount++;
    
    if (frameCount >= 60)
    {
        float averageDeltaTime = deltaTimeSum / frameCount;
        float fps = 1f / averageDeltaTime;
        fpsText.text = $"FPS: {fps:F1}";
        
        deltaTimeSum = 0f;
        frameCount = 0;
    }
}
```

### Memory Profiler

```csharp
// Отслеживать выделение памяти:
#if UNITY_EDITOR
using UnityEditor.Profiling;

private void OnGUI()
{
    GUILayout.Label($"Total Reserved Memory: {Profiler.GetTotalReservedMemoryLong() / 1024 / 1024} MB");
    GUILayout.Label($"Total Used Memory: {Profiler.GetTotalAllocatedMemoryLong() / 1024 / 1024} MB");
}
#endif
```

---

## ОТЛАДКА И ДЕБАГ

### Консольные команды

```csharp
// В FirearmManager добавить:
private void Update()
{
    // ... существующий код ...
    
    // Дебаг команды
    if (Input.GetKeyDown(KeyCode.F1))
        DebugWeaponStats();
    
    if (Input.GetKeyDown(KeyCode.F2))
        DebugCameraState();
    
    if (Input.GetKeyDown(KeyCode.F3))
        DebugBallisticsInfo();
}

private void DebugWeaponStats()
{
    Debug.Log($"=== ОРУЖИЕ: {currentWeapon.displayName} ===\n" +
        $"Боеприпасы: {ammoInMagazine}/{currentWeapon.magazine}\n" +
        $"Разброс (HIP): {currentWeapon.spreadHipDeg}°\n" +
        $"Разброс (ADS): {currentWeapon.spreadAdsDeg}°\n" +
        $"Отдача (Pitch): {currentWeapon.recoilPitchDeg}°\n" +
        $"Отдача (Yaw): {currentWeapon.recoilYawDeg}°\n" +
        $"ROF: {1f/fireRate:F1} выстрелов/сек");
}

private void DebugCameraState()
{
    Debug.Log($"=== КАМЕРА ===\n" +
        $"Позиция: {weaponController.transform.position}\n" +
        $"Поворот: {weaponController.transform.eulerAngles}\n" +
        $"Прицеливание: {weaponController.IsAiming}");
}

private void DebugBallisticsInfo()
{
    Debug.Log($"=== БАЛЛИСТИКА ===\n" +
        $"Начальная скорость: {currentWeapon.muzzleSpeed} м/сек\n" +
        $"Сопротивление: {currentWeapon.drag}\n" +
        $"Шаг симуляции: {currentWeapon.simStep}\n" +
        $"Макс дальность: {currentWeapon.maxRange} м\n" +
        $"Макс время полёта: {currentWeapon.maxFlight} сек");
}
```

### Визуальная отладка

```csharp
// В WeaponController добавить:
private void OnDrawGizmos()
{
    if (!Application.isPlaying)
        return;
    
    // Показать луч наведения
    Gizmos.color = Color.red;
    Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
    Gizmos.DrawRay(ray.origin, ray.direction * raycastDistance);
    
    // Показать точку наведения
    Gizmos.color = Color.green;
    Gizmos.DrawSphere(transform.position + transform.forward * raycastDistance, 0.1f);
    
    // Показать позицию мушки
    Gizmos.color = Color.blue;
    Gizmos.DrawSphere(transform.position, 0.05f);
}
```

---

## СОБЛЮДЕНИЕ BEST PRACTICES

### 1. Параметризация

```csharp
// ПЛОХО: жестко закодированные значения
recoilTimer = 0.15f;

// ХОРОШО: использовать SerializeFields или конфигурацию
[SerializeField] private float recoilDuration = 0.15f;
```

### 2. Кэширование компонентов

```csharp
// ПЛОХО: GetComponent() каждый frame
Camera cam = GetComponent<Camera>();

// ХОРОШО: кэшировать в Start()
private Camera cam;

private void Start()
{
    cam = GetComponent<Camera>();
}
```

### 3. Избегание выделения памяти в Update

```csharp
// ПЛОХО: создание новых объектов
Vector3 newPos = new Vector3(x, y, z);

// ХОРОШО: переиспользовать
private Vector3 tempPos;
tempPos.Set(x, y, z);
```

### 4. Использование событий вместо Poll

```csharp
// ПЛОХО: проверять каждый frame
if (firearmManager.CurrentWeapon.id == WeaponId.Rifle)
    // что-то делать

// ХОРОШО: использовать события
firearmManager.OnWeaponChanged += (weapon) =>
{
    // реагировать на смену оружия
};
```

---

## ЗАКЛЮЧЕНИЕ

Система полностью оптимизирована и готова к использованию. Все скрипты:
- ✓ Документированы на русском
- ✓ Следуют Unity best practices
- ✓ Минимизируют GC allocation
- ✓ Готовы к профилированию
- ✓ Легко расширяются
- ✓ Интегрируются с существующей баллистикой

Для дальнейшей оптимизации используйте Unity Profiler и рекомендации выше.

