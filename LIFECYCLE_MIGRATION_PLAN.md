﻿﻿﻿# Lifecycle Migration Plan

Цель: развить `UnityTemplate` так, чтобы реальный игровой цикл не уезжал внутрь
сцены, `MonoBehaviour`, `IInitializable` или `ITickable`, как это постепенно
произошло в `IgnisBearer`.

Сейчас принцип формулируем максимально просто:

```text
DI creates and wires objects.
Scene provides references and local dependencies.
External game state machine owns lifecycle.
States execute Enter / Tick / Exit directly.
UI requests transitions, but does not load scenes directly.
```

На этом этапе не вводим дополнительную сущность между `GameplayState` и игровыми
сервисами. Если позже `GameplayState` станет слишком большим, вынесем часть
реализации в helper или service. Но базовая парадигма должна быть очевидной:
lifecycle делает сам state.

## Проблема

В текущем шаблоне есть внешний `GameStateMachine`, но `GameplayState` пока пустой.
Если развивать проект естественным образом, есть риск прийти к модели
`IgnisBearer`:

```text
LoadGameplayState
-> load Gameplay scene
-> scene installers create GameplayBootstrap
-> GameplayBootstrap.Initialize() starts game
-> services and MonoBehaviours tick themselves
-> UI directly calls SceneLoader.ReloadScene() / LoadScene(MainMenu)
```

В такой модели внешний state machine остается формальным навигатором, но уже не
является владельцем игрового режима.

Нужная модель:

```text
LoadGameplayState
-> load Gameplay scene
-> scene dependencies become available
-> GameplayEnterState
-> GameplayState
-> GameplayPauseState / GameOverOrParagonState / LoadMainMenuState
```

## Главное Решение

States должны делать lifecycle напрямую:

```text
GameplayEnterState.Enter()
-> подготовить игровую сессию
-> создать/инициализировать level data, player, services, UI state
-> перейти в GameplayState

GameplayState.Enter()
-> включить активный gameplay

GameplayState.Tick()
-> выполнить активный игровой цикл
-> обработать pause / game over / win requests

GameplayState.Exit()
-> остановить активный gameplay
-> отписаться, сохранить, почистить состояние, если нужно
```

DI при этом не исчезает. Он просто не владеет жизненным циклом. Он поставляет
states нужные сервисы, фабрики и scene references.

## Целевая Схема States

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

Минимально можно оставить `MainMenuState` как сейчас, но для симметрии лучше
разделить загрузку меню и активное состояние меню.

## Границы Ответственности

### GameStateMachine

Отвечает за:

- регистрацию states;
- текущий активный state;
- вызов `Enter`;
- вызов `Exit`;
- вызов `Tick` у активного state, если он updateable;
- защиту от двойных transitions.

Не отвечает за:

- создание конкретного уровня;
- логику UI;
- сохранение игрового результата;
- детали gameplay-систем.

### States

Отвечают за lifecycle major modes:

- загрузить нужную сцену;
- войти в режим;
- каждый кадр обновлять режим;
- поставить режим на паузу;
- завершить режим;
- перейти в следующий major mode.

Именно states должны отвечать на вопрос:

```text
Когда игра началась?
Когда игра тикает?
Когда игра остановлена?
Когда можно грузить следующую сцену?
```

### DI / Installers

Отвечают за composition:

- bind global services в `ProjectContext`;
- bind scene services в `SceneContext`;
- bind factories;
- bind UI references;
- bind level data;
- bind scene reference holders.

Не отвечают за major-mode lifecycle.

`IInitializable` можно использовать для локальной подготовки, но не для старта
игрового режима.

Хорошо:

```text
cache references
subscribe local UI
register scene references
prepare service defaults
```

Плохо:

```text
start gameplay
start win/loss loop
load another scene
restart game
enter main menu
```

### Scene

Сцена предоставляет объекты:

- cameras;
- UI roots;
- spawn points;
- scene-specific configs;
- serialized references;
- MonoBehaviours that are pure view/adapters.

Сцена не должна сама решать, что игровой режим начался.

### UI

UI сообщает о намерениях:

```text
StartGame
Pause
Resume
Restart
ReturnToMainMenu
OpenSettings
```

UI не должен напрямую дергать:

```text
SceneLoader.LoadScene(...)
SceneLoader.ReloadScene()
```

## Scene Dependencies Через Providers

Проблема остается: global states живут в `ProjectContext`, а часть ссылок
появляется только после загрузки `Gameplay` scene.

Пока не вводим общий контейнер ссылок сцены. В референсе `ecs-survivors`
используется более конкретная схема:

```text
Scene initializer reads serialized scene references.
Initializer writes them into specific providers/services.
States consume those specific providers/services.
```

Примеры из `ecs-survivors`:

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
-> creates hero
```

Для нашего template это означает:

```text
GameplaySceneInitializer
-> fills LevelDataProvider
-> fills CameraProvider
-> fills UiRootProvider / WindowFactory / GameplayUiProvider
```

А state использует конкретные зависимости:

```csharp
public class GameplayEnterState : IState, IGameState
{
    private readonly ILevelDataProvider _levelData;
    private readonly IPlayerFactory _playerFactory;

    public void Enter()
    {
        _playerFactory.CreatePlayer(_levelData.StartPoint);
        // prepare other session state
    }
}
```

Если позже конкретных providers станет слишком много и появится повторяющийся
boilerplate, тогда можно подумать об общем контейнере scene references. Но пока
лучше держаться ближе к референсу и не вводить универсальную сущность заранее.

## State Responsibilities

### BootstrapState

Отвечает за глобальный старт:

- init audio;
- загрузка глобальных конфигов;
- загрузка player/global progress, если он общий для проекта;
- переход в `LoadMainMenuState`.

Не отвечает за gameplay scene setup.

### LoadMainMenuState

Отвечает только за загрузку `MainMenu`:

- show curtain;
- load `SceneEnum.MainMenu`;
- hide curtain;
- enter `MainMenuState`.

### MainMenuState

Отвечает за активное меню:

- принимает request стартовать игру;
- переводит Start Game в `LoadGameplayState`;
- при выходе чистит menu-specific состояние, если оно появится.

UI не должен напрямую грузить gameplay scene.

### LoadGameplayState

Отвечает только за загрузку gameplay scene:

- show curtain;
- load `SceneEnum.Gameplay`;
- дождаться, что scene initializers записали ссылки в нужные providers;
- hide curtain;
- enter `GameplayEnterState`.

Не создает level/player/session напрямую.

### GameplayEnterState

Отвечает за подготовку игровой сессии:

- берет scene references из конкретных providers/services;
- берет scene references из конкретных providers/services;
- использует инжектнутые factories/services;
- создает level/session/player/start data;
- готовит UI к gameplay;
- после успешной подготовки входит в `GameplayState`.

Если подготовка не удалась, здесь же можно уйти в error/retry/menu state.

### GameplayState

Отвечает за активный gameplay:

- включает активный игровой режим в `Enter`;
- каждый кадр выполняет gameplay logic в `Tick`;
- обрабатывает pause/game over/win/restart/main menu requests;
- не грузит сцены напрямую из UI;
- при выходе останавливает активный gameplay и чистит state-owned подписки.

На первом этапе `Tick` может быть пустым. Важно, что место для активного цикла
принадлежит state machine, а не scene bootstrap.

### GameplayPauseState

Отвечает за паузу:

- останавливает активные gameplay-процессы;
- показывает pause UI;
- resume -> возврат в `GameplayState`;
- main menu -> `LoadMainMenuState`;
- restart -> `LoadGameplayState`.

### GameOverOrParagonState

Отвечает за завершение сессии:

- остановить активный gameplay;
- сохранить результаты;
- показать game end UI;
- restart -> `LoadGameplayState`;
- main menu -> `LoadMainMenuState`.

## Update Lifecycle В State Machine

Добавить интерфейс:

```csharp
public interface IUpdateableState
{
    void Tick();
}
```

`GameStateMachine` должен быть `ITickable` и тикать только активное состояние:

```csharp
public class GameStateMachine : SimpleStateMachine<IGameState>, ITickable
{
    public void Tick()
    {
        if (_currentState is IUpdateableState updateable)
            updateable.Tick();
    }
}
```

Тогда `GameplayState` становится владельцем активного цикла:

```csharp
public class GameplayState : IState, IGameState, IUpdateableState
{
    public void Enter()
    {
        // Enable gameplay mode.
    }

    public void Tick()
    {
        // Active gameplay loop lives here.
    }

    public void Exit()
    {
        // Stop gameplay mode and cleanup state-owned subscriptions.
    }
}
```

## Transition Safety

Нужно не дать UI вызвать два перехода одновременно.

Минимальный вариант:

```csharp
public bool IsTransitioning { get; private set; }
```

State machine:

```text
if IsTransitioning -> ignore or log transition request
before async transition -> IsTransitioning = true
after transition complete -> IsTransitioning = false
```

UI дополнительно должен блокировать Start/Restart/MainMenu кнопки после клика.

## UI Правило

UI не должен знать `SceneLoader`.

Вместо этого:

```csharp
public interface IGameFlowService
{
    void StartGame();
    void RestartGame();
    void ReturnToMainMenu();
    void PauseGame();
    void ResumeGame();
}
```

UI вызывает flow service:

```csharp
_gameFlow.StartGame();
```

А flow service уже вызывает state machine:

```csharp
_stateMachine.Enter<LoadGameplayState>();
```

Так UI остается тонким: он не знает, какие сцены грузятся и какие states нужны.

## DI Правила

1. `ProjectContext` владеет глобальным flow:

   - `GameStateMachine`;
   - global states;
   - `SceneLoader`;
   - `LoadingCurtain`;
   - `IGameFlowService`;
   - global providers/services, которые нужны states;
   - global services.

2. `SceneContext` владеет локальной композицией сцены:

   - scene references;
   - UI root;
   - level data;
   - scene-scoped factories;
   - scene-scoped services;
   - scene initializers, которые передают ссылки в providers/services.

3. Scene services не должны начинать session сами через `IInitializable`, если
   это меняет игровой режим.

4. Все переходы между major modes идут через global state machine.

5. Если gameplay logic становится большой, сначала группировать ее в private
   methods внутри state или в простые domain services. Не вводить отдельный
   lifecycle owner без явной необходимости.

## Пошаговый План Внедрения

### Шаг 1. Зафиксировать Контракт

Добавить интерфейсы:

- `IUpdateableState`;
- `IGameFlowService`;
- конкретные providers для scene references, если их еще нет.

На этом шаге можно не менять поведение, только создать основу.

### Шаг 2. Разделить Loading И Active States

Переименовать/добавить states:

- `LoadMainMenuState`;
- `MainMenuState`;
- `LoadGameplayState`;
- `GameplayEnterState`;
- `GameplayState`;
- `GameplayPauseState`;
- `GameOverOrParagonState`.

`LoadGameplayState` больше не считается владельцем gameplay. Он только грузит
сцену.

### Шаг 3. Добавить Tick В GameStateMachine

Сделать `GameStateMachine : ITickable`.

Тикать только активный state, если он реализует `IUpdateableState`.

Это главный шаг, который возвращает внешний control loop.

### Шаг 4. Подключить Scene Initializers И Providers

В gameplay scene добавить initializers, которые читают serialized references и
передают их в конкретные providers/services.

Пример:

```text
GameplaySceneInitializer
-> LevelDataProvider.SetStartPoint(...)
-> CameraProvider.SetMainCamera(...)
-> GameplayUiProvider.SetRoot(...)
```

`LoadGameplayState` после загрузки сцены должен быть уверен, что scene
initializers уже отработали.

### Шаг 5. Реализовать GameplayEnterState

`GameplayEnterState` использует:

- concrete scene providers;
- factories;
- services;
- configs;
- save/start data.

И сам подготавливает игровую сессию.

После подготовки:

```text
stateMachine.Enter<GameplayState>()
```

### Шаг 6. Реализовать GameplayState Tick

`GameplayState` реализует `IUpdateableState`.

В `Tick` временно можно оставить заглушку, но все активные gameplay updates,
которые относятся к flow режима, должны постепенно приходить сюда или
вызываться отсюда.

### Шаг 7. Перенести Start Game UI На Flow Service

`MainMenu.StartGame()` больше не вызывает state machine напрямую.

Вместо этого:

```text
MainMenu.StartGame()
-> IGameFlowService.StartGame()
-> stateMachine.Enter<LoadGameplayState>()
```

### Шаг 8. Перенести Restart/MainMenu/GameOver UI На Flow Service

Запретить прямые вызовы:

```text
SceneLoader.ReloadScene()
SceneLoader.LoadScene(MainMenu)
```

Вместо этого:

```text
Restart -> IGameFlowService.RestartGame()
MainMenu -> IGameFlowService.ReturnToMainMenu()
```

### Шаг 9. Добавить Safe Exit

Минимум:

- `GameplayState.Exit()` останавливает state-owned gameplay processes;
- `GameOverOrParagonState.Enter()` сохраняет результат и показывает UI;
- scene-specific providers очищаются при unload сцены, если они держат ссылки на
  объекты сцены.

Позже можно добавить end-of-frame transition, если появятся гонки в кадре.

### Шаг 10. Убрать async void Из States

Цель:

```csharp
UniTask EnterAsync();
UniTask ExitAsync();
```

Можно делать не сразу. Но для scene loading и transition safety это важный шаг.

### Шаг 11. Добавить Защиту От Запуска Не Той Сцены

Добавить editor helper:

```text
if ProjectContext not ready and active scene is not Bootstrap
-> load Bootstrap
```

Это защитит DI/lifecycle от случайного запуска `MainMenu` или `Gameplay`.

## Минимальная Первая Версия

Чтобы не переписать все сразу, минимальный полезный вертикальный срез:

```text
IUpdateableState
GameStateMachine : ITickable
IGameFlowService
scene initializers/providers for scene references
LoadGameplayState waits until scene references are available
GameplayEnterState prepares session directly
GameplayState owns Tick directly
MainMenu uses IGameFlowService.StartGame
```

Это уже фиксирует главное: внешний state machine снова владеет циклом, без
добавления промежуточного владельца игрового режима.

## Проверочный Список

Перед тем как считать перенос парадигмы успешным, должно быть верно:

- Нет major-mode переходов напрямую через `SceneLoader` из UI.
- `Gameplay` не стартует сам только потому, что сцена загрузилась.
- Нет дополнительной сущности, которая владеет игровым циклом вместо state.
- `GameplayEnterState` сам подготавливает сессию.
- Активный gameplay tick принадлежит `GameplayState`.
- Pause/game over/restart/menu проходят через state machine.
- Cleanup gameplay session вызывается до входа в следующий major mode.
- Scene installers только собирают зависимости, но не владеют flow.

## Главное Правило

Если коротко, вся миграция держится на одном вопросе:

```text
Кто имеет право сказать "игра началась", "игра тикает", "игра закончилась"?
```

Ответ должен быть:

```text
External game state machine through states.
```

DI помогает создать объекты. Scene дает ссылки. UI сообщает о намерениях.
Но жизненным циклом режима владеют states.
