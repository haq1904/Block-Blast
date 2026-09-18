---
trigger: always_on
description: Development workflow standards, token efficiency, code style, unit testing policy, and commit message conventions.
---

# Block Blast - Workflow & Coding Standards

## 1. Token Efficiency & Communication (CRITICAL)
*   **Brevity First**: Answer directly and straight to the point. Eliminate conversational filler, pleasantries, and unnecessary characters.
*   **Token Saving**: Treat tokens as a strictly limited resource. Provide exactly what is needed to solve the task, nothing more.
*   **Efficient Code Edits**: Never dump full file contents into the chat unless explicitly requested. Use precise tool calls to modify code quietly and summarize actions briefly.

## 2. Code Style & Documentation
*   **English Only**: All code comments, `[Tooltip]` attributes, debug logs, and variable/method names MUST be written entirely in English.

## 3. Planning Language
*   **Vietnamese for Implementation Plans**: Whenever creating or updating an implementation plan (`implementation_plan.md`), ALWAYS write the plan in Vietnamese. Technical terms, file names, code snippets, and identifiers must remain in English.

## 4. Unit Testing Policy
*   **Run Only on Model/Controller Changes**: Automated unit tests (`unityMCP:run_tests` or NUnit EditMode tests) MUST ONLY be executed when changes involve core business logic, mathematical algorithms, data models, or controllers (e.g., `GridModel`, `GridController`, `BlockSpawnGenerator`).
*   **Skip for Pure View / Visual Changes**: When changes are strictly confined to the View layer, visual Game Juice, shaders, materials, audio, animations, or DOTween effects (e.g., `GridView`, `ScoreView`, `PreClearEffectSO` subclasses), do NOT run unit tests. Verify View changes through compiler diagnostic checks (`read_console`) and manual visual testing in PlayMode to maximize development speed and eliminate unnecessary test overhead.

## 5. Commit Message Conventions
All Git commit messages MUST strictly adhere to the **Conventional Commits** specification:

*   **Format**: `<type>(<scope>): <short description in imperative mood>`
*   **Allowed Types**:
    *   `feat`: A new feature, mechanic, visual juice, or player-facing capability.
    *   `fix`: A bug fix or regression repair.
    *   `refactor`: Code restructuring without modifying external behavior or adding features.
    *   `chore`: Maintenance, asset configuration, Inspector bindings, parameter tuning.
    *   `docs`: Documentation, rule/skill updates, walkthroughs, or architecture guidelines.
    *   `test`: Adding or updating EditMode/PlayMode unit tests.
    *   `data`: Game balance configuration, shapes, or scenario test assets.
*   **Scope**: A concise lowercase identifier in parentheses indicating the affected subsystem (e.g., `(grid)`, `(block)`, `(spawn)`, `(score)`, `(assets)`, `(rules)`, `(effect)`).
*   **Strict Rules**:
    1. **English Only**: Commit messages MUST be written entirely in English.
    2. **Imperative Mood**: Start the subject with a lowercase imperative verb (e.g., `implement`, `add`, `fix`, `configure`, `refactor`, `remove`).
    3. **No Trailing Period**: Do NOT put a period (`.`) at the end of the commit subject line.
    4. **Concise & Atomic**: Commits must be atomic, focused on a single logical change, and keep the subject line within 72 characters.
