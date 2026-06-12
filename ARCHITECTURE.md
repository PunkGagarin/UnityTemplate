# Petri Core Architecture Notes

This document keeps detailed architecture and flow notes. `AGENTS.md` is the
short operational rule file; update both when architecture changes.

## State Machine

State machine implementations live in `Infrastructure/GameStates/StateMachine/`,
state interfaces and base helpers in `StateInfrastructure/`, state resolution in
`Factory/`, and concrete lifecycle states in `States/`.

Current flow:

```text
BootstrapState
-> LoadMainMenuState
-> MainMenuState
-> LoadGameplayState
-> GameplayEnterState
-> GameplayLoopState
```

Terminal gameplay transitions:
- core death: `GameplayLoopState -> GameOverOrParagonState -> GameOverWindow`
- level complete: `GameplayLoopState -> victory currency auto-collect -> LevelCompleteState -> LevelCompleteWindow`

`GameplayPauseState` is registered for later transitions. Do not enter it for
the gear menu until `GameplayLoopState` has explicit suspend/resume semantics.

States are lifecycle orchestrators. They may load scenes, ask services/factories
to create runtime objects, start/tick/clean services, gate calls with pause or
transition conditions, and transition to another state. They must not contain
gameplay mechanics such as input-processing branches, drag/drop rules, click or
spawn progress, target selection, timers, cooldowns, physics queries, damage,
resources, score mutation, or UI choice application logic.

## Runtime Prefab Flow

Runtime prefab flow:

```text
Resources path -> IAssetProvider -> Zenject IInstantiator
```

Runtime gameplay objects must enter the active gameplay scene hierarchy.
Top-level runtime objects go under scene-owned `GameplayRuntime`; owned children
such as generated atoms are parented under their current owner.

Repeat-spawned runtime objects such as enemies and free atoms may be pooled by
their owning factories. Use `PooledFactory<T>` for shared active/pool ownership,
strict return-to-pool validation, and cleanup. Factory `Cleanup()` must destroy
both active and pooled instances before runtime hierarchy cleanup.

## Enemy Runtime

Enemy prefab paths, stats, level duration, completion reward, and parallel spawn
tracks live in `LevelCatalogConfig`. Enemy pooling is separated by `EnemyId` so
each type keeps its own prefab.

Spawn tracks are independent:
- each starts at `StartTimeSeconds`
- each optionally stops at `EndTimeSeconds`
- each may stop after `SpawnLimit`
- `0` spawn limit means unlimited while active

Runtime ownership:
- `EnemyService` owns wave timing, active enemy tracking, per-frame enemy
  ticking, death subscriptions, enemy selection for area pushes, and boss-kill
  notification.
- `MergeEnemyService` owns enemy merge eligibility, merge-pair checks, active
  merge links/groups, link tether ticking, death-wave progression, and merge
  cleanup. `GameplayLoopState` starts and cleans it, while `EnemyService` only
  registers spawned enemies and forwards merge tick phases so link motion still
  happens before core collision checks.
- `EnemyKillRewardService` owns non-boss kill subscriptions, kill-reward economy
  rules, reward cleanup, and configured currency pickup spawn requests.
- `EnemySpawner` is only the spawn helper: choose offscreen position, ask
  `EnemyFactory` to create the unit, apply spawn-time object setup, and keep
  multi-enemy wave spawns clustered in one offscreen sector.
- `CurrencyPickupService` owns physical currency pickup spawning, cursor-hover
  collection checks, victory auto-collection to the cursor, currency grant on
  collection, and pickup cleanup.
- `CurrencyPickup` is the pickup prefab facade. Pickup amount, hit-area checks,
  and collection feedback are component-owned.
- Enemy object-internal behavior stays on focused components. `EnemyUnit` is
  only the public facade over `EnemyIdentity`, `EnemyVitality`,
  `EnemyMergeState`, `EnemyRuntimeBehaviors`, `EnemyLifecycle`, and
  `EnemyKnockback`.
- `EnemyMovement` moves enemies directly toward the core; prefab-specific
  movement variants such as `MassEnemyArcMovement` own enemy-local path shapes.
- `EnemyKnockback` owns enemy-local push displacement when another gameplay
  system requests an area push through `EnemyService`.
- Enemy-local runtime behaviors implement `IEnemyRuntimeBehavior`;
  `EnemyRuntimeBehaviors` configures and ticks them while `EnemyService` owns
  the active enemy loop.
- Ranged enemies use prefab variants: `RangedEnemyStopMovement` owns stopping
  near the core, and `RangedEnemyAttack` owns only local cooldown/telegraph and
  sends shot requests. `EnemyProjectileService` owns projectile spawning,
  ticking, and cleanup; the `EnemyProjectile` root is a facade over projectile
  motion, lifetime, target-hit, damage, and runtime components.
- `EnemyCoreCollision` resolves normal overlap damage with the atom core while
  ticked through `EnemyUnit` by `EnemyService`.
- Boss one-shot core collision is a prefab component variant,
  `BossCoreCollision`, not a definition flag.

## Active Gameplay Loop

`GameplayEnterState` creates the core, configures `IBattleMoleculeService` with
that core, creates battle molecule prefabs, then enters `GameplayLoopState`.

`GameplayLoopState` inherits `EndOfFrameExitState`. It starts, ticks,
fixed-ticks, and cleans active gameplay services such as `IEnemyService`, `IMergeEnemyService`,
`IEnemyKillRewardService`, `IAtomCoreService`, `IBattleMoleculeService`, `IEnemyProjectileService`, `ICurrencyPickupService`, and
`ILevelProgressService`. It skips gameplay ticks while `PauseService.IsPaused`;
`ExitOnEndOfFrame()` cleans state-owned runtime services and objects.

## Core, Molecule, And Progress

`AtomCoreService` implements both setup-facing `IAtomCoreCreator` and
active-loop-facing `IAtomCoreService`. It creates/destroys the core, polls core
click input during the active gameplay loop, creates generated free atoms
through `FreeAtomFactory`, applies core HP and atom click count, exposes current
core transform for active targeting services, subscribes to core death during
`Start()`, and ticks current `AtomCore` plus core-owned connection atom flow
using the feed target exposed by `IBattleMoleculeFeedTargetProvider`.

`AtomCore` is the root facade: it exposes core-facing methods/properties and
passes ticks to focused components, but does not own behavior logic.
`AtomCoreClickInteraction` owns only local click hit-tests and click progress,
delegating point hit-tests to `PointHitArea`; generated atom ownership intake
belongs to `OwnedAtomReceiver`.
`AtomOrbit` owns owned atom orbit ticking for both the core and battle
molecules, while `AtomCoreHealth` owns HP, death, and shield-gated damage
resolution.

`BattleMoleculeFactory` only creates molecule prefabs. `IBattleMoleculeService`
owns the registered battle molecule list, molecule subscriptions, active
molecule ticking, active selection, active feed target provider, and cleanup.
`AtomCoreService` ticks core runtime behavior. Core-owned connection atom flow
is coordinated by `AtomCoreConnectionAtomFlow` using the current feed target
from `IBattleMoleculeFeedTargetProvider`; `AtomCoreConnectionAtomSource`
selects startable core-owned atoms, while `AtomCoreConnectionAtomMotion` owns
flow movement, geometry, speed, and tolerance config. `BattleMolecule` is a facade: setup,
identity, bond event relays, point hit-tests, core orbit/connection-line
coordination, connection arrival geometry, atom orbiting, charge consumption,
atom receiving, shot requests, attacks, aim-line feedback, and membrane
activation are owned by focused components. The facade may pass explicit ticks
and setup calls to those known components, but molecule behavior is not routed
through a generic runtime-behavior registry. The connection line is a gameplay
visual component (`BattleMoleculeConnectionVisual`) that observes bond changes
directly; gameplay objects do not add MVP-style Presenter, Controller, View, or
ViewModel layers. Attack-capable molecule prefabs use focused attack components
such as `StingerMoleculeAttack` and `SwarmMoleculeAttack` to resolve local shot
requests into raycasts, enemy damage, and shot-line feedback.

`FreeAtom` is also a facade. Ownership, despawn/destroy events, drag behavior,
connection-flow/drag state, collider enable/disable, spawn/pool reset, initial
scale reset, orbit motion, ownership intake, and radius lookup are
component-owned. Shared local components such as `AtomOrbit`,
`OwnedAtomReceiver`, `ObjectRadius`, `PointHitArea`, `ColliderSet`, and
`InitialLocalScale` should be reused instead of duplicating simple entity logic
per unit type.

Common gameplay helpers carry duplicated local math/state: `CompletionThreshold`
owns required/current/completed counter math for click, bond, and charge
progress; `OrbitMath` owns orbit angle/position math; `RandomGeometry` owns
random point sampling; `SquareArea2D` and `CircleArea2D` own reusable hit-area
math; `LineRendererUtility` owns runtime line setup/material/color.

`LevelProgressService` owns completion reward/unlock, marks the selected level
complete through `ILevelSelectionService`, grants first-clear completion reward
through `CurrencyService`, and raises completion for state transition.

## UI And Windows

Dynamic window flow:

```text
WindowService.Open(WindowId)
-> WindowFactory.CreateWindow(...)
-> WindowsConfig prefab lookup at Resources/Configs/Windows/windowConfig
-> modal root with raycast-blocking backdrop under scene UIRoot
-> Zenject IInstantiator under modal root
```

UI may call `GameStateMachine.Enter(...)` or `IWindowService.Open(...)`, but must
not load scenes or control gameplay service lifecycle.

Every dynamic window gets a shared modal backdrop. The backdrop blocks clicks to
UI behind the current window and forwards backdrop clicks to the opened
`BaseWindow`. Its color is configured in `WindowsConfig`. Windows opt into
dismiss-on-backdrop by overriding `OnBackdropClicked()`; result windows keep the
default no-op and require explicit button actions.

The gameplay menu is not a `GameplayPauseState` transition yet:

```text
GearButton
-> PauseService.SetPaused(true)
-> WindowService.Open(WindowId.GameplayMenuWindow)
```

`GameplayMenuWindow` must unpause before close/restart/main-menu actions.

## Result Flows

Game over is a normal state transition. `AtomCoreService` raises current core
death, `GameplayLoopState` transitions to `GameOverOrParagonState`, active
gameplay cleanup runs, and `GameOverOrParagonState` opens `GameOverWindow`.

Level complete is also a normal state transition. In the current MVP, the level
completes when `EnemyService` raises `BossKilled`. `GameplayLoopState` calls
`LevelProgressService.Complete()`, starts `CurrencyPickupService` victory
auto-collection for remaining pickup loot, waits for that short feedback, then
transitions to `LevelCompleteState`, which opens `LevelCompleteWindow`.

## Gameplay Input And Interaction

Gameplay input that can change runtime state belongs to the active gameplay loop.
Input polling, hit detection, cross-object targeting, spawn requests, and
drag/drop coordination should be state-owned services with explicit lifecycle
methods.

Object-local interaction state such as click progress, charge, owned-object
lists, and local visual feedback belongs on the runtime object or its focused
components.

Current drag/drop ownership is `DragService`: it reads input and camera data,
starts/moves/ends drag, handles raw release for active drags, and is ticked from
`GameplayLoopState`.

## Data, Config, And Meta

Value/config assets with gameplay or settings numbers should be assigned in
installers, bound with `FromInstance(...)`, and injected.

Dynamic prefab registries map ids to prefabs. `WindowsConfig` is a prefab
registry, not a numeric settings config.

`AtomCoreConfig` owns core setup values: base core HP, clicks required to
generate a free atom, generated atom spawn radius, and free atom orbit speed.

Talent-adjusted runtime values are applied by the current owner:
- `AtomCoreService` applies core HP and atom click count
- `BattleMoleculeFactory` applies atom charge count
- molecule-local attack components resolve shot damage and Pierce
- `AtomCoreConnectionAtomMotion` applies connection atom movement speed

Talent tree uses `TalentConfig`. `TalentService` owns talent progress and
buying; `CurrencyService` owns saved meta-currencies. Talent progress and
currencies currently use `PlayerPrefs` as MVP persistence. `ProgressData`
contains a save/progression version; if the stored version does not match the
current code version, `SaveLoadService` clears saved data and creates fresh
progress instead of migrating.
