---
trigger: always_on
description: Architectural guidelines and coding standards for the Block Blast project.
---

# Block Blast - Architectural Guidelines

You are an AI assistant working on the "Block Blast" project. When writing code or designing systems for this project, you MUST strictly adhere to the following architectural rules.

## 1. Directory Structure (Core vs Features)

Maintain a strict separation between generic/reusable code (`Core`) and game-specific code (`Features`).

*   **`Core/`**: Contains invaluable, highly reusable assets that can be dropped into any other game (e.g., Racing, Shooter) without modifications.
    *   Contains Design Patterns: `ServiceLocator`, `StateMachine<T>`, `ObjectPool`.
    *   Contains generic Services: `AudioSystem`, `IPoolService`.
    *   **Prohibited:** Do NOT place any Block Blast game-specific logic or rules in this directory.
*   **`Features/`**: Contains the specific mechanics and features of this project.
    *   Each feature must be in its own independent folder (e.g., `Features/GridSystem`, `Features/TraySystem`).
    *   If a feature requires a custom Interface (e.g., `IGridService`), that Interface MUST reside within the feature's folder, NOT in `Core`. This ensures that deleting the feature cleanly removes all its dependencies.

## 2. System Communication

The system utilizes a combination of the **Service Locator** and **Event-Driven** patterns to achieve loose coupling.

### 2.1. Synchronous Communication (Immediate Request-Response)
*   Use the **Service Locator** pattern.
*   Apply this when Controller A needs to request data or invoke an action from Controller B synchronously (e.g., Block asks Grid if placement is valid, Tray requests 3 blocks from the Pool).
*   **Rule:** Always communicate via Interfaces. 
    `bool isValid = ServiceLocator.Get<IGridService>().CanPlaceBlock(shape, pos);`

### 2.2. Asynchronous Communication (One-Way Notification)
*   Use **Game Events** (Broadcast paradigm).
*   Apply this when a state change occurs and multiple unrelated systems need to react (e.g., Game Start, Score updated, Block successfully placed).
*   **Rule:** The emitter (Controller) MUST NOT know who is listening. It simply broadcasts the event (`OnBlockPlaced.Raise()`), and interested systems subscribe and react independently.
*   **Rule:** The Modle must not broadcasts the event. It's just pure c# class to store data.


## 3. Generic State Machine

The State Machine must be designed using Generics `<T>` to ensure flexibility and prevent tight coupling to any specific class.

*   Use `StateMachine<T>` and `State<T>`.
*   Each Feature Controller (e.g., `GameFlowController`) will pass its own type as the `T` parameter.
*   Inside concrete States (e.g., `MainMenuState`), use the `core` variable (which is strongly typed to `T`) to call methods back on the controller without needing singletons or `FindObjectOfType`.

## 4. Respect Interfaces
*   All Services must be abstracted via Interfaces (e.g., `ObjectPoolingManager` inherits `IPoolService`).
*   When implementing an Interface, use standard C# implicit implementation syntax. Do NOT use the `override` keyword unless overriding a base class virtual/abstract method.

## 5. IDE and Configuration
*   Do not commit/push generated Unity folders (`Library`, `Logs`) or metadata files unless necessary. The IDE is configured via `files.exclude` to hide visual clutter like `.meta`, `.csproj`, and `.asmdef` files to keep the workspace clean.

## 6. Token Efficiency & Communication (CRITICAL)
*   **Brevity First:** Answer directly and straight to the point. Eliminate conversational filler, pleasantries, and unnecessary characters.
*   **Token Saving:** Treat tokens as a strictly limited resource. Provide exactly what is needed to solve the task, nothing more.
*   **Efficient Code Edits:** Never dump full file contents into the chat unless explicitly requested. Use precise tool calls to modify code quietly and summarize actions briefly.

## 7. MVC Pattern Guidelines
*   **Model**: The Model's core responsibility is to store data and state. It is perfectly acceptable and encouraged to use `UnityEngine` to store math/data structs like `Vector2Int`, `Vector3`, `Color`, etc. However, a Model MUST NOT store references to Scene components like `GameObject`, `Transform`, `MonoBehaviour`, etc.
*   **View**: Handles rendering and user input.
*   **Controller**: Handles game logic, processes inputs from the View, updates the Model, and broadcasts events.

## 8. Code Style & Documentation
*   **English Only**: All code comments, `[Tooltip]` attributes, debug logs, and variable/method names MUST be written entirely in English.

## 9. Planning Language
*   **Vietnamese for Implementation Plans**: Whenever creating or updating an implementation plan (`implementation_plan.md`), ALWAYS write the plan in Vietnamese. Technical terms, file names, code snippets, and identifiers must remain in English.

## 10. Strict DOTween Management Guidelines
DOTween is widely used for Game Juice, animations, and transitions. To prevent memory leaks, `MissingReferenceException`, stale callbacks, and transform desynchronization, you MUST adhere to the following rules:

*   **Kill Before Tweening**: Always kill any active tween on a target before starting a new one (`target.DOKill()` or `tweenInstance?.Kill()`).
*   **Mandatory Cleanup (`OnDisable` / `OnDestroy`)**: Any component launching tweens MUST kill them in `OnDisable()` or `OnDestroy()`. Always bind tweens to their GameObject lifecycle using `.SetLink(gameObject, LinkBehaviour.KillOnDisable)` or `.SetLink(gameObject, LinkBehaviour.KillOnDestroy)`.
*   **Object Pooling Safety**: When returning a GameObject or Transform to the Object Pool (`IPoolService.ReturnObjectToPool`), you MUST:
    1. Kill all active tweens on that object (`target.DOKill()`).
    2. Reset all modified properties (scale, position, rotation, alpha) back to their canonical default values (`Vector3.one`, default position, `Quaternion.identity`).
    3. Never leave an active tween running on a disabled/pooled object.
*   **Stateless ScriptableObjects**: ScriptableObjects (e.g., `PreClearEffectSO`) must NEVER store active `Tween` references, `Transform` references, or runtime state dictionaries in instance fields. ScriptableObjects must remain purely stateless configurators/executors.
*   **Infinite Loops Tracking**: Any tween configured with infinite loops (`.SetLoops(-1)`) MUST be tracked and explicitly killed (`target.DOKill()`) immediately when its trigger state ends (e.g. canceling pre-clear preview).
*   **No Redundant Allocations in Update**: Avoid instantiating new tweens inside `Update()` without checking `DOTween.IsTweening(target)` or caching the active `Tween` reference.