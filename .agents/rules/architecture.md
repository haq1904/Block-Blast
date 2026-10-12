---
trigger: always_on
description: Core architectural guidelines, directory structure, MVC, and service locator patterns.
---

# Block Blast - Core Architecture Guidelines

AI assistants working on "Block Blast" MUST strictly adhere to the following architectural rules.

---

## 1. Directory Structure (Core vs Features)

Strictly separate generic reusable code (`Core`) from game-specific code (`Features`):
* **`Core/`**: Reusable modules dropped into any project without modification.
  * Design Patterns: `ServiceLocator`, `StateMachine<T>`, `ObjectPool`.
  * Generic Services: `AudioSystem`, `IPoolService`.
  * **Prohibited**: Absolutely NO Block Blast game-specific logic or rules here.
* **`Features/`**: Game mechanics and features in independent folders (e.g., `Features/GridSystem`, `Features/TraySystem`).
  * Custom interfaces (e.g., `IGridService`) MUST reside inside their feature folder, NOT in `Core`, ensuring clean removal with zero dangling dependencies.

---

## 2. Synchronous Communication (Service Locator)

Use the **Service Locator** pattern for synchronous, immediate request-response communication between decoupled systems (e.g., Block asks Grid if placement is valid, Tray requests blocks from Pool).
* **Rule**: Always communicate via Interfaces:
  ```csharp
  bool isValid = ServiceLocator.Get<IGridService>().CanPlaceBlock(shape, pos);
  ```

---

## 3. MVC Pattern Guidelines

### Division of Responsibilities

#### A. Model (Pure Data & Internal State)
* **Definition**: Self-contained domain state; isolated from outside systems.
* **Permitted Calculations**: Any math requiring only the Model's internal fields MUST be computed by the Model itself:
  * Derived properties: `BlockModel.CenterOffset` and bounding boxes from `shapeOffsets`.
  * Domain matrix queries: `GridModel.IsRowFull(row)`, `IsOccupied(col, row)`, `GetPotentialLineClears(...)`, `GetExposedEdges(...)`.
  * State manipulation: `ScoreModel` combo counts; `SpawnModel` tray slot tracking.
  * Allowed Types: Pure data/math types (`Vector2Int`, `Vector3`, `Color`, primitive structs).
* **Strict Prohibitions**:
  * MUST NOT store or reference Scene components (`GameObject`, `Transform`, `MonoBehaviour`, `Renderer`).
  * MUST NOT broadcast any events (strictly adheres to `events.md`).
  * MUST NOT call `ServiceLocator` or access other Models/Services.

#### B. Controller (Orchestrator & Bridge)
* **Definition**: Manages macro gameplay state and coordinate flow outside a single Model.
* **Core Responsibilities**:
  1. **Coordinate Transformation**: Converts discrete grid space `(col, row)` to 3D world space `Vector3(x, -1, z)` and `Quaternion`.
  2. **Inter-System Bridging**: Resolves multi-system consequences (e.g., Grid line clear $\rightarrow$ awards points in `ScoreController`, plays audio via `AudioSystem`, checks moves in `SpawnService`).
  3. **Game Rules & State**: Evaluates game-over conditions; drives State Machine transitions (`GameplayState` $\rightarrow$ `GameOverState`).
  4. **Presentation Data Prep**: Pre-packages geometric layout data (intersections, centers, exposed edges) and dispatches pure C# events to Views.

#### C. View (Passive Presentation Specialist)
* **Definition**: Passive consumer responsible strictly for visual rendering, input forwarding, animations, and DOTween.
* **Permitted Presentation Math (Lightweight)**:
  * Tween dynamics: Duration clamping (`Mathf.Clamp(points * 0.04f, 0.2f, 0.65f)`), drag-to-tilt angles (`Quaternion.Euler(pitch, 0, roll)`), Slerp/Lerp, shake amplitude.
  * UI formatting: Score formatting (`score.ToString("#,##0")`) and combo text.
  * Visual state caching: Display list comparison (`AreListsEqual`) to prevent redundant tween restarts, material backup for hover.
* **Strict Prohibitions (Humble View)**:
  * MUST NOT execute algorithmic search or iterate matrices (e.g., searching line intersections across 8x8 grid, calculating outer perimeter edges).
  * MUST NOT alter game rules, compute points, or mutate Model state.
  * All coordinates, world positions, combo points, and structured edge data MUST be delivered by the Controller.

---

## 4. File Size Limits & Decomposition (> 600 Lines)

### A. The 600-Line Limit & Approval Gate
* **Threshold Rule**: Any single C# file MUST NOT exceed **600 lines of code**. Bloated files MUST be decomposed.
* **Mandatory Approval Gate**: You MUST NOT refactor or decompose files without an approved implementation plan (`plans/<task_name>_plan.md`).

### B. Decomposition Strategies ("Hub and Satellites")
1. **Decomposing a Bloated Model (`partial class`)**:
   * Split across multiple physical files using `partial`:
     * `FeatureModel.Core.cs`: State fields, constructor, basic accessors.
     * `FeatureModel.Queries.cs`: Search algorithms, matrix lookups, validation.
     * `FeatureModel.Operations.cs`: State mutations, domain calculations.
   * Remains a single unified `model.` instance to external consumers.
2. **Decomposing a Bloated Controller (Thin Controller + Pure C# Handlers)**:
   * Extract business workflows into **Pure C# Handlers** (e.g., `GridPlacementHandler`, `GridClearHandler`).
   * Handlers MUST NOT inherit `MonoBehaviour`; Controller instantiates them via `new` in `Awake()`. Zero lifecycle overhead, instant EditMode unit testability.
3. **Decomposing a Bloated View (View Hub + MonoBehaviour Sub-Views)**:
   * Extract visual domains into dedicated `MonoBehaviour` **Sub-Views** attached to GameObjects (e.g., `GridShadowView`, `GridPreClearAnimator`).
   * The main View acts as a dispatcher receiving Controller events and delegating to Sub-Views.

---

## 5. Generic State Machine
Design State Machines using Generics `<T>`:
* Use `StateMachine<T>` and `State<T>`. Controller passes its own type as `T`.
* Inside concrete States, the strongly typed `core` variable calls methods directly on the Controller without Singletons or `FindObjectOfType`.

---

## 6. Respect Interfaces
* Abstract all Services via Interfaces (`IPoolService`).
* Use standard C# implicit interface implementation. Do NOT use `override` unless overriding base virtual/abstract methods.

---

## 7. IDE and Configuration
* Do not commit generated Unity folders (`Library`, `Logs`). Hide visual clutter (`.meta`, `.csproj`, `.asmdef`) via `files.exclude`.

---

## 8. Visual Feedback & Strategy Pattern
* All visual feedback, cell morphing, wave stagger, and prop control rules follow [visual_effects.md](visual_effects.md).