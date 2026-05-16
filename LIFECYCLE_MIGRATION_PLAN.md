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
Если речь о runtime prefab/assets, сверяем всю цепочку:
state/factory -> path/provider -> asset load -> instantiation.
Совпадение только по `IInstantiator.InstantiatePrefabForComponent(...)`
не считается достаточным reference match.
```

## Progress Tracker

Статусы:

- `[done]` - реализовано в коде.
- `[deferred]` - проверено, но код не добавляется до появления реальной нужды.
- `[next]` - следующий рабочий slice или проверка.
- `[todo]` - запланировано, но еще не начато.
- `[blocked]` - требует отдельного решения или проверки.

Порядок:

- Идем по шагам сверху вниз.
- `[next]` должен указывать на самый ранний нерешенный implementation step.
- Validation не становится `[next]`, пока более ранние implementation steps не
  `[done]` или явно `[deferred]` после аудита на своем месте.

Текущее состояние:

- `[done]` Step 1 - state machine update model.
  `GameStateMachine` стал `ITickable`, добавлен `IUpdateable`, `GameplayState`
  стал active update-state с пустым `Update()`.
- `[done]` Step 2 - gameplay loading/enter/active states.
  Добавлен `GameplayEnterState`, `LoadGameplayState` переходит по цепочке
  `LoadGameplayState -> GameplayEnterState -> GameplayState`,
  `GameplayEnterState` зарегистрирован в `ProjectInstaller`.
- `[done]` Step 3 - gameplay setup in `GameplayEnterState`.
  Добавлен минимальный пример по аналогии с `BattleEnterState.PlaceHero()`:
  `GameplayEnterState` берет `ILevelStartPointProvider.StartPoint`, вызывает
  `IExampleUnitFactory.Create(...)`, затем входит в `GameplayState`.
  `ExampleUnit` asset flow приведен к reference-паттерну.
- `[done]` Step 3a - rework `ExampleUnit` prefab loading to reference flow.
  Пример приведен к цепочке `Resources path -> IAssetProvider ->
  IInstantiator`, по аналогии с `HeroFactory.AddViewPath("Gameplay/Hero/hero")`,
  `AssetProvider.LoadAsset<T>(path)` и `EntityViewFactory`.
  `ProjectInstaller` больше не хранит serialized gameplay prefab field.
- `[done]` Step 4 - concrete provider for scene reference.
  Добавлены `ILevelStartPointProvider`, `LevelStartPointProvider` и
  `GameplaySceneInitializer`. Добавлен `SceneInitializationInstaller`, который
  bind-ит scene initializers как interfaces по примеру `ecs-survivors`. В
  `Gameplay` scene добавлен root `GameplayStartPoint`, который передает позицию
  в provider. `GameplaySceneInitializer` также передает `MainCamera` в
  `CameraProvider`, как `LevelInitializer` в `ecs-survivors`.
- `[done]` Step 5 - UI boundary.
  UI дергает state machine, прямых вызовов `SceneLoader.LoadScene(...)` из UI
  нет. Из `MainMenu` удалена лишняя зависимость от `SceneLoader`.
- `[done]` Step 6 - active state cleanup.
  `GameplayState.ExitOnEndOfFrame()` вызывает `IExampleUnitFactory.Cleanup()`,
  а factory уничтожает созданные ей `ExampleUnit` objects. Это минимальный
  аналог active-state cleanup из `BattleLoopState`.
- `[done]` Step 7 - end-of-frame exit minimal implementation.
  Добавлен `EndOfFrameExitState`, `IDeferredExitState` и ожидание deferred exit
  в `SimpleStateMachine` через `UniTask.WaitUntil(...)`. `GameplayState`
  наследует `EndOfFrameExitState` и чистит state-owned objects в
  `ExitOnEndOfFrame()`.
- `[done]` Target schema - menu loading split.
  Добавлен `LoadMainMenuState`, `MainMenuState` стал active-only state,
  `BootstrapState` и return-to-menu UI входят в `LoadMainMenuState`.
- `[done]` Target schema - state registration.
  Существующие `GameplayPauseState` и `GameOverOrParagonState` зарегистрированы
  в `ProjectInstaller`.
- `[done]` Target schema - `StateFactory`.
  Добавлены `IStateFactory` и `StateFactory` по примеру
  `Infrastructure/States/Factory` из `ecs-survivors`. `SimpleStateMachine`
  больше не хранит manual dictionary/register states, а получает state через
  `IStateFactory.GetState<TState>()`.
- `[done]` Target schema - simple common services.
  Добавлены `ITimeService`/`UnityTimeService`, `IRandomService`/
  `UnityRandomService`, `ICameraProvider`/`CameraProvider`. `IPhysicsService`/
  `PhysicsService` добавлены без ECS-зависимостей: вместо `GameEntity` и
  `ICollisionRegistry` используются Unity-neutral `Collider2D`/`RaycastHit2D`.
- `[done]` Unity validation after Step 2.
  Проект запускается в Unity Editor.
- `[done]` Unity validation after `LoadMainMenuState`.
  Запуск через `Bootstrap -> LoadMainMenuState -> MainMenuState`, старт
  gameplay и возврат в меню проверены в Unity Editor.
- `[blocked]` Unity validation after example gameplay setup.
  Проверить `Bootstrap -> LoadMainMenuState -> MainMenuState ->
  LoadGameplayState -> GameplayEnterState -> GameplayState`, создание
  `ExampleUnit` на `GameplayStartPoint` и возврат в меню. Требует Play Mode в
  Unity Editor. После последнего импорта свежий `Editor.log` не показывает
  compile/runtime errors, но появление `ExampleUnit` в сцене еще нужно
  подтвердить в Play Mode.

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

### 4a. StateFactory резолвит states из DI

В `ecs-survivors` `GameStateMachine` не хранит manual registry состояний.
Смена state выглядит так:

```text
GameStateMachine.ChangeState<TState>()
-> stateFactory.GetState<TState>()
-> DiContainer.Resolve<TState>()
```

Для нашего template:

```text
ProjectInstaller
-> BindInterfacesAndSelfTo<StateFactory>().AsSingle()
-> BindInterfacesAndSelfTo<SomeState>().AsSingle()

SimpleStateMachine.ChangeCurrentState<TState>()
-> stateFactory.GetState<TState>()
```

`GameRunner` только входит в `BootstrapState`. Он не регистрирует states вручную.

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

GameplayState.ExitOnEndOfFrame()
-> остановить/почистить активную игру
```

Минимальная реализация уже добавлена через `EndOfFrameExitState`: state machine
запрашивает deferred exit, текущий active state завершает текущий update, затем
вызывает `ExitOnEndOfFrame()` и только после этого входит в следующий state.

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

`MainMenuState` больше не является loading+active state. Загрузка меню вынесена
в `LoadMainMenuState`, а `MainMenuState` остается active menu mode.

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
GameStateMachine asks StateFactory for concrete states.
StateFactory resolves states from Zenject.
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

### Шаг 3. Добавить подготовку gameplay в GameplayEnterState

Референс из `ecs-survivors`:

```text
BattleEnterState.Enter()
-> PlaceHero()
-> heroFactory.CreateHero(levelDataProvider.StartPoint)
-> stateMachine.Enter<BattleLoopState>()
```

Для нашего template:

```text
GameplayEnterState.Enter()
-> подготовить gameplay через concrete providers/factories
-> stateMachine.Enter<GameplayState>()
```

Если подготовка станет сложнее, расширяем этот же паттерн внутри
`GameplayEnterState` через concrete providers/factories. Не добавляем нового
владельца lifecycle заранее.

Текущий template-пример:

```text
GameplaySceneInitializer
-> LevelStartPointProvider.SetStartPoint(GameplayStartPoint.position)

SceneInitializationInstaller
-> binds GameplaySceneInitializer as IInitializable

GameplayEnterState.Enter()
-> exampleUnitFactory.Create(levelStartPointProvider.StartPoint)
-> ExampleUnitFactory follows the reference-backed asset chain:
   Resources path "Gameplay/Units/ExampleUnit"
   -> IAssetProvider.LoadAsset<ExampleUnit>(path)
   -> IInstantiator.InstantiatePrefabForComponent(...)
-> stateMachine.Enter<GameplayState>()
```

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

- `GameplayState.ExitOnEndOfFrame()` чистит подписки и state-owned процессы.
- `MainMenuState.Exit()` чистит menu-owned процессы, если они появятся.
- `GameOverOrParagonState` отвечает за завершение и результат.

Текущий example cleanup:

```text
GameplayEnterState.Enter()
-> exampleUnitFactory.Create(...)

GameplayState.ExitOnEndOfFrame()
-> exampleUnitFactory.Cleanup()
-> destroy created ExampleUnit objects
```

### Шаг 7. Добавить минимальный end-of-frame exit

В `ecs-survivors` для долгоживущих states есть отложенный выход в конце кадра.

Для нашего template минимальный вариант:

```text
stateMachine.Enter<NextState>()
-> current EndOfFrameExitState.BeginExit()
-> current state finishes current Update()
-> current EndOfFrameExitState.EndExit()
-> current state ExitOnEndOfFrame()
-> nextState.Enter()
```

## Проверочный Список

Перед тем как считать перенос успешным:

- UI не вызывает `SceneLoader`.
- UI может вызывать `GameStateMachine.Enter`.
- State machine берет states через `StateFactory`, а не через manual register.
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
