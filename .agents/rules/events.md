---
trigger: always_on
description: Asynchronous communication standards, Pure C# Events vs ScriptableObject Game Events, and Decision Matrix.
---

# Block Blast - Event Communication Guidelines

The system adopts a dual-tier event communication model to maintain maximum performance, type safety, and seamless automated testing while enabling flexible decoupling:

## Tier 1: Pure C# Events (`event Action`) [Default Project Standard]
Mandatory whenever ANY of the following conditions are met:
1. **Payload Contains Data**: Any event carrying payloads (structs, classes, collections, placement lists, board coordinates, model references).
2. **High-Frequency & Streaming**: Events fired repeatedly during gameplay (every frame, continuous touch/drag gestures, number counting, preview updates).
3. **Inter-System Data Exchange**: Communication between systems via Service Locator interfaces (`IGridService`, `IScoreService`, `ISpawnService`).
4. **Intra-Feature MVC Pipeline**: Direct communication from `Controller -> View` within a feature.
5. **Core Logic & Automated Unit Testing**: Any event involved in game algorithms, grid placement, score calculation, or spawning that is subject to EditMode Unit Testing. Pure C# events execute in-memory with zero GC allocations and no Unity Editor dependencies.

## Tier 2: ScriptableObject Game Events (`GameEvent SO` / `BaseGameEventGeneric<T>`) [Requires Prior Approval]
Exclusively reserved for and permitted only under the following scenarios:
1. **Game Flow State Transitions**: High-level macro milestones of the game lifecycle (`OnGameStart`, `OnGameOver`, `OnGamePause`, `OnGameResume`, `OnRevive`).
2. **Signal-Only Events**: Fire-and-forget notifications signaling "Event X happened" without requiring complex data payloads.
3. **Cross-System Game Juice (Plug & Play)**: One-to-many broadcast triggers where multiple independent peripheral systems respond (Camera Shake via `CinemachineImpulseSource`, Haptic Feedback, Particle VFX, celebratory Audio) without modifying existing Controller code.
4. **Cross-Scene Decoupling**: Communication between distinct scenes (e.g., `GamePlayScene` notifying `BootstrapScene` to transition BGM or change Audio Mixer state) without Singletons or `FindObjectOfType`.
5. **No-Code Designer Authoring**: Empowering designers or artists to hook up audio/VFX responses directly via Inspector `UnityEvent` bindings without C# modifications.

## Mandatory Rules
*   **Mandatory User Approval Rule**: You MUST NOT create, integrate, or convert any feature to use ScriptableObject Game Events without asking the user for explicit approval first. Always present the proposed event and wait for user confirmation before implementing.
*   **Model Prohibition Rule**: The Model MUST NOT broadcast any events. It is a pure C# class solely responsible for storing data and state.

## Event Selection Decision Matrix

| Criterion | Pure C# Event | GameEvent SO |
| :--- | :---: | :---: |
| Carries data payload (struct, class, list)? | **Mandatory** | Avoid |
| High-frequency or continuous streaming? | **Mandatory** | Prohibited |
| Intra-feature communication (`Controller -> View`)? | **Mandatory** | Prohibited |
| Covered by automated Unit Tests (EditMode)? | **Mandatory** | Prohibited |
| Inter-system communication exchanging data? | **Mandatory** | Avoid |
| High-level game state transition (`OnGameOver`, `OnGameStart`)? | Optional | **Recommended** |
| Cross-system Plug & Play Game Juice (VFX, Shake, Audio)? | Limited | **Recommended** |
| Cross-scene communication (`Bootstrap` $\leftrightarrow$ `GamePlay`)? | Service Locator | **Recommended** |
| Requires explicit user approval before use? | No (Default) | **Mandatory** |
