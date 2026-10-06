---
name: auto-submit-plan
description: Reads a user-provided Markdown implementation plan for the Block Blast Unity project and executes it end-to-end in the current Antigravity workspace without waiting for approval. Use when the user asks to execute a prepared plan directly.
---

# Auto-submit plan for Block Blast

Use this skill when the user provides a plan path or explicitly asks to run a
prepared plan.

## Invocation

From the Antigravity prompt:

```text
/auto-submit-plan plans/<plan-name>.md
```

If no path is provided, ask for the relative path under `plans/`.

## Execution

1. Resolve the plan path relative to the workspace root.
2. Read the complete plan before editing anything.
3. Read `AGENTS.md` and all applicable documents in `.agents/rules/`.
4. Execute the plan continuously in the current workspace.
5. Do not pause for approval of file edits or terminal commands.
6. Do not create a Git branch, checkpoint, commit, stash, reset, revert, or push.
7. Do not create persistent automation logs; report progress in the agent panel.
8. Do not discard or roll back existing user changes.

## Block Blast rules

- Keep Unity version `6000.4.3f1` unless the plan explicitly changes it.
- Preserve `.meta` files and existing GUIDs.
- Do not modify `Library/`, `Temp/`, `Logs/`, `obj/`, or `UserSettings/` directly.
- Follow the Core/Features and Model/Controller/View architecture.
- Follow applicable event, DOTween, shader, visual-effects, and asset rules.
- Run Unity Test Framework tests when the plan changes Model, Controller,
  algorithms, data models, or other test-covered gameplay logic.
- For View, UI, VFX, shader, audio, or DOTween-only changes, use appropriate
  compiler, Editor, or PlayMode verification instead of forcing unit tests.
- Do not commit or push when the plan is complete.

## Completion report

Report briefly:

- files changed;
- commands and Unity validation performed;
- compile/test result;
- unresolved warnings or limitations.
