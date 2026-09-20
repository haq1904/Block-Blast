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

### Core Architectural Mantra
*   **Model**: Computes pure data strictly within its own boundary ("Within its own house").
*   **Controller**: Orchestrates communication across systems, converts coordinates, and prepares structured data for Views ("Connecting houses and preparing food for the table").
*   **View**: Computes presentation math to make visual motion, juice, and animation feel smooth and beautiful ("Setting the table and presenting the meal").

---

### Detailed Division of Responsibilities

#### A. Model ("Internal Specialist")
*   **Definition**: Owns its domain data and internal state. It is self-contained and isolated from the outside world.
*   **Rule of Thumb**: Any mathematical calculation that only requires the Model's own internal fields MUST be computed by the Model itself.
*   **Permitted Calculations**:
    *   *Self-contained derived properties*: `BlockModel` computes `CenterOffset` and bounding boxes from its `shapeOffsets`.
    *   *Domain matrix queries*: `GridModel` queries its 8x8 matrix to check `IsRowFull(row)`, `IsOccupied(col, row)`, `GetPotentialLineClears(...)`, or `GetExposedEdges(...)`.
    *   *State manipulation*: `ScoreModel` updates combo counts and streak records; `SpawnModel` tracks active tray slot states.
    *   *Allowed Structs*: Uses pure data/math types (`Vector2Int`, `Vector3`, `Color`, primitive structs).
*   **Strict Prohibitions**:
    *   MUST NOT store or reference Scene components (`GameObject`, `Transform`, `MonoBehaviour`, `Renderer`, etc.).
    *   MUST NOT broadcast any events (strictly adheres to the `events.md` Model Prohibition Rule).
    *   MUST NOT call `ServiceLocator` or access other Models/Services.

#### B. Controller ("Conductor / Orchestrator")
*   **Definition**: Sits above individual Models to observe the macro gameplay state. It handles everything that falls **outside the scope of a single Model**.
*   **Core Responsibilities**:
    1.  **Space & Coordinate Transformation**:
        *   `GridModel` only knows discrete integer grid space `(col, row)`.
        *   `GridController` knows 3D world space, camera placement, and board dimensions. It converts discrete coordinates `(col, row)` into world space `Vector3(x, -1, z)` and orientation `Quaternion`.
    2.  **Inter-System Communication (Bridging)**:
        *   When `GridModel` clears 2 lines, `GridController` resolves this with external systems: it asks `ScoreController` to award combo points, triggers `AudioSystem` for escalating sound pitches, and checks `SpawnService` for valid placement moves.
    3.  **Game Rules & State Transitions**:
        *   Evaluates game-over conditions when no valid placements remain.
        *   Drives State Machine transitions (e.g., `GameplayState` -> `GameOverState`).
    4.  **Presentation Data Preparation**:
        *   Computes and pre-packages geometric layout data (e.g., combo intersection points, shape center vectors, exposed edge lists) so Views do not have to compute them.
        *   Broadcasts pure C# events to passive Views.

#### C. View ("Humble / Presentation Specialist")
*   **Definition**: A passive consumer responsible strictly for visual rendering, user input forwarding, animations, and DOTween sequences.
*   **Permitted Presentation Math (Lightweight)**:
    *   *Motion & Tween dynamics*: Calculating duration based on gained points (`Mathf.Clamp(points * 0.04f, 0.2f, 0.65f)`), converting drag velocity to 3D tilt angles (`Quaternion.Euler(pitch, 0, roll)`), Slerp/Lerp smoothing, shake amplitude.
    *   *String & UI formatting*: Formatting score numbers (`score.ToString("#,##0")`) and combo text (`$"COMBO x{combo}"`).
    *   *Visual state caching*: Comparing display lists (`AreListsEqual`) to prevent redundant tween restarts, toggling container GameObjects, backing up materials for hover highlights.
*   **Strict Prohibitions (Humble View Violations)**:
    *   MUST NOT execute heavy algorithmic computations or iterate over game data matrices (e.g., searching line intersections across an 8x8 grid, calculating exposed outer perimeter edges, or averaging multi-cell bounding box centers).
    *   MUST NOT alter game rules, compute gameplay points, or directly update Model state.
*   **Passive Consumer Principle**:
    *   All calculated coordinates, world positions, combo points, and structured edge data must be delivered to the View by the Controller.
    *   The View simply consumes this pre-calculated data to invoke visual feedback (`Play`, `Apply`, `Tween`).

## 4. File Size Limits & Decomposition Strategy (> 600 Lines)

### A. The 600-Line Limit & Approval Gate
*   **Threshold Rule**: Any single C# file MUST NOT exceed **600 lines of code**. If a file exceeds or is anticipated to exceed 600 lines due to feature expansion, it is considered bloated and MUST be decomposed.
*   **Mandatory User Approval Gate**: You MUST NOT refactor or decompose bloated files into sub-files/sub-classes without first creating an implementation plan (`implementation_plan.md`) and obtaining explicit user approval.

### B. Decomposition Strategies by MVC Layer
When decomposing a bloated component, strictly follow the "Hub and Satellites" pattern without altering external APIs:

1.  **Decomposing a Bloated Model (`partial class`)**:
    *   Split the single class across multiple physical `.cs` files using C#'s `partial` keyword.
    *   Group by functionality:
        *   `FeatureModel.Core.cs`: State fields, constructor, basic accessors/bounds.
        *   `FeatureModel.Queries.cs`: Search algorithms, matrix lookups, validation.
        *   `FeatureModel.Operations.cs`: State mutations, domain calculations.
    *   **Rule**: To external consumers (Controller), it remains a single unified `model.` instance. No sub-instances or wrapper classes are created.

2.  **Decomposing a Bloated Controller (Thin Controller + Pure C# Handlers)**:
    *   Extract business logic and subsystem workflows into focused **Handlers / Sub-Controllers** (e.g., `GridPlacementHandler`, `GridClearHandler`).
    *   **Strict Rule**: Handlers MUST be **Pure C# classes** (do NOT inherit `MonoBehaviour` and do NOT attach to GameObjects). The main Controller simply instantiates them via `new` in `Awake()`.
    *   **Benefits**: Keeps the Inspector completely clean, consumes zero Unity lifecycle overhead, and allows instantaneous EditMode Unit Testing.

3.  **Decomposing a Bloated View (View Hub + MonoBehaviour Sub-Views)**:
    *   Extract distinct visual domains into dedicated **Sub-Views** (e.g., `GridShadowView`, `GridPreClearAnimator`, `GridPlacementVFXDriver`).
    *   **Strict Rule**: Sub-Views MUST be `MonoBehaviour` components attached to the GameObject or its child GameObjects to allow Inspector serialized field wiring, DOTween management, and visual toggling.
    *   **View Hub**: The main View acts as a central dispatcher/coordinator that receives events from the Controller and delegates work directly to the relevant Sub-Views.

## 5. Generic State Machine
The State Machine must be designed using Generics `<T>` to ensure flexibility and prevent tight coupling to any specific class.

*   Use `StateMachine<T>` and `State<T>`.
*   Each Feature Controller (e.g., `GameFlowController`) will pass its own type as the `T` parameter.
*   Inside concrete States (e.g., `MainMenuState`), use the `core` variable (which is strongly typed to `T`) to call methods back on the controller without needing singletons or `FindObjectOfType`.

## 6. Respect Interfaces
*   All Services must be abstracted via Interfaces (e.g., `ObjectPoolingManager` implements `IPoolService`).
*   When implementing an Interface, use standard C# implicit implementation syntax. Do NOT use the `override` keyword unless overriding a base class virtual/abstract method.

## 7. IDE and Configuration
*   Do not commit/push generated Unity folders (`Library`, `Logs`) or metadata files unless necessary. The IDE is configured via `files.exclude` to hide visual clutter like `.meta`, `.csproj`, and `.asmdef` files to keep the workspace clean.