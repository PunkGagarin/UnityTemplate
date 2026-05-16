# AGENTS.md

This file provides guidance to Codex and other coding agents when working with code in this repository.

## Project

Unity template project built with Unity `6000.4.4f1` + URP. Infrastructure is the main focus; `Gameplay/Units` and `Gameplay/Enemies` are minimal example features used to exercise lifecycle and DI patterns.

## Build & Run

This is a Unity project - there is no CLI build command. Open in Unity `6000.4.4f1` and press Play. Scenes must be loaded in order: `Bootstrap -> MainMenu -> Gameplay` (configured in Build Settings).

No project-owned automated tests exist under `Assets/_Project`. Manual validation is done by running the game in the Editor.

## Repository Rules

- Keep Unity `.meta` files together with their assets and scripts.
- Do not edit generated folders: `Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, `.vs`, or IDE caches.
- Avoid editing generated solution/project files (`*.sln`, `*.csproj`) unless the task explicitly requires it.
- Prefer changes inside `Assets/_Project` for project code. Treat `Assets/Plugins`, `Assets/NaughtyAttributes`, `Assets/TextMesh Pro`, and imported packages as vendor code unless asked otherwise.
- Do not revert unrelated local changes. This repository may contain user, IDE, or Unity-generated changes in progress.

## Architecture

### Lifecycle reference lock

Lifecycle, state machine, DI, scene loading, UI flow, and gameplay loop work is reference-locked to `ecs-survivors`.
Resolve its local path from `REFERENCES.local.md` first when present, otherwise from `REFERENCES.md`.
If neither file contains a valid local path, ask the user for the current `ecs-survivors` checkout path before doing lifecycle work.

Implemented UnityTemplate architecture is the first source of truth. When a rule, pattern, or working example already exists in `AGENTS.md` or `Assets/_Project`, follow this project even if it intentionally differs from `ecs-survivors`.

Use `ecs-survivors` only for new architecture/lifecycle decisions that are not already described or exemplified in UnityTemplate, or when the user explicitly asks to compare with the reference. Do not "correct" user-approved UnityTemplate deviations back to `ecs-survivors` unless the user asks to revisit that decision.

Before recommending, documenting, or implementing a new lifecycle pattern:

1. Check `AGENTS.md` and existing UnityTemplate code first.
2. If the pattern is not already covered locally, find the matching `ecs-survivors` class/file or concrete pattern.
3. Map that pattern to this project without adding extra architectural layers.
4. If there is no matching pattern in `ecs-survivors`, label it as a new proposal and ask before adding it to docs or code.

Reference-backed lifecycle rules:

- UI may call `GameStateMachine.Enter(...)`, as in `HomeHUD` / `GameOverWindow` from `ecs-survivors`.
- UI may query UI-facing gameplay/meta services and static data for display, as in `LevelUpWindow` and `ShopWindow`.
- Current UnityTemplate decision: gameplay/meta choice UI uses MVP direct service calls for now, not request queues. This is a user-approved simplification from the `ecs-survivors` request-entity pattern.
- UI must not call `SceneLoader.LoadScene(...)` or `SceneLoader.ReloadScene(...)` directly.
- UI must not run gameplay cleanup or control gameplay service lifecycle. Opening/closing gameplay menu may toggle the high-level `PauseService`, but UI must not start, stop, tick, or clean individual gameplay services.
- Runtime gameplay/meta changes from UI should go through high-level feature/domain services, not scattered low-level calls across multiple services.
- Loading states own scene loading.
- Enter/setup states prepare a mode after scene load.
- Active states own update, exit, and cleanup.
- `GameStateMachine` resolves states through `IStateFactory`, which resolves concrete state instances from Zenject, matching `ecs-survivors`.
- Bind states in `ProjectInstaller`; do not manually register state instances into the state machine.
- DI creates and wires objects, but does not decide when gameplay starts.
- Scene initializers write concrete scene references into concrete providers/services.
- Do not add flow services, gameplay runtime/session wrappers, generic scene scopes, lifecycle registries, or other lifecycle-owner abstractions unless the user explicitly asks for a separate proposal.
- Do not add presenter/controller/flow layers as lifecycle intermediaries. Existing UI MVP code may stay where it is already part of the project.

Reference map for common lifecycle/UI decisions:
- `HomeHUD` backs scene HUD flow: UI button calls `IGameStateMachine.Enter(...)` or `IWindowService.Open(...)`.
- `LoadingBattleState` backs loading-state ownership: loading state calls scene loader, then enters setup state.
- `BattleEnterState` backs enter/setup ownership: setup state creates gameplay runtime objects, then enters active loop state.
- `BattleLoopState` backs active loop ownership: active state receives ticks and owns exit cleanup.
- `StateFactory` backs state resolution: state machine asks factory, factory resolves concrete states from Zenject.
- `LevelInitializer` and `UIInitializer` back concrete scene references: scene objects write references into concrete providers/services.
- `WindowService`, `WindowFactory`, `BaseWindow`, `WindowsConfig`, `GameOverWindow`, and `GameOverState` back dynamic window flow.
- `LevelUpWindow`, `ShopWindow`, `UpgradeAbilityOnRequestSystem`, and `BuyItemOnRequestSystem` are the reference for request-based gameplay/meta choice UI, but request transport is deferred in this template.

Reference-backed asset and prefab rules:

- Runtime prefab creation from gameplay states/factories is part of lifecycle and DI work. It must be checked against the full `ecs-survivors` chain, not only against the final `IInstantiator` call.
- The reference chain for gameplay views is: state/domain factory creates gameplay data with a resource path, `IAssetProvider` loads from `Resources`, and a view factory instantiates with Zenject `IInstantiator`.
- Do not replace this with serialized gameplay prefab fields on `ProjectInstaller` unless `ecs-survivors` has the same pattern for that case, or the user explicitly approves it as a new proposal.
- A partial API match is not enough. If only `InstantiatePrefabForComponent(...)` matches but asset ownership/loading differs, call it out as a mismatch before changing code or docs.
- Do not copy ECS-only service dependencies such as `GameEntity`, generated contexts, or `ICollisionRegistry` into this template. When a reference service depends on ECS, keep only the lifecycle/DI placement and adapt the service API to Unity-neutral types.

Service lifecycle rule:

```text
State decides when.
Service knows how.
```

If a service is passive, do not add lifecycle methods to it. If a service owns subscriptions, timers, spawn loops, input modes, async tasks, or update work, expose explicit methods such as `Start`, `Stop`, `Enable`, `Disable`, `Update`, or `Cleanup`, and call them from the owning state.

### Architecture workflow

- `AGENTS.md` is the source of truth for current architecture rules.
- `LIFECYCLE_MIGRATION_PLAN.md` is a historical migration/audit log, not the active task plan. Do not add new work there unless the user explicitly reopens migration planning.
- Keep new or undocumented lifecycle decisions reference-backed by `ecs-survivors`; if a needed decision is neither covered locally nor reference-backed, stop and present it as a separate proposal.
- After each architecture/lifecycle/UI/DI slice, check whether `AGENTS.md` still matches the implemented architecture. Update it before the final response if current flow, state registration, lifecycle rules, or project conventions changed.

### Layer structure

```
Assets/_Project/Scripts/
├── Gameplay/         # Game features, one subfolder per feature
│   ├── Cameras/      # Camera provider and camera-facing gameplay helpers
│   ├── Common/       # Small common gameplay services: time, random, physics
│   ├── Enemies/      # Minimal enemy feature: view, factory, spawner service
│   ├── Level/        # Concrete scene references and providers
│   ├── Units/        # Example gameplay feature folder
│   └── Windows/      # Dynamic window infrastructure and window configs
├── Infrastructure/   # App lifecycle: GameRunner, StateMachine, States, SceneManagement
├── GameplayData/     # ScriptableObject repositories and base Definitions
├── Audio/            # Audio subsystem: Domain/, Data/, View/
├── Localization/     # EN/RU via XML
├── MainMenu.cs       # Main menu UI entry script
├── Restart.cs        # Example restart/menu UI script
└── Utils/            # ContentUi helper, Pause service, Editor tools
```

### Core patterns

**State Machine** controls game flow. States live in `Infrastructure/GameStates/States/`. Current flow is `BootstrapState -> LoadMainMenuState -> MainMenuState -> LoadGameplayState -> GameplayEnterState -> GameplayState`. `GameplayPauseState` and `GameOverOrParagonState` are registered lifecycle states for later transitions. Do not enter `GameplayPauseState` for the gear menu until `GameplayState` has explicit suspend/resume semantics; a normal state transition out of `GameplayState` runs cleanup.

Each state implements `IState, IGameState` directly or inherits a base state that does. Bind every lifecycle state in `ProjectInstaller` with self binding, resolve states through `IStateFactory`, transition with `_stateMachine.Enter<SomeState>()`, and keep scene loading inside loading states.

`GameplayEnterState` owns the current example gameplay setup: it reads `ILevelStartPointProvider.StartPoint`, calls `IExampleUnitFactory.Create(...)`, then enters `GameplayState`. `ExampleUnitFactory` follows the reference-backed prefab flow: `Resources` path `Gameplay/Units/ExampleUnit` -> `IAssetProvider` -> Zenject `IInstantiator`, mirroring `HeroFactory.AddViewPath("Gameplay/Hero/hero")` and `EntityViewFactory.CreateViewForEntity(...)` in `ecs-survivors`. `GameplayState` inherits `EndOfFrameExitState`; it starts/ticks/cleans active gameplay services such as `IEnemySpawner`, skips gameplay ticks while `PauseService.IsPaused`, and its `ExitOnEndOfFrame()` calls cleanup for state-owned runtime objects. `EnemyFactory` follows the same prefab flow with `Resources` path `Gameplay/Enemies/EnemyUnit`. `EnemySpawner` reads tunable numeric values from injected `EnemySpawnerConfig`, which is assigned in `GlobalConfigInstaller`. Do not use serialized gameplay prefab fields on `ProjectInstaller` for runtime gameplay prefabs unless explicitly approved as a new proposal. `GameplaySceneInitializer` writes the `MainCamera` and `GameplayStartPoint` scene references into `CameraProvider` and `LevelStartPointProvider`.

Scene initializers that implement Zenject interfaces must be listed in `SceneInitializationInstaller` on the scene `SceneContext`, matching the `ecs-survivors` pattern.

**Window Flow** follows the reference-backed dynamic window path:
`WindowService.Open(WindowId)` -> `WindowFactory.CreateWindow(...)` ->
`WindowsConfig` prefab lookup at `Resources/Configs/Windows/windowConfig` ->
Zenject `IInstantiator` under the scene `UIRoot`. `UIInitializer` writes the
scene `Canvas`/`UIRoot` into `WindowFactory` through `SceneInitializationInstaller`.
`SettingsView` is now a `BaseWindow` opened by `MainMenu` through
`WindowService.Open(WindowId.SettingsWindow)`. In `Gameplay`, the scene-owned
HUD `GearButton` sets `PauseService.SetPaused(true)` and opens the dynamic
`WindowId.GameplayMenuWindow`; `Restart` backs that gameplay menu modal and
unpauses before close/restart/main-menu actions. This is not a result/game-over
window, and `GameOverOrParagonState` must not open it unless explicitly
redesigned. Do not inject concrete windows such as `SettingsView` directly into
menu UI.

**Gameplay Menu Pause** is not a `GameplayPauseState` transition yet.
`GearButton -> PauseService.SetPaused(true) -> WindowService.Open(WindowId.GameplayMenuWindow)`.
`Close`, `Restart`, and `MainMenu` actions must unpause first; loading states
also reset pause as a safety measure. `GameplayState` skips active gameplay
ticks while `PauseService.IsPaused`, but keeps state-owned runtime objects alive.

**Zenject DI** wires dependencies. No `new SomeService()` for DI-owned services - bind them in installers and inject them. `IInitializable` is allowed for local setup, UI presenters, settings, cached references, and other non-flow initialization. Do not use `IInitializable` to enter gameplay states, load gameplay scenes, or start active gameplay loops.

**Config Assets vs Prefab Registries** are intentionally different. Value/config
assets with gameplay or settings numbers should be assigned in installers,
bound with `FromInstance(...)`, and injected; `EnemySpawnerConfig` is the current
example. Dynamic prefab registries that map ids to prefabs may stay
resource-backed, matching `ecs-survivors` `WindowsConfig`/`StaticDataService`
lookup. `WindowsConfig` is a prefab registry, not a numeric settings config.
When adding a new value config, create the asset under `Assets/_Project/Data/Config`,
add a serialized auto-property to `GlobalConfigInstaller`, bind it with
`FromInstance(...)`, and inject the config into the owning service.

Three installer types:
- `MonoInstaller` - scene-bound, serialized fields for Unity references
- `ScriptableObjectInstaller` - asset-based config (e.g. `GlobalConfigInstaller`)
- Installers in `ProjectContext` apply project-wide; scene `GameObjectContext`/`SceneContext` are local

**MVP** used for UI: `Model` (data + PlayerPrefs), `Presenter` (`IInitializable`, UI/local logic), `View` (MonoBehaviour, UI only). See `Audio/` for the canonical example. Presenters must not become lifecycle intermediaries for gameplay flow.

**Gameplay choice UI** uses MVP direct service calls for now. Presenters/windows may read display data from services/static data and may call high-level feature/domain methods such as `BuyItem(id)`, `SelectUpgrade(id)`, or `ApplyChoice(id)` directly. Keep the operation encapsulated in one owning service; do not spread one UI click across low-level calls such as `RemoveGold`, `AddPurchasedItem`, `ApplyBoost`, and `SaveProgress` from the presenter. The `ecs-survivors` request pattern (`UpgradeRequest`/`BuyRequest` entities processed later by systems) is documented as a deferred option, not the current default.

**Deferred request transport** may be reconsidered later if direct service calls cause real problems: multiple UI entry points apply the same effect differently, strict ordering inside the active gameplay/meta loop matters, close/pause/state transitions conflict with direct calls, or choices need to be logged/tested separately from their effects. If this returns, do not add an abstract event bus by default; model it as a named request transport owned by the feature, then process it from the owning gameplay/meta loop.

**Repositories** - inherit `Repository<T> : ScriptableObject` where `T : Definition` for any game data. Bind with `FromInstance()` in an installer.

**UniTask** for all async and time-based operations. Coroutines are **never** used, including Unity yield instructions such as `WaitForSeconds`, `UnityEngine.WaitUntil`, and similar. Use `UniTask.Delay`, `UniTask.WaitUntil`, `async UniTaskVoid` instead. This applies to all code: MonoBehaviour components, services, states.

**Component-based approach** - runtime Unity objects are built from small `MonoBehaviour` components. One responsibility equals one component. Dependencies between gameplay components should use `[SerializeField]` or `GetComponent`, not Zenject. State-owned lifecycle helpers such as factories and spawners can be plain DI services that are started, updated, and cleaned by states.

### Adding new things

**New gameplay feature:**
1. Create a `Gameplay/FeatureName/` folder.
2. Put runtime object behavior into focused `MonoBehaviour` components.
3. Put factories/spawners/lifecycle helpers in the same feature folder as plain DI services when a state owns their lifecycle.
4. If the feature needs ScriptableObject data, create a `GameplayData/Definitions/FeatureName/` folder.

**New service:**
1. Define the interface beside the feature/service implementation, or in the relevant `Domain/` folder for subsystems that already use one.
2. Implement the class.
3. Bind in an installer with `Container.Bind<IMyService>().To<MyService>().AsSingle()` or `Container.BindInterfacesAndSelfTo<MyService>().AsSingle()` when self binding is also needed.
4. If the service has active lifecycle, call its explicit lifecycle methods from a state.
5. Inject via `[Inject]`.

**New value config:**
1. Create a `ScriptableObject` config with serialized auto-properties.
2. Place the asset in `Assets/_Project/Data/Config`.
3. Add it to `GlobalConfigInstaller` as a serialized auto-property.
4. Bind it with `FromInstance(...)` through the shared config binding helper.
5. Inject it into the service that owns the behavior.

**New prefab registry:**
1. Use this only for dynamic prefab lookup by id/path, not for gameplay tuning numbers.
2. Keep it resource-backed when it mirrors `WindowsConfig`/`StaticDataService` from `ecs-survivors`.
3. Document the lookup path and keep every enum id mapped to a prefab.

**New game state:**
1. `public class MyState : IState, IGameState`
2. Implement `Enter()` and `Exit()`
3. Implement update only when the state owns an active loop
4. Add `Container.BindInterfacesAndSelfTo<MyState>().AsSingle()` in `ProjectInstaller`
5. Transition: `_stateMachine.Enter<MyState>()`

**New gameplay data:**
1. `public class MyDef : Definition { }`
2. `public class MyRepo : Repository<MyDef> { }` with `[CreateAssetMenu]`
3. Bind in `RepositoryInstaller`

## Coding conventions

| Element | Convention |
|---|---|
| Private fields | `_camelCase` |
| Constants | `UPPER_SNAKE_CASE` |
| Interfaces | `IPascalCase` |
| Properties | `PascalCase`, `private set` |
| Namespaces | Mirror directory path |

- Use field injection for Zenject dependencies. Prefer `[Inject] private SomeService _service;` over constructor injection.
- Use serialized auto-properties for inspector-exposed fields: `[field: SerializeField] private GameObject Obj { get; set; }`. Do not add new `[SerializeField] private GameObject _obj;` fields.
- When converting existing serialized fields to serialized auto-properties, update scene/prefab YAML references to the backing field name, for example `<Obj>k__BackingField`.
- Prefer `List<T>` over arrays (`T[]`) where possible, including `[field: SerializeField]` collections
- For new files, prefer usings grouped as System -> UnityEngine -> third-party -> project. In existing files, keep the surrounding order unless the file is already being cleaned up.

## Validation

- When possible, validate Unity changes by opening the project in Unity `6000.4.4f1`.
- For code-only changes, at minimum check affected C# files for compile-time issues and keep scene/prefab references in sync.
- If adding or moving Unity assets, ensure corresponding `.meta` files are present.
- Current manual lifecycle/UI validation should include `MainMenu -> Settings -> Apply/Cancel`, `MainMenu -> Gameplay`, `GearButton -> GameplayMenuWindow -> Close`, `GearButton -> GameplayMenuWindow -> Restart`, and `GearButton -> GameplayMenuWindow -> MainMenu`.
- Current gameplay validation should include `Bootstrap -> LoadMainMenuState -> MainMenuState -> LoadGameplayState -> GameplayEnterState -> GameplayState`, `ExampleUnit` creation at `GameplayStartPoint`, enemy spawning after first gameplay click, and cleanup when restarting or returning to main menu.

## Git Notes

- Git may report `dubious ownership` in sandboxed environments. Use a per-command safe directory override when inspecting status:
  `git -c safe.directory=F:/unity_personal/UnityTemplate status --short --branch`
- Do not create commits, branches, stage files, or rewrite history unless the user asks for that explicitly.

## Key dependencies

- **Zenject** - DI container (in `Plugins/`)
- **UniTask** - async/await (Cysharp)
- **DOTween** - tweening (loading curtain fade)
- **Cinemachine 2.10.7**, **Input System 1.19.0**, **URP 17.4.0**
