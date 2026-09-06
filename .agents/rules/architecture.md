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