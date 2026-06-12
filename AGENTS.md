# AGENTS.md

This file provides guidance to Codex and other coding agents when working with code in this repository.

## Operational Gates

Before gameplay code changes, write this ownership map in the working update:

```text
Behavior:
Owner:
Caller:
Why not service/root:
```

Do not edit until ownership is clear. If you cannot name the owning component,
do not put the behavior in a service yet. Run these gates before code changes:

- Existing project first: check `AGENTS.md` and current `Assets/_Project`; Petri Core code and approved deviations win over `ecs-survivors`.
- User-facing implementation plans should be written in Russian by default unless the user asks for another language.
- Ownership: object-local behavior goes to focused components; cross-object coordination, input, spawning, cleanup, lifecycle, and active loops go to state-owned services/states.
- Root object: runtime roots are facades only. Key entity scripts must not own behavior logic, including validation, ownership changes, geometry, state mutation, spawn/pool reset, or collection cleanup; they only expose facade methods/properties and pass ticks to focused components. Gameplay object components must own a domain behavior or pure visual behavior; do not add MVP-style Presenter/Controller/View/ViewModel layers to runtime gameplay objects. Use gameplay names like `Visual`, `Line`, `Indicator`, or `Feedback` for pure visuals. Different unit/enemy behavior should be a prefab component variant, not a config bool in a shared component.
- Reuse: search for similar components/interfaces before adding object-local logic. Prefer a reusable component over a per-entity duplicate whenever the behavior shape matches.
- Public API: after component extraction, search callers for every new or changed public method/property/event. Delete unused facade/component members, or narrow visibility when only same-assembly components call them.
- Event/update: use events/subscriptions/explicit calls for discrete changes; use `Update` only for continuous work.
- Static state: avoid mutable runtime `static` state. If a static event, cache, counter, buffer, or collection is required, add an explicit `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` reset so Enter Play Mode without domain reload starts cleanly.
- Lifecycle: states decide when; services know how.
- Unity assets: keep `.meta` files, preserve GUIDs, update prefabs/scenes for serialized fields/components.
- Localization: every new player-facing text, and every discovered existing text without localization, must be added through the localization flow. Use `ToLocalize` on static text components, `LocalizationTool`/localized keys for dynamic strings, update `Assets/_Project/Resources/LocalizationFile/localization.csv`, and regenerate/update the Russian and English XML language files.
- Docs: update `PetriCore_GDD.md` for player-facing rules; update `PetriCore_BALANCE.md` for concrete numbers; update `Spawn.md` for level spawn tables whenever `LevelCatalogConfig` waves change; update `Pacing.md` when spawn, currency rewards, level rewards, or upgrade costs/prerequisites change.
- Validation: run available compile/static checks and report Unity Editor/manual validation gaps.

## Project

Petri Core is a Unity project built with Unity `6000.4.4f1` + URP. Game design is documented in [PetriCore_GDD.md](PetriCore_GDD.md), concrete gameplay numbers and tuning tables live in [PetriCore_BALANCE.md](PetriCore_BALANCE.md), level spawn tables live in [Spawn.md](Spawn.md), and progression pacing lives in [Pacing.md](Pacing.md). Infrastructure is still template-derived; `Gameplay/Units` and `Gameplay/Enemies` are minimal example features used to exercise lifecycle and DI patterns until production gameplay replaces them.

## Build & Run

This is a Unity project - there is no CLI build command. Open in Unity `6000.4.4f1` and press Play. Scenes must be loaded in order: `Bootstrap -> MainMenu -> Gameplay` (configured in Build Settings).

No project-owned automated tests exist under `Assets/_Project`. Manual validation is done by running the game in the Editor.

Do not run `dotnet build`, MSBuild, or generated solution/project builds as routine validation for this Unity project. They are not the project-owned build path and may fail because the local environment lacks a .NET SDK/MSBuild or Unity-generated references. Use Unity Editor validation when available; otherwise use focused static checks/searches and report the Unity/manual validation gap. Only run CLI solution builds when the user explicitly asks for them or a Unity-compatible build pipeline has been confirmed.

## Repository Rules

- Keep Unity `.meta` files together with their assets and scripts.
- Do not edit generated folders: `Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, `.vs`, `.idea`, or IDE caches.
- Avoid editing generated solution/project files (`*.sln`, `*.csproj`) unless the task explicitly requires it.
- Prefer changes inside `Assets/_Project` for project code. Treat `Assets/Plugins`, `Assets/NaughtyAttributes`, `Assets/TextMesh Pro`, and imported packages as vendor code unless asked otherwise.
- Do not revert unrelated local changes. This repository may contain user, IDE, or Unity-generated changes in progress.

## Architecture

### Lifecycle reference lock

Petri Core implementation is the first source of truth. Use `ecs-survivors`
only for new lifecycle/state machine/DI/scene/UI flow decisions not already
covered locally, or when explicitly asked to compare. Resolve its path from
`REFERENCES.local.md`, then `REFERENCES.md`; ask if neither has a valid path.

Lifecycle rules:
- UI may call `GameStateMachine.Enter(...)` or `IWindowService.Open(...)`, and may query UI-facing services/static data.
- UI must not call `SceneLoader` directly or start/stop/tick/clean gameplay services.
- Runtime gameplay/meta changes from UI go through high-level feature/domain services.
- Loading states load scenes; enter/setup states prepare runtime objects; active states tick, exit, and clean up.
- `GameStateMachine` resolves Zenject-bound states through `IStateFactory`; bind states in `ProjectInstaller`.
- DI wires objects but does not decide when gameplay starts.
- Scene initializers write scene references into concrete providers/services.
- Do not add flow services, gameplay runtime/session wrappers, lifecycle registries, or presenter/controller flow layers unless explicitly requested.
- Gameplay/meta choice UI currently uses MVP direct service calls, not request queues.

Prefab rules:
- Runtime prefab flow is `Resources` path -> `IAssetProvider` -> Zenject `IInstantiator`.
- Do not replace runtime prefab loading with serialized `ProjectInstaller` prefab fields without explicit approval.
- Do not copy ECS-only dependencies from `ecs-survivors`; adapt only lifecycle/DI placement.

### Architecture workflow

- `AGENTS.md` is the source of truth for current architecture rules.
- Keep new or undocumented lifecycle decisions reference-backed by `ecs-survivors`; if a needed decision is neither covered locally nor reference-backed, stop and present it as a separate proposal.
- After each architecture/lifecycle/UI/DI slice, check whether `AGENTS.md` still matches the implemented architecture. Update it before the final response if current flow, state registration, lifecycle rules, or project conventions changed.
- Every time code adds gameplay, content, UI behavior, player-facing rules, or other design-relevant behavior that is not already described in `PetriCore_GDD.md`, update the GDD in the same task before the final response. Keep concrete gameplay numbers out of the GDD; if the change adds or changes balance values, update `PetriCore_BALANCE.md` instead or alongside the GDD. If the change touches `LevelCatalogConfig` waves, update `Spawn.md` as the only detailed spawn-table document and `Pacing.md` if rewards/progression expectations change. Pure infrastructure refactors that do not change design or player-facing behavior do not require GDD or balance-document updates.

### Layer structure

Prefer project code under `Assets/_Project/Scripts`. Main areas: `Gameplay/` for
features, `Infrastructure/` for app lifecycle/state machine/scene loading,
`GameplayData/` for repositories/definitions, plus `Audio/`, `Localization/`,
`MainMenu/`, and `Utils/`. Runtime object feature folders keep focused
components in a `Components/` subfolder.

### Core patterns

Detailed flow notes live in [PetriCore_ARCHITECTURE.md](PetriCore_ARCHITECTURE.md).
Keep `AGENTS.md` operational and update the architecture notes when current flow changes.

- State flow is `Bootstrap -> MainMenu -> GameplayEnter -> GameplayLoop`; terminal transitions go to game-over or level-complete states/windows.
- `GameplayLoopState` owns active service ticking/fixed-ticking/cleanup and skips gameplay ticks while paused.
- Runtime prefab flow is `Resources` path -> `IAssetProvider` -> Zenject `IInstantiator`; runtime objects belong under gameplay scene hierarchy.
- Battle molecule ownership: `BattleMoleculeFactory` only creates molecule prefabs; `BattleMoleculeService` owns the registered molecule list, subscriptions, ticking, active selection, active feed target provider, and cleanup; `AtomCoreService` ticks core runtime behavior; `AtomCoreConnectionAtomFlow` coordinates core-owned connection atom flow, `AtomCoreConnectionAtomSource` selects startable core atoms, and `AtomCoreConnectionAtomMotion` owns flow movement/geometry until delivery to a molecule receiver.
- Gameplay tutorial ownership: `GameplayTutorialService` owns tutorial progress, UI overlay spawning, and subscriptions to core/molecule/enemy events; `GameplayLoopState` starts/ticks/cleans it with active gameplay services. Tutorial view is a prefab under gameplay UI resources and receives UI elements through serialized fields.
- Enemy ownership: `EnemyService` coordinates enemy waves, active enemy ticking, enemy selection for area pushes, and boss-kill notification; `MergeEnemyService` owns enemy merge-pair checks, merge links/groups, tether ticking, death-wave progression, and merge cleanup, while `GameplayLoopState` starts/cleans it; `EnemySpawner` creates; `EnemyUnit` is a facade over identity/vitality/merge/lifecycle/runtime-behavior/knockback components; `EnemyKillRewardService` owns non-boss kill subscriptions, kill-reward economy rules, and reward cleanup; `EnemyProjectileService` owns enemy projectile spawning/ticking/cleanup; `CurrencyPickupService` owns physical currency pickup spawning/collection. Enemy components own enemy-local behavior, `EnemyKnockback` owns enemy-local push displacement, `MassEnemyArcMovement` is the mass-enemy movement variant, `RangedEnemyStopMovement`/`RangedEnemyAttack` are ranged-enemy local variants, and `BossCoreCollision` is the boss one-shot variant.
- UI may call state/window services, but must not load scenes or control gameplay service lifecycle. Gameplay menu pause is not a `GameplayPauseState` transition yet.
- Dynamic windows opened through `IWindowService` use the shared modal backdrop from `WindowFactory`; backdrop color is configured in `WindowsConfig`, while outside-click dismissal belongs in the owning window's `OnBackdropClicked()` override.
- Zenject wires dependencies; do not `new` DI-owned services. `IInitializable` must not enter states, load scenes, or start gameplay loops.
- MVP UI may call high-level feature/domain services directly; keep each operation encapsulated in one owning service.
- Use UniTask for async/time-based work. Coroutines and Unity yield instructions are never used.

### Adding new things

- New gameplay feature: create `Gameplay/FeatureName/`; put object-local behavior in focused components; put input, timers, spawning, cleanup, resource/progress changes, and cross-object coordination in a plain DI service ticked by the owning state.
- New service: define interface beside the implementation, bind it in an installer, inject with `[Inject]`, and expose explicit lifecycle methods only when a state owns them.
- New value config: create a `ScriptableObject` with serialized auto-properties under `Assets/_Project/Data/Config`, add it to `GlobalConfigInstaller`, bind with `FromInstance(...)`, and inject it into the owning service.
- New prefab registry: use only for dynamic id/path prefab lookup, not tuning values; document the lookup path and keep enum ids mapped.
- New game state: implement `IState, IGameState`, bind with `Container.BindInterfacesAndSelfTo<MyState>().AsSingle()`, and transition through `_stateMachine.Enter<MyState>()`.
- New gameplay data: inherit `Definition`, store in a `Repository<T>`, and bind in `RepositoryInstaller`.

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
- ScriptableObject configs with multiple semantic groups must use editor-friendly `[field: Header("...")]` sections with human-readable names.
- When converting existing serialized fields to serialized auto-properties, update scene/prefab YAML references to the backing field name, for example `<Obj>k__BackingField`.
- UI text must not be left as raw player-facing strings. Static `TextMeshProUGUI` labels use `ToLocalize` with a key; dynamic text uses localized format/name keys from `LocalizationTool`.
- Prefer `List<T>` over arrays (`T[]`) where possible, including `[field: SerializeField]` collections
- For new files, prefer usings grouped as System -> UnityEngine -> third-party -> project. In existing files, keep the surrounding order unless the file is already being cleaned up.

## Validation

- When possible, validate Unity changes by opening the project in Unity `6000.4.4f1`.
- For code-only changes, at minimum check affected C# files for compile-time issues and keep scene/prefab references in sync.
- Do not attempt `dotnet build`/MSBuild as a fallback validation step for ordinary Unity code changes; prefer targeted static inspection and explicit manual validation notes unless the user asks for CLI build validation.
- If adding or moving Unity assets, ensure corresponding `.meta` files are present and Unity-valid. Script `.cs.meta` files should contain a `MonoImporter` block, folder `.meta` files should contain `folderAsset: yes` and `DefaultImporter`, and new/moved prefabs or assets must keep stable GUID references. Prefer letting Unity generate or refresh these files when possible.
- Current manual lifecycle/UI validation should include `MainMenu -> Settings -> Apply/Cancel`, `MainMenu -> Update -> TalentTreeWindow`, `MainMenu -> LevelSelector previous/next arrows`, `MainMenu -> Reset`, `MainMenu -> Gameplay`, `GearButton -> GameplayMenuWindow -> Close`, `GearButton -> GameplayMenuWindow -> Restart`, and `GearButton -> GameplayMenuWindow -> MainMenu`.
- Current gameplay validation should include `Bootstrap -> LoadMainMenuState -> MainMenuState -> LoadGameplayState -> GameplayEnterState -> GameplayLoopState`, `AtomCore` creation at `GameplayStartPoint`, enemy spawning after first gameplay click, ranged enemy stop-and-shoot behavior, projectile damage absorption by membrane, non-boss enemy Nucleotids pickup spawn and hover collection, no boss currency pickup, `LevelProgressService` completion into `LevelCompleteState -> LevelCompleteWindow`, final-level completion into `LevelCompleteState -> GameCompleteWindow`, first-clear isotope reward persistence, no completion-isotope reward on replay, and cleanup when restarting or returning to main menu.

## Git Notes

- Git may report `dubious ownership` in sandboxed environments. Use a per-command safe directory override when inspecting status:
  `git -c safe.directory=<repo-root> status --short --branch`
- Do not create commits, branches, stage files, or rewrite history unless the user asks for that explicitly.

## Key dependencies

- **Zenject** - DI container (in `Plugins/`)
- **UniTask** - async/await (Cysharp)
- **DOTween** - tweening (loading curtain fade)
- **Cinemachine 2.10.7**, **Input System 1.19.0**, **URP 17.4.0**
