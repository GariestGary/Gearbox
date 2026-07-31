# Repository Guidelines

## Project Structure & Module Organization

Gearbox is a Unity Package Manager package (`com.volumebox.gearbox`) targeting Unity 2021.3+. Runtime state-machine code lives in `Core/`; keep it independent of editor-only APIs. Custom inspectors and preferences belong in `Editor/`. `Examples/` contains sample states and manual-use components, while `Tests/` contains Editor test assemblies. Each module has its own `.asmdef`. Preserve Unity `.meta` files and commit them alongside every added or moved asset.

## Build, Test, and Development Commands

This repository is not a standalone Unity project, so there is no local `make` or `dotnet build` entry point. Add the package to a host project's `Packages/manifest.json` or embed it under `Packages/com.volumebox.gearbox`, then use that project's Unity version.

- Open **Window > General > Test Runner** and run EditMode tests for interactive verification.
- Run tests in CI with `Unity -batchmode -quit -projectPath <host-project> -runTests -testPlatform EditMode -testResults results.xml`.
- Inspect `package.json` after dependency or release changes; increment its semantic version when publishing a package release.

Before submitting, confirm that Unity imports all three assemblies without console errors and that sample components still serialize correctly.

## Coding Style & Naming Conventions

Use C# with four-space indentation and braces on their own lines. Follow existing conventions: `PascalCase` for types, methods, properties, and public members; `_camelCase` for private fields; and `camelCase` for parameters and locals. Keep namespaces aligned with assembly roots, such as `VolumeBox.Gearbox.Core`. Prefer explicit Unity lifecycle methods and `UniTask` for asynchronous state transitions. Add XML documentation to public APIs whose behavior is not immediately obvious. No formatter is configured, so match adjacent code and remove trailing whitespace.

## Testing Guidelines

Tests use NUnit plus Unity Test Framework attributes. Put tests in `Tests/`, name files after the subject (for example, `StateMachineTests.cs`), and use descriptive methods such as `StateMachine_TransitionToStateByName`. Use `[Test]` for synchronous behavior and `[UnityTest]` with `ToCoroutine()` for `UniTask` flows. Add regression coverage for transition, initialization, serialization, and lifecycle changes; no numeric coverage threshold is currently enforced.

## Commit & Pull Request Guidelines

History favors short, lowercase, imperative summaries such as `fixed error logging` and `inspector adjustments`. Keep each commit focused and mention the affected behavior. Pull requests should explain the motivation, summarize runtime/editor impact, list test results and Unity version, and link relevant issues. Include screenshots or a short capture for inspector or preferences-window changes. Never include generated `Library/`, `Temp/`, or IDE files.
