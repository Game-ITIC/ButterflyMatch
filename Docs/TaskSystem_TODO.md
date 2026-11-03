# TODO: Интеграция Task-системы с геймплеем

## Описание
Система задач (`TaskService`, `TaskSO`, `TasksListSO`, `TaskPresenter`, `TaskPanel`, `TaskView`) полностью настроена и готова. Для того чтобы задачи засчитывались непосредственно во время игры на Match-3 уровнях, необходимо подключить события геймплейного цикла к вызовам `TaskService.TrackProgress(...)` (или `QuestEvents`).

---

## Задачи на интеграцию (Gameplay -> Tasks)

### 1. Прохождение уровней (`QuestType.CompleteLevels`, `QuestType.PerfectLevels`)
- **Где**: Окно победы или место завершения уровня в `LevelManager` (`JuiceFresh/Scripts/GUI/AnimationManager.cs` / `LevelManager.cs`).
- **Действие**: При успешном завершении уровня вызывать:
  ```csharp
  taskService.TrackProgress(QuestType.CompleteLevels, amount: 1);
  // Если уровень пройден на 3 звезды:
  taskService.TrackProgress(QuestType.PerfectLevels, amount: 1);
  ```

### 2. Сбор фишек/предметов на поле (`QuestType.CollectItems`)
- **Где**: Уничтожение/сбор фишек на поле (`Item.DestroyItem()` / сбор ингредиентов в `LevelManager`).
- **Действие**: При сборе фишек передавать ID предмета (например, `"butterfly"`, `"yellow"`, `"flower"`) и количество:
  ```csharp
  taskService.TrackProgress(QuestType.CollectItems, targetId: itemColorOrName, amount: 1);
  ```

### 3. Использование бустеров (`QuestType.UseItems`)
- **Где**: Активация бустеров в игровом процессе (`JuiceFresh/Scripts/GUI/Boosters/`).
- **Действие**: При использовании бустера вызывать:
  ```csharp
  taskService.TrackProgress(QuestType.UseItems, targetId: boosterId, amount: 1);
  ```

---

## Статус системы
- [x] Шаблоны задач (`TaskSO`) с компактными диапазонами `Vector2Int`.
- [x] Конфиг списка задач (`TasksListSO`) с таймерами сброса.
- [x] Модель данных (`TaskModel`) и хранилище в `PlayerPrefs` (`TaskStorage`).
- [x] Автоматическая генерация задач (`TaskGenerator`).
- [x] Сервис управления и начисления наград (`TaskService`).
- [x] Отображение и биндинг (`TaskPresenter`, `TaskPanel`, `TaskView`).
- [x] Подключение геймплейных триггеров из Match-3 движка (2026-11-03: `GameInitializer.OnLevelWon/OnLevelLost`, счётчики `Models/LevelSessionStats`).
