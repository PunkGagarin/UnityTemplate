# Lifecycle Migration Plan

Status: historical migration/audit log. `AGENTS.md` is the source of truth for
current architecture rules. Do not treat this file as an active task plan unless
the user explicitly reopens lifecycle migration planning.

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
- `[done]` Example feature - enemy spawner.
  Добавлены `EnemyUnit`, `IEnemyFactory`/`EnemyFactory` и
  `IEnemySpawner`/`EnemySpawner`. `EnemyFactory` использует reference-backed
  asset chain `Resources path "Gameplay/Enemies/EnemyUnit" -> IAssetProvider ->
  IInstantiator`. Lifecycle mapping взят из `BattleLoopState.OnUpdate()` ->
  `BattleFeature.Execute()` -> `EnemySpawnSystem.Execute()`, но без ECS/Feature
  слоя. `GameplayState` запускает spawner с player transform, тикает его в
  `Update()` и чистит через `ExitOnEndOfFrame()`. Spawn interval вынесен в
  value config `EnemySpawnerConfig`, который биндится через
  `GlobalConfigInstaller`.
- `[done]` UI reference audit.
  UI lifecycle в `ecs-survivors` разобран по классам `UIInitializer`,
  `WindowService`, `WindowFactory`, `BaseWindow`, `WindowsConfig`, `HomeHUD`,
  `GameOverWindow`, `LevelUpWindow`, `AbilityCard`,
  `OpenLevelUpWindowSystem`, `UpgradeAbilityOnRequestSystem`, `ShopWindow`,
  `ShopItem`, `BuyItemOnRequestSystem`, `HomeScreenState` и `GameOverState`.
- `[done]` UI Step 1 - add window infrastructure.
  Добавлены `WindowId`, `BaseWindow`, `IWindowService`, `WindowService`,
  `IWindowFactory`, `WindowFactory`, `WindowsConfig` и `WindowConfig`.
  `WindowFactory` берет prefab из `Resources/Configs/Windows/windowConfig` и
  создает окно через Zenject `IInstantiator`, без дополнительных flow слоев.
- `[done]` UI Step 2 - add scene UI root initialization.
  Добавлен `UIInitializer`; `MainMenu` и `Gameplay` сцены регистрируют его
  через `SceneInitializationInstaller`, а текущий scene `Canvas` используется
  как `UIRoot`.
- `[done]` UI Step 3 - migrate settings to window flow.
  `MainMenu` больше не инжектит `SettingsView`; кнопка settings вызывает
  `WindowService.Open(WindowId.SettingsWindow)`. `SettingsView` стал
  `BaseWindow`, создается через `WindowFactory` и закрывается через
  `WindowService.Close(...)`.
- `[done]` UI Step 4 - migrate gameplay menu modal to window flow.
  `ExampleGameplayMenu` удален из `Gameplay` scene как preplaced prefab.
  Scene HUD `GearButton` открывает `WindowId.GameplayMenuWindow`; `Restart`
  стал `BaseWindow`, закрывает себя через `WindowService.Close(...)` и затем
  вызывает state transition. Это не result/game-over window.
- `[done]` UI Step 5 - keep scene HUDs simple.
  `MainMenu` остается scene HUD аналогом `HomeHUD`: `Start` вызывает
  `GameStateMachine.Enter<LoadGameplayState>()`, `Settings` вызывает
  `WindowService.Open(WindowId.SettingsWindow)`, direct `SceneLoader` нет.
- `[done]` UI Step 6 - document MVP direct service-call rule.
  Request transport pattern из `ecs-survivors` зафиксирован как deferred option.
  Текущий UnityTemplate rule: gameplay/meta choice UI в MVP может напрямую
  вызывать high-level feature/domain services; один сервис инкапсулирует effect.
- `[done]` UI Step 7 - cleanup and static validation.
  Статически проверено: direct `SceneLoader` остался в loading states,
  `SettingsView` и `Restart` наследуют `BaseWindow`, `WindowId` entries есть в
  `Resources/Configs/Windows/windowConfig`, `UIInitializer` зарегистрирован в
  `MainMenu` и `Gameplay` сценах.
- `[done]` Config rule - value configs vs prefab registries.
  Value configs с gameplay/settings значениями назначаются в installers и
  инжектятся. Prefab registries, которые мапят id/path на prefab для dynamic
  creation, могут оставаться resource-backed. `WindowsConfig` относится к
  prefab registry, как в `ecs-survivors`.
- `[done]` Config example - enemy spawner interval.
  Добавлен `EnemySpawnerConfig` с `SpawnIntervalSeconds = 3`. Config asset
  лежит в `Assets/_Project/Data/Config`, назначается в `GlobalConfigInstaller`
  и инжектится в `EnemySpawner`.
- `[done]` Gameplay menu pause.
  `GearButton` включает `PauseService`, затем открывает
  `GameplayMenuWindow`. `GameplayState` не тикает gameplay services пока
  `PauseService.IsPaused`; `Restart` снимает паузу перед close, restart или
  main menu. `LoadGameplayState` и `LoadMainMenuState` также снимают паузу как
  safety reset.
- `[blocked]` Unity validation after UI window migration.
  Требует Play Mode в Unity Editor: проверить `MainMenu -> Settings -> Apply`,
  `MainMenu -> Settings -> Cancel`, `MainMenu -> Gameplay`, и в gameplay
  `GearButton -> GameplayMenuWindow -> Close/Restart/MainMenu`.
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
- Не тикает gameplay services, если `PauseService.IsPaused`.
- При game over / pause / restart / return to menu переводит state machine в
  следующий state.
- В `Exit()` чистит то, чем владеет state.

### GameplayPauseState

- Зарегистрирован как будущий lifecycle state.
- Сейчас gear-menu pause не входит в `GameplayPauseState`, потому обычный
  transition из `GameplayState` вызывает `ExitOnEndOfFrame()` и cleanup
  runtime objects.
- Перед использованием для menu pause нужно добавить явную suspend/resume
  семантику, отличную от cleanup exit.

### GameOverOrParagonState

- Останавливает активную игру.
- Показывает результат.
- Restart переводит в `LoadGameplayState`.
- Main menu переводит в `LoadMainMenuState`.

## UI Правило

Текущий выбор для `UnityTemplate`: MVP direct service calls.

```text
UI -> GameStateMachine.Enter(...)
UI -> WindowService.Open/Close(...)
UI -> query UI-facing services for display data
UI -> call high-level feature/domain services for gameplay/meta choices
UI -> not SceneLoader.LoadScene(...)
UI -> not own gameplay service lifecycle or cleanup
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
сам state. Для gameplay/meta choice меню в текущем template UI/Presenter может
напрямую вызывать high-level service method, если этот service сам
инкапсулирует проверку и применение effect.
Дополнительную прослойку между UI и `GameStateMachine` не вводим.

## UI Lifecycle Reference

В `ecs-survivors` UI lifecycle состоит из нескольких разных потоков:

```text
Scene HUD
-> scene object already exists
-> inject GameStateMachine and WindowService
-> button click calls GameStateMachine.Enter(...) or WindowService.Open(...)
```

```text
Modal window
-> WindowService.Open(WindowId)
-> WindowFactory.CreateWindow(WindowId)
-> WindowsConfig resource registry gives prefab
-> Zenject IInstantiator creates BaseWindow under scene UIRoot
-> window buttons call WindowService.Close(...) and/or GameStateMachine.Enter(...)
```

```text
Gameplay choice window
-> gameplay/meta system opens window through WindowService
-> window reads options/state from UI-facing services/static data
-> player selects an option
-> window writes a concrete request into a context-owned request transport
-> gameplay/meta execute loop reads that transport
-> processor system calls domain service and consumes/destroys request
```

### Reference classes

- `Infrastructure/Installers/UIInitializer.cs`
  Scene initializer with `RectTransform UIRoot`. On initialize it calls
  `IWindowFactory.SetUIRoot(UIRoot)`.
- `Gameplay/Windows/WindowService.cs`
  Stores opened `BaseWindow` instances, opens through factory, closes by
  destroying the opened window GameObject.
- `Gameplay/Windows/WindowFactory.cs`
  Stores current `UIRoot`, gets prefab by `WindowId` from resource-backed
  `WindowsConfig`, instantiates prefab with Zenject `IInstantiator` under
  `UIRoot`.
- `Gameplay/Windows/BaseWindow.cs`
  Window MonoBehaviour lifecycle: `Awake -> OnAwake`, `Start -> Initialize +
  SubscribeUpdates`, `OnDestroy -> Cleanup -> UnsubscribeUpdates`.
- `Gameplay/Windows/Configs/WindowsConfig.cs`
  ScriptableObject list of `WindowConfig { WindowId, GameObject Prefab }`.
- `Meta/UI/HUD/HomeHUD.cs`
  Scene HUD. Button calls `stateMachine.Enter<LoadingBattleState, string>(...)`;
  another button calls `windowService.Open(WindowId.ShopWindow)`.
- `Gameplay/GameOver/UI/GameOverWindow.cs`
  Dynamic window. Button closes itself through `WindowService`, then calls
  `stateMachine.Enter<LoadingHomeScreenState>()`.
- `Infrastructure/States/GameStates/GameOverState.cs`
  State opens `GameOverWindow` through `IWindowService.Open(...)`.
- `Gameplay/Features/LevelUp/Systems/OpenLevelUpWindowSystem.cs`
  Gameplay system opens `LevelUpWindow` when level-up happens.
- `Gameplay/Features/LevelUp/Windows/LevelUpWindow.cs`
  Dynamic gameplay choice window. It reads upgrade options from
  `IAbilityUpgradeService`, reads display data from `IStaticDataService`,
  creates ability cards, and on selection writes `UpgradeRequest` into
  `GameContext`.
- `Gameplay/Features/LevelUp/Systems/UpgradeAbilityOnRequestSystem.cs`
  Gameplay system reads `GameContext` entities with `AbilityId` +
  `UpgradeRequest`, calls `IAbilityUpgradeService.UpgradeAbility(...)`, and
  marks the request entity for destruction.
- `Meta/UI/Shop/ShopWindow.cs`
  Dynamic meta choice window. It reads available items from `IShopUIService`,
  observes `IStorageUIService`, and creates item views.
- `Meta/UI/Shop/Items/ShopItem.cs`
  UI item writes `BuyRequest` into `MetaContext` on click; it does not spend
  currency directly.
- `Meta/UI/Shop/Systems/BuyItemOnRequestSystem.cs`
  Meta system reads `MetaContext` entities with `BuyRequest` + `ShopItemId`,
  checks storage, updates storage, calls `IShopUIService.UpdatePurchasedItem(...)`,
  and marks the request entity for destruction.

### What "request" means in the reference

В `ecs-survivors` request - это не абстрактный event bus и не прямой вызов
effect-сервиса. Это короткоживущая entity в конкретном context.

Важно для текущего template: этот pattern остается справкой и deferred option,
а не обязательной реализацией. Мы пока выбираем MVP direct service calls.

Level-up:

```csharp
CreateEntity.Empty()
    .AddAbilityId(id)
    .isUpgradeRequest = true;
```

Куда отправляется: в `GameContext`.

Кто читает: `UpgradeAbilityOnRequestSystem`, через matcher `AbilityId` +
`UpgradeRequest`.

Кто применяет effect: `UpgradeAbilityOnRequestSystem` вызывает
`IAbilityUpgradeService.UpgradeAbility(...)`, потом помечает request entity на
destruction.

Shop:

```csharp
CreateMetaEntity.Empty()
    .AddShopItemId(Id)
    .isBuyRequest = true;
```

Куда отправляется: в `MetaContext`.

Кто читает: `BuyItemOnRequestSystem`, через matcher `BuyRequest` +
`ShopItemId`.

Кто применяет effect: `BuyItemOnRequestSystem` проверяет storage, списывает
gold, обновляет purchased state/UI cache и помечает request entity на
destruction.

Если позже вернемся к request pattern, для `UnityTemplate` без ECS это можно
будет маппить не в entity, а в явно названный request transport: маленький DI
service/queue, который только хранит requests. Этот transport не применяет
effect сам.

```text
LevelUpWindow
-> abilityUpgradeRequests.RequestUpgrade(abilityId)

GameplayState.Update()
-> abilityUpgradeProcessor.ProcessRequests()
-> abilityUpgradeRequests.ConsumeAll()
-> abilityUpgradeService.ApplyUpgrade(...)
```

Сейчас это не внедряем. Вернуться к request transport стоит, если direct calls
начнут ломать порядок исполнения, pause/close lifecycle, тестируемость или
начнут размазывать один gameplay/meta effect по нескольким UI presenters.

### What This Means For UnityTemplate

```text
MainMenu
-> scene HUD like HomeHUD
-> StartGame calls GameStateMachine.Enter<LoadGameplayState>()
-> Settings calls WindowService.Open(WindowId.SettingsWindow)
-> never calls SceneLoader
```

```text
SettingsWindow / GameplayMenuWindow
-> BaseWindow prefab
-> opened by WindowService
-> instantiated under UIRoot set by UIInitializer
-> closes itself through WindowService
-> may request state transition through GameStateMachine
```

```text
LevelUpWindow / ShopWindow style gameplay menus
-> read display data from UI-facing services/static data
-> subscribe to UI-facing service events when needed
-> presenter/window calls one high-level feature/domain service method
-> service validates and applies gameplay/meta effect
-> UI does not split effect across low-level services
```

Не добавляем отдельный `UIFlowService`, presenter/controller слой для flow,
generic scene scope или registry. В референсе их нет.

## UI Migration Plan

### UI Step 1. Add window infrastructure

Добавить минимальный набор из `ecs-survivors`:

```text
Gameplay/Windows/BaseWindow
Gameplay/Windows/WindowId
Gameplay/Windows/IWindowService
Gameplay/Windows/WindowService
Gameplay/Windows/IWindowFactory
Gameplay/Windows/WindowFactory
Gameplay/Windows/Configs/WindowConfig
Gameplay/Windows/Configs/WindowsConfig
```

Bind в `ProjectInstaller`:

```text
Container.Bind<IWindowService>().To<WindowService>().AsSingle()
Container.Bind<IWindowFactory>().To<WindowFactory>().AsSingle()
```

Prefab lookup остается config-backed, как в `ecs-survivors`, и текущий
UnityTemplate rule разделяет два случая:

```text
Value config with gameplay/settings numbers
-> installer serialized reference
-> FromInstance(...)
-> inject where needed

Example:
EnemySpawnerConfig
-> GlobalConfigInstaller
-> EnemySpawner

Dynamic prefab registry by id/path
-> resource-backed registry is allowed
-> lookup during factory/static-data flow
```

`WindowsConfig` - это prefab registry, а не numeric/settings config, поэтому он
остается в `Resources/Configs/Windows/windowConfig`.

### UI Step 2. Add scene UI root initialization

Добавить `UIInitializer` по примеру `ecs-survivors`:

```text
UIInitializer
-> [field: SerializeField] RectTransform UIRoot
-> [Inject] IWindowFactory
-> Initialize()
-> windowFactory.SetUIRoot(UIRoot)
```

В сценах, где открываются dynamic windows, добавить root object `UIRoot` и
зарегистрировать `UIInitializer` через существующий `SceneInitializationInstaller`
паттерн.

### UI Step 3. Migrate SettingsView to window flow

Текущий `SettingsView` больше не должен быть direct injected dependency в
`MainMenu`.

Целевой flow:

```text
MainMenu.SettingsButton
-> windowService.Open(WindowId.SettingsWindow)
-> WindowFactory creates SettingsWindow prefab under UIRoot
```

`SettingsView` становится `BaseWindow` или отдельным `SettingsWindow`, сохраняя
существующий `SettingsPresenter` и audio/localization logic внутри окна.

### UI Step 4. Migrate gameplay menu modal to window flow

Текущий `Restart` prefab используется как gameplay menu modal. Это не
result/game-over UI: он открывается scene HUD кнопкой-шестеренкой, а не
`GameOverOrParagonState`.

Целевой flow:

```text
Gameplay scene Canvas
-> GearButton
-> GameplayMenuButton.OpenGameplayMenu()
-> pauseService.SetPaused(true)
-> windowService.Open(WindowId.GameplayMenuWindow)

GameplayMenuWindow.RestartButton
-> pauseService.SetPaused(false)
-> windowService.Close(Id)
-> stateMachine.Enter<LoadGameplayState>()

GameplayMenuWindow.MainMenuButton
-> pauseService.SetPaused(false)
-> windowService.Close(Id)
-> stateMachine.Enter<LoadMainMenuState>()

GameplayMenuWindow.CloseButton
-> pauseService.SetPaused(false)
-> windowService.Close(Id)
```

Не открываем result window из `GameplayState` напрямую, если это станет
responsibility конкретного result state. State owns lifecycle; window only
asks for state transition.

### UI Step 5. Keep MainMenu as scene HUD

`MainMenu` остается scene object аналогом `HomeHUD`.

Допустимо:

```text
MainMenu.StartGame -> GameStateMachine.Enter<LoadGameplayState>()
MainMenu.OpenSettings -> WindowService.Open(WindowId.SettingsWindow)
```

Недопустимо:

```text
MainMenu -> SceneLoader.LoadScene(...)
MainMenu -> custom UIFlowService -> StateMachine
```

### UI Step 6. Use MVP direct service calls for gameplay choices

Reference pattern из `LevelUpWindow` и `ShopWindow` остается известным, но для
текущего `UnityTemplate` откладываем request transport. MVP проще: presenter
может напрямую дергать high-level service.

Допустимо:

```text
LevelUpWindow
-> abilityUpgradeService.GetUpgradeOptions()
-> staticData.GetAbilityLevel(...)
-> player selects card
-> abilityUpgradeService.UpgradeAbility(id)

ShopWindow
-> shopUIService.GetAvailableShopItems
-> storageUIService events/current values
-> shopService.BuyItem(id)
```

Недопустимо:

```text
ShopItem
-> storage.RemoveGold(...)
-> purchasedItems.Add(...)
-> boostService.Apply(...)
-> saveService.Save(...)
```

Правило не в том, что UI вообще не может менять gameplay/meta state. Правило в
том, что один UI action должен идти в один owning feature/domain service, а не
размазывать effect по набору low-level сервисов.

```text
Good:
ShopPresenter -> shopService.BuyItem(id)

Bad:
ShopPresenter -> wallet.RemoveGold(price)
ShopPresenter -> purchases.Add(id)
ShopPresenter -> boostService.Apply(id)
ShopPresenter -> saveService.Save()
```

Request transport можно вернуть позже, если появятся реальные симптомы:

- несколько UI entry points применяют один и тот же effect по-разному;
- важен строгий порядок применения effect внутри active gameplay/meta loop;
- закрытие окна, pause или смена state конфликтуют с direct service call;
- стало трудно тестировать или логировать решения игрока отдельно от effect.

### UI Step 7. Cleanup and validation

Проверить:

- UI buttons either call `GameStateMachine.Enter(...)`,
  `WindowService.Open/Close(...)`, or one high-level feature/domain service;
- gameplay/meta choice windows may query UI-facing services/static data;
- gameplay/meta choice windows do not split one effect across low-level
  services from UI;
- no UI component calls `SceneLoader`;
- dynamic windows inherit `BaseWindow`;
- windows unsubscribe in `Cleanup/UnsubscribeUpdates`;
- `UIInitializer` sets `UIRoot` before any state opens a window in that scene;
- `WindowsConfig` contains every `WindowId` used by code;
- value configs are assigned in installers and injected;
- dynamic prefab registries may be resource-backed when used for id/path lookup;
- `EnemySpawner` uses `EnemySpawnerConfig.SpawnIntervalSeconds`, not a hardcoded
  interval constant;
- `GearButton -> GameplayMenuWindow` sets pause, and close/restart/main-menu
  actions unpause before closing or state transitions;
- Play Mode validates `MainMenu -> Settings`, `MainMenu -> Gameplay`,
  `GearButton -> GameplayMenuWindow -> Close`,
  `GearButton -> GameplayMenuWindow -> Restart`,
  `GearButton -> GameplayMenuWindow -> MainMenu`.

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

GameplayState.Enter()
-> enemySpawner.Start(exampleUnitFactory.CurrentUnit.transform)

GameplayState.Update()
-> enemySpawner waits for first gameplay click
-> enemySpawner spawns EnemyUnit near player every EnemySpawnerConfig.SpawnIntervalSeconds
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
-> enemySpawner.Cleanup()
-> exampleUnitFactory.Cleanup()
-> destroy created EnemyUnit and ExampleUnit objects
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
UI asks for state transitions or calls one high-level gameplay/meta service.
State owns lifecycle.
```
