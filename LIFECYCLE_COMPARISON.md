# Lifecycle Comparison

Сравнение трех Unity-проектов:

- `UnityTemplate`: текущий шаблон в `F:\unity_personal\UnityTemplate`.
- `IgnisBearer`: развитая версия игрового проекта в `F:\unity_personal\IgnisBearer`.
- `ecs-survivors`: внешний референс в `F:\unity_personal\Tutorials\ecs-survivors-viewers-main\src\ecs-survivors`.

Фокус: точка входа, загрузка сцен, жизненный цикл состояний, UI-триггеры,
runtime-инициализация, cleanup и возврат между режимами. ECS/OOP-подходы
намеренно не сравниваются.

## Короткий Вывод

Все три проекта используют похожую верхнеуровневую идею: стартовая сцена
поднимает глобальный Zenject-контекст, затем игра управляется через state
machine и загрузку сцен.

Разница в зрелости lifecycle:

- `UnityTemplate` имеет правильный минимальный скелет: bootstrap, state machine,
  loading curtain, enum-based scene loading. Gameplay lifecycle пока почти
  пустой.
- `IgnisBearer` показывает, как этот скелет начинает жить в реальной игре:
  gameplay поднимается через scene installers и `GameplayBootstrap`, есть
  сохранения, прогресс, pause UI, restart и game end. Но часть переходов уже
  обходит state machine напрямую через `SceneLoader`.
- `ecs-survivors` жестче держит lifecycle в state machine: вход в режим,
  update-loop, выход, cleanup и переход к следующему режиму описаны явно.

Практический вывод для `UnityTemplate`: лучше брать у `IgnisBearer` подход к
scene-level composition через installers/bootstrap, а у `ecs-survivors` -
дисциплину ownership-а lifecycle внутри state machine.

## Быстрая Карта

| Область | UnityTemplate | IgnisBearer | ecs-survivors |
| --- | --- | --- | --- |
| Unity version | `6000.4.4f1` | `6000.2.13f1` | `6000.4.4f1` |
| Entry scene | `Bootstrap` | `Bootstrap` | `Boot` |
| Main scenes | `Bootstrap`, `MainMenu`, `Gameplay` | `Bootstrap`, `MainMenu`, `Gameplay` | `Boot`, `HomeScreen`, `Meadow` |
| State machine | Простая синхронная | Та же основа, но больше overload-ов | `ITickable`, promise-based transitions |
| Gameplay state | `GameplayState` пустой | `GameloopState` пустой, runtime в `GameplayBootstrap` | `BattleLoopState` владеет battle loop |
| Gameplay init | Пока не выражен | Scene installers + `GameplayBootstrap` | `BattleEnterState` + `BattleLoopState` |
| Game over | Заготовка | `GameEndService` + `GameEndUI`, restart reload scene | `GameOverState` + `GameOverWindow` |
| Cleanup | Пустые `Exit()` | Частично через Zenject dispose/scene unload | Явный state teardown |
| Главный риск | Runtime размазан по сцене в будущем | Переходы обходят state machine | Сложнее transition model |

## Общий Flow

### UnityTemplate

```text
Bootstrap scene
-> ProjectContext / installers
-> GameRunner.Initialize()
-> BootstrapState
-> MainMenuState
-> MainMenu scene
-> MainMenu.StartGame button
-> LoadGameplayState
-> Gameplay scene
-> GameplayState
```

Дополнительные переходы:

```text
Gameplay scene restart button -> LoadGameplayState -> GameplayState
Gameplay scene main menu button -> MainMenuState -> MainMenu scene
```

### IgnisBearer

```text
Bootstrap scene
-> ProjectContext / installers
-> GameRunner.Initialize()
-> BootstrapState
-> MainMenuState
-> MainMenu scene
-> MainMenu.StartGame button
-> LoadGameplayState
-> Gameplay scene
-> scene installers
-> GameplayBootstrap.Initialize()
-> GameloopState
```

Внутри gameplay:

```text
GameplayBootstrap
-> load PlayerData or create start data
-> create level
-> init currency/buildings/slots/lanterns/skill tree/game end/tutorial
-> gameplay services tick through Zenject
```

Дополнительные переходы:

```text
GameEndService -> GameEndUI.Show()
GameEndUI.Restart button -> SceneLoader.ReloadScene()
PausePopup.MainMenu button -> SceneLoader.LoadScene(MainMenu)
```

### ecs-survivors

```text
Boot scene
-> BootstrapInstaller.Initialize()
-> BootstrapState
-> LoadProgressState
-> ActualizeProgressState
-> LoadingHomeScreenState
-> HomeScreen scene
-> HomeScreenState
-> StartBattle button
-> LoadingBattleState
-> Meadow scene
-> BattleEnterState
-> BattleLoopState
-> GameOverState
-> GameOverWindow
-> ReturnHome button
-> LoadingHomeScreenState
```

## Точка Входа

### UnityTemplate

- Первая build-сцена: `Assets/_Project/_Scenes/Bootstrap.unity`.
- Глобальные биндинги идут через `ProjectContext`.
- `ProjectInstaller` регистрирует `BootstrapState`, `LoadGameplayState`,
  `MainMenuState`, `GameplayState` и `GameStateMachine`.
- `BootstrapInstaller` регистрирует `GameRunner`.
- `GameRunner.Initialize()` регистрирует все `IGameState` и входит в
  `BootstrapState`.

Если открыть `MainMenu` или `Gameplay` напрямую, есть риск, что глобальные
сервисы не будут подняты.

### IgnisBearer

- Первая build-сцена тоже `Assets/_Project/_Scenes/Bootstrap.unity`.
- Глобальный flow почти совпадает с `UnityTemplate`.
- `ProjectInstaller` регистрирует `BootstrapState`, `MainMenuState`,
  `LoadGameplayState`, `GameloopState` и `GameStateMachine`.
- `Gameplay`-сцена дополнительно содержит набор scene installers:
  `GameplayInstaller`, `GameplayUiInstaller`, `BuildingsInstaller`,
  `UnitsInstaller`, `SkillTreeInstaller`, `TutorialInstaller`, `PlayerInstaller`.
- Реальная gameplay-инициализация происходит не в `GameloopState`, а в
  `GameplayBootstrap`, который создается из `GameplayInstaller`.

То есть `IgnisBearer` сохраняет тот же entry pattern, но переносит наполнение
gameplay lifecycle в сценовый composition root.

### ecs-survivors

- Первая build-сцена: `Assets/Scenes/Boot.unity`.
- `BootstrapInstaller` является главным installer-ом.
- Он регистрирует сервисы, фабрики, state machine, все states и coroutine runner.
- В `Initialize()` он сразу вызывает `Enter<BootstrapState>()`.
- Есть editor-helper `SwitchToEntrySceneInEditor`, который при запуске из другой
  сцены возвращает проект в entry scene, если глобальный context еще не создан.

Референс сильнее защищен от запуска "не той" сцены.

## Bootstrap Phase

### UnityTemplate

`BootstrapState` сейчас делает минимальный bootstrap:

- инициализирует `AudioService`;
- сразу переводит игру в `MainMenuState`.

В комментариях уже намечены будущие обязанности: curtain, загрузка ресурсов,
asset provider, инициализация глобальных систем.

### IgnisBearer

`BootstrapState` почти такой же:

- инициализирует `AudioService`;
- сразу входит в `MainMenuState`.

Отличие не в bootstrap phase, а в следующем уровне: когда загружается gameplay
scene, ее installers поднимают много реальных сервисов и UI-компонентов.

### ecs-survivors

Bootstrap разбит на несколько явных шагов:

- `BootstrapState` загружает static data;
- `LoadProgressState` загружает или создает прогресс;
- `ActualizeProgressState` досчитывает offline-прогресс;
- после этого игра входит в загрузку home screen.

Это делает запуск более длинным, но лучше структурированным: каждый шаг имеет
одну причину существования.

## Загрузка Сцен

### UnityTemplate

- Загрузка идет через `SceneLoader.LoadScene(SceneEnum)`.
- `MainMenuState` грузит `SceneEnum.MainMenu`.
- `LoadGameplayState` показывает `LoadingCurtain`, грузит `SceneEnum.Gameplay`,
  скрывает curtain и входит в `GameplayState`.
- `SceneLoader` использует `UniTask` и `SceneManager.LoadSceneAsync`.

Текущая модель простая, но `LoadGameplayState` уже смешивает несколько задач:
curtain, загрузку сцены, будущую загрузку ресурсов и переход дальше.

### IgnisBearer

- Загрузка тоже идет через `SceneLoader.LoadScene(SceneEnum)`.
- `SceneLoader` хранит `_currentScene` и умеет `ReloadScene()`.
- `LoadGameplayState` после загрузки `Gameplay` входит в `GameloopState`.
- `GameEndUI.Restart` не идет через state machine, а вызывает
  `SceneLoader.ReloadScene()`.
- `PausePopup.MainMenu` тоже не идет через state machine, а напрямую вызывает
  `SceneLoader.LoadScene(SceneEnum.MainMenu)`.

Это удобно и быстро для разработки, но lifecycle начинает расползаться: часть
переходов идет через states, часть напрямую через scene loader.

### ecs-survivors

- Загрузка идет через `ISceneLoader.LoadScene(string, Action onLoaded)`.
- Для home screen и battle есть отдельные loading states:
  `LoadingHomeScreenState` и `LoadingBattleState`.
- Loading state отвечает только за загрузку сцены и переход в следующий state.
- Сценовые зависимости поднимаются через scene initializers.

Здесь сильная сторона в том, что загрузка сцены явно отделена от "входа в режим".

## Жизненный Цикл State Machine

### UnityTemplate

`SimpleStateMachine` работает синхронно:

```text
Enter new state
-> call Exit() on current state
-> replace current state
-> call Enter() on new state
```

Ограничения:

- нет собственного `Tick()` у state machine;
- нет отдельного lifecycle для долгоживущих states;
- `async void Enter()` усложняет обработку ошибок и повторных переходов;
- выход из состояния происходит сразу, а не в безопасной точке кадра;
- cleanup зависит от того, что конкретный state или сцена сами все почистят.

### IgnisBearer

State machine основана на том же синхронном подходе, но расширена:

- есть `Enter(Type stateType)`;
- есть payload-переходы;
- есть overload-и для unit state chains;
- появился интерфейс `IUpdateState`, но global `GameStateMachine` его не tick-ает.

Главное наблюдение: state machine стала использоваться не только для глобальных
game states, но и как общий паттерн для unit states. Это повышает reuse, но
смешивает разные масштабы жизненного цикла в одном механизме.

### ecs-survivors

`GameStateMachine` обновляется каждый кадр как `ITickable`.

Если активный state реализует update lifecycle, state machine вызывает его
`Update()`. Для долгоживущих states используется модель:

```text
Enter()
-> OnUpdate() every tick
-> transition requested
-> BeginExit()
-> finish current frame
-> ExitOnEndOfFrame()
-> EndExit()
-> enter next state
```

Состояние владеет своим update-loop, а teardown происходит в предсказуемой фазе.

## Main Menu / Home Screen

### UnityTemplate

`MainMenuState` загружает сцену `MainMenu`. После этого `MainMenu` MonoBehaviour
подписывает кнопки:

- Start -> `LoadGameplayState`;
- Settings -> открывает `SettingsView`;
- Credits -> пока пусто.

State отвечает за загрузку сцены, а UI-действия живут в MonoBehaviour сцены.

### IgnisBearer

`MainMenuState` работает так же:

- показывает curtain;
- грузит `MainMenu`;
- скрывает curtain.

`MainMenu` MonoBehaviour:

- Start -> `LoadGameplayState`;
- Settings -> `SettingsView`;
- Credits -> `CreditsPopup`.

То есть menu lifecycle почти совпадает с нашим, но UI уже наполнен.

### ecs-survivors

`HomeScreenState` не просто показывает сцену, а запускает `HomeScreenFeature`,
который каждый кадр:

- обновляет meta simulation;
- обновляет UI золота;
- обслуживает shop UI;
- периодически сохраняет прогресс;
- делает cleanup временного runtime.

Ключевое отличие: home screen в референсе является активным режимом игры, а не
только загруженной сценой.

## Gameplay Entry

### UnityTemplate

`LoadGameplayState`:

- показывает curtain;
- грузит `Gameplay`;
- скрывает curtain;
- входит в `GameplayState`.

`GameplayState` пока пустой. Поэтому будущая gameplay-логика либо должна быть
добавлена в state lifecycle, либо будет жить в объектах сцены и отдельных
сервисах.

### IgnisBearer

`LoadGameplayState`:

- показывает curtain;
- грузит `Gameplay`;
- скрывает curtain;
- входит в `GameloopState`.

`GameloopState` при этом тоже пустой. Реальный entry находится в
`GameplayBootstrap.Initialize()`:

- загружает `PlayerData` или создает стартовые данные;
- создает уровень;
- инициализирует meta currency;
- создает building slots;
- создает prebuild buildings;
- инициализирует church light consume progressor;
- включает building slot enabler;
- создает lantern slots и стартовые lanterns;
- инициализирует skill tree;
- инициализирует game end service;
- запускает tutorial.

Это важная промежуточная модель: gameplay runtime уже имеет явный bootstrap, но
он принадлежит Zenject scene lifecycle, а не global game state lifecycle.

### ecs-survivors

Gameplay entry разделен:

- `LoadingBattleState` грузит сцену `Meadow`;
- `BattleEnterState` создает героя в start point;
- `BattleLoopState` создает и запускает battle runtime;
- `BattleLoopState.OnUpdate()` каждый кадр выполняет весь battle loop.

Ключевое отличие: gameplay runtime явно начинается и явно заканчивается внутри
state lifecycle.

## Gameplay Runtime

### UnityTemplate

Runtime loop пока не задан. `GameplayState.Enter()` и `Exit()` пустые.

### IgnisBearer

Runtime живет в нескольких слоях:

- `GameplayBootstrap` собирает стартовое состояние.
- Сервисы, зарегистрированные как `IInitializable`, стартуют через Zenject.
- Сервисы, зарегистрированные как `ITickable`, например `ProgressService`,
  обновляются через Zenject tick loop.
- MonoBehaviour-компоненты сцены продолжают работать через Unity lifecycle.
- Unit state machines живут отдельно от global game state machine.

Это рабочая production-like схема, но ownership распределен между сценой,
Zenject, сервисами, MonoBehaviour и global state machine.

### ecs-survivors

Runtime loop более централизован:

- `BattleLoopState` создает battle runtime;
- state machine tick вызывает active state update;
- active state выполняет runtime systems;
- при выходе active state сам выполняет teardown.

## Pause / Game End / Возврат

### UnityTemplate

Есть заготовки:

- `GameplayPauseState`;
- `GameOverOrParagonState`;
- `Restart` MonoBehaviour с кнопками Restart и Main Menu.

Сейчас `Restart` напрямую вызывает:

- Restart -> `LoadGameplayState`;
- Main Menu -> `MainMenuState`.

Отдельный lifecycle для pause/game over пока не оформлен.

### IgnisBearer

Pause и game end уже есть, но частично обходят state machine:

- `PausePopupController` открывает `PausePopup`.
- Play закрывает popup.
- Settings открывает `SettingsView`.
- Main Menu напрямую вызывает `SceneLoader.LoadScene(SceneEnum.MainMenu)`.
- `GameEndService` слушает ресурс church light storage.
- Когда ресурс доходит до нуля, `GameEndService` вызывает `OnGameEnded` и
  показывает `GameEndUI`.
- `GameEndUI.Restart` вызывает `SceneLoader.ReloadScene()`.

Есть один конкретный риск в `PausePopupController`: в `OnDestroy()` обработчик
`ShowPauseButton` добавляется повторно вместо отписки. Это может оставлять
лишнюю подписку, если объект переживет ожидаемый lifecycle.

### ecs-survivors

Game over встроен в lifecycle:

- death condition переводит state machine в `GameOverState`;
- при выходе из `BattleLoopState` весь battle runtime деактивируется,
  чистится и уничтожает игровые views/runtime objects;
- `GameOverState` открывает `GameOverWindow`;
- Return Home закрывает окно и входит в `LoadingHomeScreenState`.

Ключевое отличие: game over является состоянием приложения, а не только UI.

## Cleanup / Teardown

### UnityTemplate

Сейчас cleanup в state-ах почти пустой:

- `BootstrapState.Exit()` пустой;
- `MainMenuState.Exit()` пустой;
- `LoadGameplayState.Exit()` пустой;
- `GameplayState.Exit()` пустой;
- `GameplayPauseState.Exit()` пустой;
- `GameOverOrParagonState.Exit()` пустой.

Это нормально для раннего шаблона, но при росте проекта может привести к
накоплению подписок, сервисного состояния, tweens, runtime-объектов и UI.

### IgnisBearer

Cleanup частично опирается на Zenject/scene lifecycle:

- `GameplayBootstrap.Dispose()` сохраняет player data.
- `PlayerDataService.Dispose()` тоже вызывает `Save()`.
- `ProgressService.Dispose()` отписывается от `GameEndService.OnGameEnded`.
- Многие MonoBehaviour отписываются в `OnDestroy()`.
- При reload/load scene Unity уничтожает сценовые объекты, и Zenject должен
  dispose-ить scene-scoped сервисы.

Сильная сторона: реальные сервисы уже думают о dispose/save.

Слабая сторона: global states почти ничего не чистят, а часть переходов идет
напрямую через `SceneLoader`, поэтому state machine не является единственным
местом lifecycle cleanup.

### ecs-survivors

Cleanup является частью state lifecycle:

- Home screen при выходе чистит storage/shop UI и home feature.
- Battle loop при выходе деактивирует reactive systems, чистит systems,
  destruct-ит runtime objects и вызывает teardown.
- Windows уничтожаются через window service.

Практически это снижает шанс, что старый режим оставит после себя живые
объекты или подписки.

## Что Можно Перенять В UnityTemplate

1. Из `IgnisBearer`: scene-level composition.

   Для `Gameplay` полезно иметь отдельные installers:

   ```text
   GameplayInstaller
   GameplayUiInstaller
   GameplayBootstrap
   Feature/service installers by domain
   ```

   Это хорошо масштабируется, когда сцена обрастает level data, UI, services,
   factories и save/load.

2. Из `IgnisBearer`: отдельный gameplay bootstrap.

   `GameplayBootstrap` как идея хорош: он собирает стартовое состояние сцены.
   Но лучше, чтобы global state явно знал, что этот bootstrap завершился, и мог
   перевести игру в active gameplay state без гонок.

3. Из `ecs-survivors`: разделить loading и enter.

   Целевая форма:

   ```text
   LoadGameplayState -> GameplayEnterState -> GameplayState
   ```

   `LoadGameplayState` только грузит сцену. `GameplayEnterState` создает session
   runtime. `GameplayState` живет до pause/game over/win/exit.

4. Из `ecs-survivors`: добавить update lifecycle в global state machine.

   Достаточно интерфейса:

   ```csharp
   public interface IUpdateableState
   {
       void Tick();
   }
   ```

   Тогда active gameplay/menu state сможет явно владеть loop-ом, а не отдавать
   все MonoBehaviour и Zenject tickables без общего владельца.

5. Из `ecs-survivors`: безопасный выход из долгоживущих states.

   Для gameplay полезна фаза:

   ```text
   request transition -> finish frame -> cleanup -> enter next state
   ```

   Особенно если в кадре одновременно происходят UI click, save, scene loading,
   game end, tweens и gameplay ticks.

6. Из `IgnisBearer` и `ecs-survivors`: сделать Game Over полноценным state.

   `GameOverOrParagonState` уже есть как заготовка. Его можно сделать владельцем:

   - остановки gameplay;
   - сохранения результатов;
   - показа game end UI;
   - restart;
   - return to menu.

7. Убрать прямые scene transitions из UI.

   UI лучше оставить только источником намерения:

   ```text
   PausePopup.MainMenu click -> stateMachine.Enter<LoadMainMenuState>()
   GameEndUI.Restart click -> stateMachine.Enter<LoadGameplayState>()
   ```

   Тогда cleanup и save не будут зависеть от конкретной кнопки.

8. Убрать `async void` из state transitions там, где возможно.

   Лучше, чтобы асинхронные переходы возвращали `UniTask`, а state machine знала,
   что переход еще выполняется. Это упростит обработку ошибок, двойных кликов и
   повторных входов.

9. Добавить защиту от повторного transition request.

   Например, блокировать кнопку Start/Restart после клика или дать state machine
   флаг `IsTransitioning`.

10. Защититься от прямого запуска не-entry scene.

   В `ecs-survivors` это делает editor-helper, который возвращает запуск в entry
   scene, если глобальный context еще не создан. Для наших проектов это тоже
   полезно.

## Возможная Целевая Схема Для UnityTemplate

```text
Bootstrap
-> BootstrapState
   - init audio
   - load global configs/assets
   - maybe load player progress
-> LoadMainMenuState
   - show curtain
   - load MainMenu scene
   - hide curtain
-> MainMenuState
   - menu runtime ready
   - wait for player action
-> LoadGameplayState
   - show curtain
   - load Gameplay scene
   - wait for scene installers/bootstrap
   - hide curtain
-> GameplayEnterState
   - create session/runtime
   - spawn level data
   - load save/start data
-> GameplayState
   - active gameplay loop
   - can enter pause/game over/win
-> GameplayPauseState
   - pause runtime
   - return to GameplayState or MainMenu
-> GameOverOrParagonState
   - stop gameplay
   - save results
   - show results UI
   - restart or return to menu
```

## Итог

`UnityTemplate` уже имеет правильный фундамент. `IgnisBearer` показывает, как
этот фундамент начинает использоваться в настоящей игре: появляются scene
installers, gameplay bootstrap, save/load, tickable services, pause и game end.
Но именно там видно, почему lifecycle легко начинает расползаться, если UI
напрямую грузит сцены.

`ecs-survivors` сильнее всего не из-за конкретной архитектуры, а из-за
дисциплины владения lifecycle:

```text
State owns mode lifecycle.
Scene provides objects.
Scene installers compose dependencies.
UI only requests transitions.
Cleanup happens before next mode starts.
```

Для нашего проекта лучший путь: сохранить простоту `UnityTemplate`, перенять
scene composition из `IgnisBearer` и постепенно подтянуть state lifecycle к
уровню явности из `ecs-survivors`.
