# AGENTS.md

This file provides guidance to Codex and other coding agents when working with code in this repository.

## Project

Unity template project built with Unity `6000.4.4f1` + URP. Infrastructure is the main focus; gameplay domain code is expected to be added per project.

## Build & Run

This is a Unity project - there is no CLI build command. Open in Unity 6.0.4 and press Play. Scenes must be loaded in order: `Bootstrap -> MainMenu -> Gameplay` (configured in Build Settings).

No automated tests exist. Manual validation is done by running the game in the Editor.

## Repository Rules

- Keep Unity `.meta` files together with their assets and scripts.
- Do not edit generated folders: `Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, `.vs`, or IDE caches.
- Avoid editing generated solution/project files (`*.sln`, `*.csproj`) unless the task explicitly requires it.
- Prefer changes inside `Assets/_Project` for project code. Treat `Assets/Plugins`, `Assets/NaughtyAttributes`, `Assets/TextMesh Pro`, and imported packages as vendor code unless asked otherwise.
- Do not revert unrelated local changes. This repository may contain user, IDE, or Unity-generated changes in progress.

## Architecture

### Lifecycle reference lock

Lifecycle, state machine, DI, scene loading, UI flow, and gameplay loop work is reference-locked to:

```text
F:\unity_personal\Tutorials\ecs-survivors-viewers-main\src\ecs-survivors
```

Use `ecs-survivors` as the baseline for lifecycle shape, but do not copy ECS, generated code, Entitas features, or ECS-specific terminology into this template.

Before recommending, documenting, or implementing a lifecycle change:

1. Find the matching `ecs-survivors` class/file or concrete pattern.
2. Map that pattern to this project without adding extra architectural layers.
3. If there is no matching pattern in `ecs-survivors`, label it as a new proposal and ask before adding it to docs or code.

Reference-backed lifecycle rules:

- UI may call `GameStateMachine.Enter(...)`, as in `HomeHUD` / `GameOverWindow` from `ecs-survivors`.
- UI must not call `SceneLoader.LoadScene(...)` or `SceneLoader.ReloadScene(...)` directly.
- Loading states own scene loading.
- Enter/setup states prepare a mode after scene load.
- Active states own update, exit, and cleanup.
- `GameStateMachine` resolves states through `IStateFactory`, which resolves concrete state instances from Zenject, matching `ecs-survivors`.
- Bind states in `ProjectInstaller`; do not manually register state instances into the state machine.
- DI creates and wires objects, but does not decide when gameplay starts.
- Scene initializers write concrete scene references into concrete providers/services.
- Do not add flow services, gameplay runtime/session wrappers, generic scene scopes, registries, or other lifecycle-owner abstractions unless the user explicitly asks for a separate proposal.
- Do not add presenter/controller/flow layers as lifecycle intermediaries. Existing UI MVP code may stay where it is already part of the project.

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

### Lifecycle migration workflow

- Before starting lifecycle migration work, read `LIFECYCLE_MIGRATION_PLAN.md`.
- Implement the migration as small vertical slices in the order listed in the plan.
- Do not jump to later steps, validation items, or abstractions before the earliest unresolved implementation step is implemented or explicitly audited at its turn.
- Keep lifecycle changes reference-backed by `ecs-survivors`; if a needed decision is not reference-backed, stop and present it as a separate proposal.
- After each lifecycle slice, update the `Progress Tracker` in `LIFECYCLE_MIGRATION_PLAN.md` before the final response.
- If a planned step is reached, audited, and intentionally left without code because there is no real current need, mark it as `[deferred]` with the reason. Do not leave audited steps as `[todo]`, and do not treat deferred as permission to skip earlier unaudited steps.
- After each lifecycle slice, check whether `AGENTS.md` still matches the implemented architecture. Update it before the final response if current flow, state registration, lifecycle rules, or project conventions changed.
- After each completed slice, update `LIFECYCLE_MIGRATION_PLAN.md` or `AGENTS.md` only if real class names, rules, or decisions changed.

### Layer structure

```
Assets/_Project/Scripts/
├── Gameplay/         # Game features, one subfolder per feature
│   ├── Cameras/      # Camera provider and camera-facing gameplay helpers
│   ├── Common/       # Small common gameplay services: time, random, physics
│   ├── Level/        # Concrete scene references and providers
│   └── Units/        # Example gameplay feature folder
├── Infrastructure/   # App lifecycle: GameRunner, StateMachine, States, SceneManagement
├── GameplayData/     # ScriptableObject repositories and base Definitions
├── Audio/            # Audio subsystem: Domain/, Data/, View/
├── Localization/     # EN/RU via XML
├── MainMenu/         # Main menu UI
└── Utils/            # Coroutine helper, Pause service, Editor tools
```

### Core patterns

**State Machine** controls game flow. States live in `Infrastructure/GameStates/States/`. Current flow is `BootstrapState -> LoadMainMenuState -> MainMenuState -> LoadGameplayState -> GameplayEnterState -> GameplayState`. `GameplayPauseState` and `GameOverOrParagonState` are registered lifecycle states for later transitions.

Each state implements `IState, IGameState` directly or inherits a base state that does. Bind every lifecycle state in `ProjectInstaller` with self binding, resolve states through `IStateFactory`, transition with `_stateMachine.Enter<SomeState>()`, and keep scene loading inside loading states.

`GameplayEnterState` owns the current example gameplay setup: it reads `ILevelStartPointProvider.StartPoint`, calls `IExampleUnitFactory.Create(...)`, then enters `GameplayState`. `ExampleUnitFactory` follows the reference-backed prefab flow: `Resources` path `Gameplay/Units/ExampleUnit` -> `IAssetProvider` -> Zenject `IInstantiator`, mirroring `HeroFactory.AddViewPath("Gameplay/Hero/hero")` and `EntityViewFactory.CreateViewForEntity(...)` in `ecs-survivors`. `GameplayState` inherits `EndOfFrameExitState`; its `ExitOnEndOfFrame()` calls `IExampleUnitFactory.Cleanup()` to clean state-owned runtime objects, mirroring active-state cleanup in `BattleLoopState`. Do not use serialized gameplay prefab fields on `ProjectInstaller` for runtime gameplay prefabs unless explicitly approved as a new proposal. `GameplaySceneInitializer` writes the `MainCamera` and `GameplayStartPoint` scene references into `CameraProvider` and `LevelStartPointProvider`.

Scene initializers that implement Zenject interfaces must be listed in `SceneInitializationInstaller` on the scene `SceneContext`, matching the `ecs-survivors` pattern.

**Zenject DI** wires dependencies. No `new SomeService()` for DI-owned services - bind them in installers and inject them. `IInitializable` is allowed for local setup, UI presenters, settings, cached references, and other non-flow initialization. Do not use `IInitializable` to enter gameplay states, load gameplay scenes, or start active gameplay loops.

Three installer types:
- `MonoInstaller` - scene-bound, serialized fields for Unity references
- `ScriptableObjectInstaller` - asset-based config (e.g. `GlobalConfigInstaller`)
- Installers in `ProjectContext` apply project-wide; scene `GameObjectContext`/`SceneContext` are local

**MVP** used for UI: `Model` (data + PlayerPrefs), `Presenter` (`IInitializable`, UI/local logic), `View` (MonoBehaviour, UI only). See `Audio/` for the canonical example. Presenters must not become lifecycle intermediaries for gameplay flow.

**Repositories** - inherit `Repository<T> : ScriptableObject` where `T : Definition` for any game data. Bind with `FromInstance()` in an installer.

**UniTask** for all async and time-based operations. Coroutines are **never** used - including `WaitForSeconds`, `WaitUntil`, and similar. Use `UniTask.Delay`, `UniTask.WaitUntil`, `async UniTaskVoid` instead. This applies to all code: MonoBehaviour components, services, states.

**Component-based approach** - gameplay logic outside UI is built with `MonoBehaviour` components. One responsibility equals one component. Dependencies between gameplay components should use `[SerializeField]` or `GetComponent`, not Zenject. Example: `Gameplay/Units/` may split entity/view, movement, health, and interaction components.

### Adding new things

**New gameplay feature:**
1. Create a `Gameplay/FeatureName/` folder.
2. Put each gameplay behavior into a separate `MonoBehaviour` in that folder.
3. If the feature needs ScriptableObject data, create a `GameplayData/Definitions/FeatureName/` folder.

**New service:**
1. Define interface in `Domain/`
2. Implement class
3. Bind in installer: `Container.BindInterfacesAndSelfTo<MyService>().AsSingle()`
4. If the service has active lifecycle, call its explicit lifecycle methods from a state
5. Inject via `[Inject]`

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
- Usings grouped: System -> UnityEngine -> third-party -> project

## Validation

- When possible, validate Unity changes by opening the project in Unity `6000.4.4f1`.
- For code-only changes, at minimum check affected C# files for compile-time issues and keep scene/prefab references in sync.
- If adding or moving Unity assets, ensure corresponding `.meta` files are present.

## Git Notes

- Git may report `dubious ownership` in sandboxed environments. Use a per-command safe directory override when inspecting status:
  `git -c safe.directory=F:/unity_personal/UnityTemplate status --short --branch`
- Do not create commits, branches, stage files, or rewrite history unless the user asks for that explicitly.

## Key dependencies

- **Zenject** - DI container (in `Plugins/`)
- **UniTask** - async/await (Cysharp)
- **DOTween** - tweening (loading curtain fade)
- **Cinemachine 2.10.7**, **Input System 1.19.0**, **URP 17.4.0**
