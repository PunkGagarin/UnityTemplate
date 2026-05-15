# Project Agent Guide

## Project Snapshot
- Unity project: `IgnisBearer` / `UnityTemplate`.
- Unity editor version: `6000.4.4f1`.
- Main gameplay code lives under `Assets/_Project/Scripts`.
- Main scenes live under `Assets/_Project/_Scenes`: `Bootstrap`, `MainMenu`, and `Gameplay`.
- The project uses Zenject for dependency injection and UniTask for async workflows.
- Rendering is configured with URP assets in `Assets/_Project/Resources`.

## Repository Rules
- Keep Unity `.meta` files together with their assets and scripts.
- Do not edit generated folders: `Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, `.vs`, or IDE caches.
- Avoid editing generated solution/project files (`*.sln`, `*.csproj`) unless the task explicitly requires it.
- Prefer changes inside `Assets/_Project` for project code. Treat `Assets/Plugins`, `Assets/NaughtyAttributes`, `Assets/TextMesh Pro`, and imported packages as vendor code unless asked otherwise.
- Do not revert unrelated local changes. This repository may contain user or IDE changes in progress.

## Architecture Notes
- Startup flow is driven from `Bootstrap` scene through `BootstrapInstaller` and `GameRunner`.
- Global bindings are configured through `ProjectInstaller` and scene/local bindings through Zenject installers.
- Game state flow lives in `Assets/_Project/Scripts/Infrastructure/GameStates`.
- Scene loading utilities live in `Assets/_Project/Scripts/Infrastructure/SceneManagement`.
- Audio domain/data/view code lives in `Assets/_Project/Scripts/Audio`.
- Localization code lives in `Assets/_Project/Scripts/Localization`.

## C# Style
- Follow the existing namespace style, for example `_Project.Scripts.Infrastructure`.
- Prefer explicit, small classes with clear responsibilities.
- Use Zenject constructor or field injection consistently with the surrounding code.
- Use UniTask for Unity async code when adding async flows.
- Keep Unity lifecycle methods (`Awake`, `Start`, `Update`, etc.) focused and delegate domain logic to services where practical.

## Validation
- When possible, validate Unity changes by opening the project in Unity `6000.4.4f1`.
- For code-only changes, at minimum check affected C# files for compile-time issues and keep scene/prefab references in sync.
- If adding or moving Unity assets, ensure corresponding `.meta` files are present.

## Git Notes
- Git may report `dubious ownership` in sandboxed environments. Use a per-command safe directory override when inspecting status:
  `git -c safe.directory=F:/unity_personal/UnityTemplate status --short --branch`
- Do not create commits, branches, or stage files unless the user asks for that explicitly.
