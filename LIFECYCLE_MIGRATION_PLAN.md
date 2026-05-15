# Lifecycle Migration Plan

Цель: переложить в `UnityTemplate` lifecycle-подход из `ecs-survivors`, не
перенося ECS, generated code, `Feature`-слой и прочие детали реализации.

## Reference-Locked Правило

Этот документ locked на `ecs-survivors` как lifecycle-референс.

Перед изменением плана нужно сначала найти соответствующий класс/паттерн в
`ecs-survivors` и перенести именно его смысл, без дополнительных прослоек.

Если паттерна нет в `ecs-survivors`, он не должен попадать в этот документ как
рекомендация. Его можно вынести только как отдельное предложение и сначала
явно обсудить.

```text
Берем только то, что реально есть в ecs-survivors.
Не добавляем новые архитектурные прослойки заранее.
Если в ecs-survivors UI дергает state machine напрямую, в плане пишем так же.
Если в ecs-survivors scene references идут через конкретные initializers/providers,
в плане пишем так же.
```

## Что Именно Берем Из ecs-survivors

### 1. UI не грузит сцены, но может дергать state machine

В `ecs-survivors` UI-компоненты сами подписываются на кнопки и напрямую вызывают
`IGameStateMachine`.

Пример flow:

```text
HomeHUD.StartBattleButton
-> HomeHUD.EnterBattleLoadingState()
-> stateMachine.Enter<LoadingBattleState, string>(BattleSceneName)
-> LoadingBattleState loads scene
```

То есть правило не такое:

```text
UI -> additional application layer -> StateMachine
```

А такое:

```text
UI -> StateMachine -> State -> SceneLoader
```

Главная граница: UI не вызывает `SceneLoader.LoadScene(...)` напрямую.

### 2. Loading state владеет загрузкой сцены

В `ecs-survivors` scene loading находится в отдельном state.

Паттерн:

```text
LoadingBattleState.Enter(sceneName)
-> sceneLoader.LoadScene(sceneName, callback)
-> callback enters BattleEnterState
```

Для нашего template:

```text
LoadGameplayState.Enter()
-> loadingCurtain.Show()
-> sceneLoader.LoadScene(SceneEnum.Gameplay)
-> loadingCurtain.Hide()
-> stateMachine.Enter<GameplayEnterState>()
```

### 3. Enter state готовит режим

В `ecs-survivors` после загрузки battle scene отдельный state делает подготовку
перед активным loop.

Паттерн:

```text
BattleEnterState.Enter()
-> create hero at LevelDataProvider.StartPoint
-> stateMachine.Enter<BattleLoopState>()
```

Для нашего template:

```text
GameplayEnterState.Enter()
-> подготовить игровую сессию обычными сервисами/фабриками
-> stateMachine.Enter<GameplayState>()
```

Не вводим отдельного промежуточного владельца gameplay lifecycle. Подготовку
делает сам state.

### 4. Active state владеет update-loop

В `ecs-survivors` `GameStateMachine` является `ITickable` и тикает только
активный state, если он реализует update-интерфейс.

Паттерн:

```text
GameStateMachine.Tick()
-> if activeState is IUpdateable
-> activeState.Update()
```

Для нашего template:

```text
GameStateMachine : ITickable
-> ticks current state

GameplayState : IState, IGameState, IUpdateable
-> Enter()
-> Update()
-> Exit()
```

Не переносим ECS/Features. Внутри `GameplayState.Update()` вызываем обычные
сервисы нашего проекта, если они нужны.

### 5. Active state владеет cleanup

В `ecs-survivors` долгоживущие states не просто запускаются, а еще явно чистят
режим при выходе.

Паттерн:

```text
ActiveState.Enter()
-> start mode

ActiveState.Update()
-> tick mode

ActiveState.ExitOnEndOfFrame()
-> cleanup mode
```

Для нашего template:

```text
GameplayState.Enter()
-> включить активную игру

GameplayState.Update()
-> тик активной игры

GameplayState.Exit()
-> остановить/почистить активную игру
```

На первом шаге можно оставить обычный `Exit()`. Отложенный выход в конце кадра
можно добавить отдельно, когда появятся реальные гонки между update и переходом.

### 6. Scene references идут через конкретные initializers/providers

В `ecs-survivors` нет общего контейнера scene references.

Реальный паттерн:

```text
LevelInitializer
-> reads StartPoint and MainCamera from scene
-> writes StartPoint into LevelDataProvider
-> writes MainCamera into CameraProvider

UIInitializer
-> reads UIRoot from scene
-> writes UIRoot into WindowFactory

BattleEnterState
-> reads LevelDataProvider.StartPoint
```

Для нашего template:

```text
GameplaySceneInitializer
-> fills concrete providers/services

GameplayEnterState
-> uses concrete providers/services
```

Не вводим общий контейнер scene references заранее.

## Целевая Схема Для UnityTemplate

```text
BootstrapState
-> LoadMainMenuState
-> MainMenuState
-> LoadGameplayState
-> GameplayEnterState
-> GameplayState
-> GameplayPauseState
-> GameOverOrParagonState
```

Минимальная версия может временно оставить `MainMenuState` как loading+active
state, но для соответствия референсу лучше разделить loading state и active
state там, где режим становится долгоживущим.

## Responsibilities

### BootstrapState

- Инициализирует глобальные сервисы.
- Переходит в загрузку меню.
- Не занимается gameplay.

### LoadMainMenuState

- Показывает curtain.
- Загружает `MainMenu`.
- Скрывает curtain.
- Входит в `MainMenuState`.

### MainMenuState

- Представляет активный menu mode.
- Может быть пустым на старте.
- UI меню может напрямую вызвать `stateMachine.Enter<LoadGameplayState>()`.
- UI меню не должен вызывать `SceneLoader` напрямую.

### LoadGameplayState

- Показывает curtain.
- Загружает `Gameplay`.
- Дожидается завершения загрузки сцены.
- Скрывает curtain.
- Входит в `GameplayEnterState`.

### GameplayEnterState

- Использует конкретные providers/services, заполненные scene initializers.
- Создает/готовит игровую сессию.
- Входит в `GameplayState`.

### GameplayState

- Владеет активным игровым циклом.
- Реализует update-интерфейс по аналогии с `IUpdateable` в `ecs-survivors`.
- В `Update()` вызывает нужные gameplay services.
- При game over / pause / restart / return to menu переводит state machine в
  следующий state.
- В `Exit()` чистит то, чем владеет state.

### GameplayPauseState

- Останавливает активную игру или переводит сервисы в pause-состояние.
- Resume возвращает в `GameplayState`.
- Main menu переводит в `LoadMainMenuState`.
- Restart переводит в `LoadGameplayState`.

### GameOverOrParagonState

- Останавливает активную игру.
- Показывает результат.
- Restart переводит в `LoadGameplayState`.
- Main menu переводит в `LoadMainMenuState`.

## UI Правило

Как в `ecs-survivors`:

```text
UI -> GameStateMachine.Enter(...)
UI -> not SceneLoader.LoadScene(...)
```

Допустимо:

```csharp
private void StartGame()
{
    _stateMachine.Enter<LoadGameplayState>();
}
```

Недопустимо:

```csharp
private async void StartGame()
{
    await _sceneLoader.LoadScene(SceneEnum.Gameplay);
}
```

Смысл: UI выбирает следующий state, но загрузкой сцены и cleanup занимается
сам state. Дополнительную прослойку между UI и `GameStateMachine` не вводим.

## DI Rule

Как в `ecs-survivors`:

```text
DI creates states and services.
States receive dependencies through DI.
Scene initializers write scene references into concrete providers.
States consume concrete providers.
```

Не добавляем новые прослойки заранее. Если позже появится реальная боль, будем
обсуждать ее отдельно и сверять с задачей, а не добавлять абстракции по инерции.

## Пошаговый План

### Шаг 1. Привести state machine к update-модели

Сделать `GameStateMachine` `ITickable`.

Добавить интерфейс по аналогии с `ecs-survivors`:

```csharp
public interface IUpdateable
{
    void Update();
}
```

В `GameStateMachine.Tick()`:

```text
if currentState is IUpdateable updateable
-> updateable.Update()
```

### Шаг 2. Добавить loading/enter/active states для gameplay

Добавить:

- `LoadGameplayState`;
- `GameplayEnterState`;
- `GameplayState`.

`LoadGameplayState` только грузит сцену.

`GameplayEnterState` готовит сессию.

`GameplayState` тикает активную игру.

### Шаг 3. Перенести подготовку gameplay в GameplayEnterState

Если появятся scene services по типу `GameplayBootstrap`, они не должны сами
стартовать игру через `IInitializable`.

То, что в `IgnisBearer` делает `GameplayBootstrap.CreateGame()`, в template
должно вызываться из `GameplayEnterState`, обычными инжектнутыми сервисами.

### Шаг 4. Добавить concrete providers для scene references

По примеру `LevelDataProvider`, `CameraProvider`, `WindowFactory.SetUIRoot`.

Пример:

```text
GameplaySceneInitializer
-> LevelDataProvider.SetStartPoint(...)
-> CameraProvider.SetMainCamera(...)
-> UiRootProvider.SetRoot(...)
```

States используют эти providers напрямую.

### Шаг 5. UI оставляем простым

UI MonoBehaviour может быть подписан на кнопки сам.

Он может дергать:

```text
stateMachine.Enter<...>()
windowService.Open(...)
windowService.Close(...)
```

Он не должен дергать:

```text
sceneLoader.LoadScene(...)
sceneLoader.ReloadScene()
```

### Шаг 6. Добавить cleanup в active states

Минимум:

- `GameplayState.Exit()` чистит подписки и state-owned процессы.
- `MainMenuState.Exit()` чистит menu-owned процессы, если они появятся.
- `GameOverOrParagonState` отвечает за завершение и результат.

### Шаг 7. Позже добавить end-of-frame exit, если понадобится

В `ecs-survivors` для долгоживущих states есть отложенный выход в конце кадра.

Пока не тащим это автоматически. Сначала делаем простую модель:

```text
Enter
Update
Exit
```

Если появятся баги из-за перехода посреди update-кадра, тогда переносим паттерн
`EndOfFrameExitState`.

## Проверочный Список

Перед тем как считать перенос успешным:

- UI не вызывает `SceneLoader`.
- UI может вызывать `GameStateMachine.Enter`.
- Загрузкой сцен владеют loading states.
- Подготовкой gameplay владеет `GameplayEnterState`.
- Активным tick владеет `GameplayState`.
- Cleanup находится в state exit.
- Scene references передаются через concrete initializers/providers.
- Нет новых абстракций, которых нет в референсном lifecycle-паттерне.

## Главное Правило

```text
Scene is data and references.
DI wires objects.
UI requests state transitions.
State owns lifecycle.
```
