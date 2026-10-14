# Block Blast - Core Architecture Guidelines

---

## 1. Directory Structure (Core vs Features)

* **`Core/`**: Chỉ reusable patterns/services dùng được ở project khác không cần sửa (`ServiceLocator`, `StateMachine<T>`, `ObjectPool`, `AudioSystem`, `IPoolService`); CẤM logic Block Blast.
* **`Features/`**: Mỗi game feature nằm trong folder độc lập. Interface riêng như `IGridService` phải ở feature tương ứng, không đưa vào `Core`.

---

## 2. Synchronous Communication (Service Locator)

Giao tiếp đồng bộ request-response giữa system tách rời dùng Service Locator qua interface:
  ```csharp
  bool isValid = ServiceLocator.Get<IGridService>().CanPlaceBlock(shape, pos);
  ```

---

## 3. MVC Pattern Guidelines

#### A. Model (Pure Data & Internal State)
* Sở hữu domain state và mọi phép tính chỉ dùng state nội bộ: derived properties/bounds, matrix queries, line-clear/exposed-edge queries, combo và tray state.
* Chỉ dùng data/math types (`Vector2Int`, `Vector3`, `Color`, primitives).
* CẤM giữ Scene component (`GameObject`, `Transform`, `MonoBehaviour`, `Renderer`), phát event, gọi `ServiceLocator` hoặc truy cập Model/Service khác.

#### B. Controller (Orchestrator & Bridge)
Điều phối state/flow ngoài một Model: đổi grid↔world coordinates; nối hậu quả liên-system; xử lý game rules/game-over/state transitions; chuẩn bị positions, intersections, centers, edges, points và phát Pure C# events cho View.

#### C. View (Passive Presentation Specialist)
View chỉ render, forward input và animate/DOTween. Được phép presentation math nhẹ (clamp duration, tilt, Slerp/Lerp, shake), format UI và cache visual state. CẤM search/iterate domain matrix, tính perimeter/intersections/points, đổi game rule hoặc Model. Controller phải cung cấp coordinates, world positions, points và structured edge data.

---

## 4. File Size Limits & Decomposition (> 600 Lines)

Mọi C# file phải `≤600` lines; file vượt ngưỡng phải tách nhưng CẤM refactor/decompose khi chưa có approved plan.

### B. Decomposition Strategies ("Hub and Satellites")
1. **Model**: `partial` files `Core` (state/constructor/accessors), `Queries` (search/lookup/validation), `Operations` (mutation/calculation); vẫn là một model instance.
2. **Controller**: thin controller + pure C# handlers (`GridPlacementHandler`, `GridClearHandler`); handler không kế thừa `MonoBehaviour`, được tạo bằng `new` trong `Awake()` và test được bằng EditMode.
3. **View**: View hub dispatch sang `MonoBehaviour` sub-views theo visual domain (`GridShadowView`, `GridPreClearAnimator`).

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
