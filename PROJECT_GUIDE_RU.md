# Инструкция: как устроена игра про остров на Unity

Этот файл объясняет проект с нуля: что в нём есть, как он работает и как самому сделать похожую игру в Unity.

## 1. Что это за проект

Это простая 3D-игра на Unity:

- игрок бегает по острову;
- остров создаётся кодом через Unity Terrain;
- есть вода, камни, пальмы и декор;
- есть кристаллы, которые нужно собрать;
- есть враги, которые патрулируют остров и наносят урон;
- есть здоровье игрока;
- есть мини-карта;
- есть звуки;
- есть маяк-финиш;
- есть победное и проигрышное меню;
- сохраняется лучшее время прохождения.

Главная идея проекта: часть объектов не ставится вручную в Unity, а создаётся автоматически через C# код.

## 2. Ручное создание объектов и создание кодом

Обычно в Unity можно создавать объекты вручную:

1. `GameObject > 3D Object > Cube`
2. поставить объект на сцену;
3. настроить `Transform`;
4. добавить `Material`;
5. добавить скрипт.

В этом проекте то же самое делает код:

```csharp
var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
rock.name = "Rock";
rock.transform.position = position;
rock.transform.localScale = new Vector3(3f, 2f, 3f);
```

Это называется процедурная генерация.

Плюсы:

- можно быстро создавать много объектов;
- можно менять `seed` и получать другой остров;
- можно автоматически расставлять камни, пальмы, кристаллы и врагов;
- удобно для прототипов.

Минусы:

- сложнее понимать сначала;
- сгенерированные объекты могут исчезнуть после `Regenerate Island`;
- не всё удобно редактировать руками.

## 3. Главные файлы проекта

Основные скрипты лежат в `Assets/Scripts`.

### IslandTerrainGenerator.cs

Главный генератор сцены.

Он создаёт:

- terrain-остров;
- воду;
- камни;
- пальмы;
- игрока;
- кристаллы;
- маяк;
- врагов;
- мини-карту через специальные иконки;
- Animator Controller для ходьбы.

Именно этот скрипт висит на объекте:

```text
Island Scene Generator
```

Если выбрать этот объект в Unity, можно нажать:

```text
Regenerate Island
```

Тогда остров пересоздастся.

### IslandPlayerController.cs

Отвечает за управление игроком:

- `WASD` или стрелки — движение;
- `Shift` — бег;
- `Space` — прыжок;
- мышь — вращение камеры;
- камера стоит за игроком.

Движение сделано через `CharacterController`:

```csharp
characterController.Move(movement * Time.deltaTime);
```

### IslandPlayerHealth.cs

Отвечает за здоровье игрока.

У игрока есть здоровье, например:

```text
100 HP
```

Когда враг касается игрока, вызывается:

```csharp
TakeDamage(12);
```

Если здоровье стало `0`, игра вызывает проигрыш.

### IslandEnemy.cs

Отвечает за врагов.

Враг:

- патрулирует вокруг своей точки;
- замечает игрока на расстоянии;
- начинает догонять игрока;
- наносит урон, если подошёл близко.

### IslandGameManager.cs

Главный игровой менеджер.

Он хранит:

- сколько всего кристаллов;
- сколько собрано;
- победил ли игрок;
- проиграл ли игрок;
- время прохождения;
- лучший результат.

Также он рисует HUD через `OnGUI`:

- здоровье;
- количество кристаллов;
- время;
- мини-карту;
- меню победы;
- меню проигрыша.

### IslandCrystal.cs

Скрипт кристалла.

Кристалл:

- вращается;
- немного двигается вверх-вниз;
- исчезает при касании игроком;
- сообщает `GameManager`, что его собрали.

### IslandBeacon.cs

Скрипт золотого маяка.

Маяк:

- светится;
- пульсирует;
- становится финальной целью;
- засчитывает победу, когда все кристаллы собраны.

### IslandMinimapIcon.cs

Скрипт для мини-карты.

Он говорит мини-карте, какой это объект:

```csharp
Player
Crystal
Enemy
Beacon
```

## 4. Как создаётся остров

Остров создаётся через `TerrainData`.

Упрощённая идея:

```csharp
var terrainData = new TerrainData();
terrainData.heightmapResolution = 257;
terrainData.size = new Vector3(240f, 56f, 240f);
```

Потом создаётся массив высот:

```csharp
var heights = new float[resolution, resolution];
```

Каждая точка terrain получает высоту.

Чтобы получился остров, используется расстояние от центра:

```csharp
float radius = Mathf.Sqrt(dx * dx + dz * dz);
float islandShape = Mathf.Clamp01(1f - Mathf.Pow(radius, 2.25f));
```

В центре `islandShape` большой, поэтому земля высокая.

На краях `islandShape` маленький, поэтому земля низкая и уходит под воду.

Дополнительно используется `Mathf.PerlinNoise`, чтобы остров был не идеально круглым и гладким.

## 5. Как создаётся вода

Вода — это обычная большая плоскость:

```csharp
var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
water.transform.position = new Vector3(0f, waterLevel, 0f);
water.transform.localScale = new Vector3(26f, 1f, 26f);
```

Материал воды сделан прозрачным.

Важно: если `waterLevel` слишком высокий, вода может закрыть остров.

## 6. Как создаются камни, пальмы и декор

Камни создаются как сферы:

```csharp
GameObject.CreatePrimitive(PrimitiveType.Sphere);
```

Пальмы создаются из простых примитивов:

- ствол — `Cylinder`;
- листья — `Cube`;
- поворот листьев делается через `Quaternion.Euler`.

Случайная точка на острове выбирается через функцию:

```csharp
RandomIslandPoint(minRadius, maxRadius);
```

Она выбирает случайный угол и радиус, а потом вычисляет высоту земли в этой точке.

## 7. Как создаётся игрок

Игрок создаётся кодом:

```csharp
var player = new GameObject("Player");
player.tag = "Player";
```

На него добавляются компоненты:

```csharp
player.AddComponent<CharacterController>();
player.AddComponent<IslandPlayerHealth>();
player.AddComponent<IslandPlayerController>();
```

Модель игрока собрана из примитивов:

```text
Player
  Body
  Head
  Hair
  Nose
  Left Arm
  Right Arm
  Left Leg
  Right Leg
  Backpack
```

Это не настоящая 3D-модель из Blender, но уже выглядит как простой low-poly персонаж.

## 8. Как работает камера

Камера следует за игроком сзади.

В `IslandPlayerController` есть offset:

```csharp
cameraOffset = new Vector3(0f, 3.1f, -6.5f);
```

Камера ставится так:

```csharp
playerCamera.transform.position = transform.position + cameraRotation * cameraOffset;
```

И смотрит на игрока:

```csharp
Quaternion.LookRotation(target - cameraPosition);
```

## 9. Как работают кристаллы

Генератор создаёт несколько кристаллов на острове.

Каждый кристалл получает:

- материал;
- свет;
- trigger collider;
- скрипт `IslandCrystal`;
- иконку мини-карты.

Когда игрок входит в trigger:

```csharp
IslandGameManager.Instance?.CollectCrystal();
Destroy(gameObject);
```

## 10. Как работают враги

Враг создаётся почти как игрок:

```csharp
var enemy = new GameObject("Island Sentry");
enemy.AddComponent<CharacterController>();
enemy.AddComponent<IslandEnemy>();
```

У врага есть:

- радиус патруля;
- скорость патруля;
- скорость погони;
- дистанция обнаружения;
- дистанция атаки;
- урон.

Если игрок близко, враг начинает идти к нему.

Если враг совсем рядом, он вызывает:

```csharp
player.GetComponent<IslandPlayerHealth>()?.TakeDamage(damage);
```

## 11. Как работает здоровье

Здоровье хранится в `IslandPlayerHealth`.

При получении урона:

```csharp
currentHealth = Mathf.Max(0, currentHealth - amount);
```

Если здоровье стало `0`:

```csharp
IslandGameManager.Instance?.Lose();
```

После этого появляется меню проигрыша.

## 12. Как работает победа

Сначала нужно собрать все кристаллы:

```csharp
HasCollectedAllCrystals
```

Потом нужно добежать до маяка.

Если игрок вошёл в trigger маяка и все кристаллы собраны:

```csharp
IslandGameManager.Instance.Win();
```

Игра показывает победное меню и сохраняет лучший результат.

## 13. Как сохраняется лучший результат

Используется `PlayerPrefs`.

Сохранение:

```csharp
PlayerPrefs.SetFloat("IslandBestTime", time);
PlayerPrefs.Save();
```

Загрузка:

```csharp
PlayerPrefs.GetFloat("IslandBestTime", 0f);
```

Так Unity сохраняет результат между запусками игры.

## 14. Как работает мини-карта

Мини-карта рисуется через `OnGUI`.

Каждый важный объект получает `IslandMinimapIcon`.

Например:

```csharp
player.AddComponent<IslandMinimapIcon>().Configure(IslandMinimapIconType.Player);
```

GameManager находит все иконки:

```csharp
FindObjectsOfType<IslandMinimapIcon>();
```

И рисует точки:

- игрок — белый;
- кристаллы — голубые;
- враги — красные;
- маяк — жёлтый.

## 15. Как работают звуки

Чтобы не добавлять аудио-файлы вручную, звуки создаются кодом.

В `IslandGameManager` есть функция:

```csharp
CreateTone(...)
```

Она создаёт простой `AudioClip` из синусоиды.

Так появились звуки:

- сбор кристалла;
- получение урона;
- победа.

В настоящем проекте лучше использовать реальные `.wav` или `.mp3` файлы.

## 16. Как работает анимация

Сначала руки и ноги двигались вручную через код.

Потом был добавлен `Animator`.

Генератор создаёт:

```text
Assets/Generated/HumanoidWalk.anim
Assets/Generated/Humanoid.controller
```

Анимация двигает pivot-объекты рук и ног.

Когда игрок или враг двигается, скорость `Animator` становится больше.

Когда стоит на месте, `Animator.speed = 0`.

## 17. Как повторить проект с нуля

1. Создай новый Unity 3D project.

2. Создай папки:

```text
Assets/Scenes
Assets/Scripts
```

3. Создай сцену:

```text
Assets/Scenes/Island.unity
```

4. В сцене создай:

```text
Main Camera
Sun / Directional Light
Island Scene Generator
```

5. Создай скрипт `IslandTerrainGenerator`.

6. В нём сначала сделай генерацию terrain.

7. Потом добавь воду.

8. Потом добавь камни и пальмы.

9. Потом добавь игрока с `CharacterController`.

10. Потом добавь управление камерой и движением.

11. Потом добавь кристаллы и GameManager.

12. Потом добавь маяк и условие победы.

13. Потом добавь здоровье и врагов.

14. Потом добавь HUD, мини-карту, звуки и сохранение лучшего времени.

Лучше делать именно постепенно. Если добавить всё сразу, будет сложно искать ошибки.

## 18. Частые ошибки

### Не видно остров

Проверь:

- не слишком ли высокая вода;
- смотрит ли камера на остров;
- сработал ли `Regenerate Island`.

### Safe Mode / compilation errors

Открой Console или `Editor.log` и ищи строки:

```text
error CS...
```

Например, для Terrain нужен модуль:

```json
"com.unity.modules.terrain": "1.0.0"
```

Для Animator нужен модуль:

```json
"com.unity.modules.animation": "1.0.0"
```

### Игрок не появился

Выбери `Island Scene Generator` и нажми:

```text
Regenerate Island
```

### Управление не работает

Кликни в Game view, чтобы захватить мышь.

`Escape` отпускает мышь.

## 19. Что можно улучшить дальше

Следующие хорошие шаги:

- заменить примитивы на настоящие модели из Blender;
- добавить нормальный Canvas UI вместо `OnGUI`;
- добавить музыку и реальные звуки;
- сделать меню старта;
- сделать несколько уровней;
- добавить оружие или способности;
- добавить NavMesh для умных врагов;
- добавить сохранение настроек;
- сделать билд под Windows.

## 20. Главная мысль

Этот проект показывает важную идею Unity:

объекты можно создавать не только руками в редакторе, но и кодом.

Ручной подход удобен для точного дизайна сцены.

Кодовый подход удобен для генерации, случайности и быстрого создания большого количества объектов.

В реальных играх обычно используют оба подхода вместе.
