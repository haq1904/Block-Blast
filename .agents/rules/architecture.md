---
trigger: always_on
description: Core architectural guidelines, directory structure, MVC, and service locator patterns.
---

# Block Blast - Core Architecture Guidelines

You are an AI assistant working on the "Block Blast" project. When writing code or designing systems for this project, you MUST strictly adhere to the following architectural rules.

## 1. Directory Structure (Core vs Features)
Maintain a strict separation between generic/reusable code (`Core`) and game-specific code (`Features`).

*   **`Core/`**: Contains invaluable, highly reusable assets that can be dropped into any other game (e.g., Racing, Shooter) without modifications.
    *   Contains Design Patterns: `ServiceLocator`, `StateMachine<T>`, `ObjectPool`.
    *   Contains generic Services: `AudioSystem`, `IPoolService`.
    *   **Prohibited**: Do NOT place any Block Blast game-specific logic or rules in this directory.
*   **`Features/`**: Contains the specific mechanics and features of this project.
    *   Each feature must be in its own independent folder (e.g., `Features/GridSystem`, `Features/TraySystem`).
    *   If a feature requires a custom Interface (e.g., `IGridService`), that Interface MUST reside within the feature's folder, NOT in `Core`. This ensures that deleting the feature cleanly removes all its dependencies.

## 2. Synchronous Communication (Service Locator)
*   Use the **Service Locator** pattern for synchronous, immediate request-response communication between decoupled systems.
*   Apply this when Controller A needs to query data or invoke an action from Controller B synchronously (e.g., Block asks Grid if placement is valid, Tray requests 3 blocks from the Pool).
*   **Rule**: Always communicate via Interfaces:
    ```csharp
    bool isValid = ServiceLocator.Get<IGridService>().CanPlaceBlock(shape, pos);
    ```

## 3. MVC Pattern Guidelines
*   **Model**: Stores pure data and state. It is permitted and encouraged to use `UnityEngine` math/data structs like `Vector2Int`, `Vector3`, `Color`, etc. However, a Model MUST NOT store references to Scene components like `GameObject`, `Transform`, `MonoBehaviour`, etc.
*   **View**: Handles visual rendering, user input, animations, and DOTween effects.
*   **Controller**: Coordinates business logic, processes input from the View, updates the Model, and broadcasts events.

## 4. Generic State Machine
The State Machine must be designed using Generics `<T>` to ensure flexibility and prevent tight coupling to any specific class.

*   Use `StateMachine<T>` and `State<T>`.
*   Each Feature Controller (e.g., `GameFlowController`) will pass its own type as the `T` parameter.
*   Inside concrete States (e.g., `MainMenuState`), use the `core` variable (which is strongly typed to `T`) to call methods back on the controller without needing singletons or `FindObjectOfType`.

## 5. Respect Interfaces
*   All Services must be abstracted via Interfaces (e.g., `ObjectPoolingManager` implements `IPoolService`).
*   When implementing an Interface, use standard C# implicit implementation syntax. Do NOT use the `override` keyword unless overriding a base class virtual/abstract method.

## 6. IDE and Configuration
*   Do not commit/push generated Unity folders (`Library`, `Logs`) or metadata files unless necessary. The IDE is configured via `files.exclude` to hide visual clutter like `.meta`, `.csproj`, and `.asmdef` files to keep the workspace clean.