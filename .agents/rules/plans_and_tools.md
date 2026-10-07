---
trigger: always_on
description: Required locations, naming, ownership, and documentation for implementation plans and project tools.
---

# Block Blast - Plan and Tool Organization

## 1. Project-relative Paths

Resolve paths from the current project root containing `Assets/`, `Packages/`, and `ProjectSettings/`. Never hardcode a developer's drive, username, or previous project location.

- `plans/`: implementation plans, task decisions, and handoff documents.
- `Tools/`: reusable development scripts and external automation.
- `.agents/rules/`: agent rules; `AGENTS.md` routes agents to them.
- `Assets/`: assets and code that Unity must import or compile.

Use the existing spelling `Tools/` consistently. Do not create a second `tools/` directory or perform a case-only rename.

## 2. Creating and Updating Plans

- Every new plan MUST be stored inside `plans/` at project root. The default path is `plans/<task_name>_plan.md`.
- If a task needs several related documents, use `plans/<task_name>/<task_name>_plan.md` and keep its supporting documents alongside it. A task folder is optional; the plan filename must still describe the task.
- Use English lowercase snake_case filenames describing the specific feature and work, for example `plans/lobby_ui_layout_plan.md` or `plans/pet_fetch_frisbee_exit_facing_fix_plan.md`. Independent tasks must have distinct descriptive names.
- Do not create new plans named `plan.md`, `implementation_plan.md`, or `task.md`. Do not append `new`, `final`, or `v2` merely to avoid a naming collision; update the existing plan for the same task or choose a descriptive name for a different task.
- Before creating a plan, inspect existing plans for the same task. Continue its existing plan instead of creating competing copies.
- Existing plans directly under `plans/` or using older filenames remain valid. Update them in place when continuing their tasks. Do not migrate or rename another chat's plans without coordination.
- Do not create new plans at the project root, in `docs/`, `Assets/`, `Tools/`, or temporary folders.
- Write plan content in Vietnamese. Keep technical terms, identifiers, filenames, and code in English, as required by [standards.md](standards.md).
- Include the objective, scope, affected files, implementation steps, applicable rules, validation, completion criteria, and important risks/dependencies. Scale detail to task complexity.
- Include `RequestFeedback: true` while requesting approval and wait as required by `standards.md`. Record explicit approval when received; update progress and validation truthfully. A plan file or a tool invocation alone is not approval.
- A direct implementation request does not require inventing a new approval gate; follow the direct-task workflow in `standards.md`.
- Give handoff/decision documents task-specific names, such as `<task_name>_handoff.md`. Keep them beside the plan, directly in `plans/` or in its optional task folder. Link to the canonical plan rather than copying it into multiple locations.
- Use project-relative links inside repository documents and absolute clickable file links when presenting local files in chat.
- Never overwrite, delete, or repurpose another task's plan. Coordinate before editing a plan owned by another chat.
- When moving a plan is explicitly requested, inspect path references and tool discovery first, preserve contents, update affected links/call sites, and verify the destination before removing an empty source folder.

## 2.1. Required Plan Delivery

Whenever creating or updating a plan, the Markdown plan file itself MUST contain a `## Auto submit` section near the top, after its title and approval/status metadata. In that section, include a `plaintext` code block with `/auto-submit-plan <actual_project_relative_plan_path>`, using the exact path of that same file under `plans/`. Do not leave placeholders in a saved plan. Refresh the embedded command whenever the file is renamed or moved.

The response presenting the plan to the user MUST also include both:

1. A clickable Markdown link to the actual saved plan using its absolute local path.
2. A separate `plaintext` code block containing a copyable invocation with the plan's actual project-relative path:

```plaintext
/auto-submit-plan plans/<task_name>_plan.md
```

Replace the placeholder with the exact filename and include any task subfolder. For example, when the saved plan is `plans/pet_signature_clear_plan.md`, provide:

```plaintext
/auto-submit-plan plans/pet_signature_clear_plan.md
```

- Verify that the file exists and that the clickable link, invocation in chat, and invocation embedded in the Markdown file all identify the same plan. Use forward slashes in the invocation.
- Never copy a previous task's path or present an unresolved placeholder as the user's command.
- When presenting an updated or renamed/moved plan, provide the invocation again using its current location.
- This invocation is for the user to copy into the environment supporting the `auto-submit-plan` skill (currently the project Antigravity workflow). Displaying it does not execute the plan or bypass the approval requirements in `standards.md`. Do not invoke it automatically merely because a plan was created.

## 3. Tool Locations and Documentation

- Inspect existing tools before adding a new one. Reuse or extend suitable tools within the authorized scope.
- Every retained external development tool MUST be stored inside `Tools/` at project root. Tools may live directly there or in an existing domain/task subfolder such as `Tools/Blender/` or `Tools/Audio/`; a domain folder is not mandatory.
- New tool filenames MUST describe the action and task/subject using English lowercase snake_case, normally `<action>_<task_or_subject>.<extension>`. Examples: `Tools/Blender/generate_pet_clear_props.py`, `Tools/Audio/generate_pet_clear_sfx.ps1`, and `Tools/validate_lobby_scene_references.py`.
- Do not create tools named `tool.py`, `script.ps1`, `helper.py`, or `temp.py`. A tool shared across tasks must describe its actual reusable function, for example `validate_scene_references.py`.
- Standard supporting filenames such as `README.md` and configuration/dependency manifests keep their conventional names. Keep required configuration/templates beside the tool.
- Existing tools remain valid and may be updated in place; do not rename another task's tool solely to enforce the new naming convention.
- Provide or update the nearest `README.md` with purpose, prerequisites, invocation from project root, parameters, input/output paths, side effects, and a verification example. Label examples as examples; do not claim a tool exists before it has been created.
- Resolve paths from the script/project location or explicit arguments. Do not rely on a particular current directory or embed local machine paths. Validate input/output destinations and report failures clearly.
- Temporary one-off checks belong in an OS temporary directory unless they are useful enough to retain as a documented reusable tool. Do not leave random helper scripts at project root.
- Generated game assets go into their proper `Assets/` feature/category folder with valid Unity metadata/references, not into `Tools/` or `plans/`.
- Keep generated logs, captures, caches, and build outputs outside tool source folders in appropriate existing output locations. Do not commit them by default or store credentials in tools/configuration.
- Scripts that require Unity compilation remain under the appropriate `Assets/` location: runtime code follows [architecture.md](architecture.md), and Editor-only utilities use an appropriate `Editor/` folder or Editor-only assembly. Document their usage near the utility. Do not move them into `Tools/` where Unity will not import them.
- Creating a tool does not authorize executing destructive operations, controlling another chat's Editor turn, creating commits, or bypassing existing approval requirements. Read the tool documentation and relevant project rules before running it.
- Apply [asset_optimization.md](asset_optimization.md) to new tools and generated assets, and [blender_tools.md](blender_tools.md) when applicable.

## 4. Maintenance

Keep existing plans, tools, and dependencies intact unless their migration is part of the authorized task. Before renaming or moving a tool, inspect callers, configuration, and documentation; update them and verify the result.

For documentation-only organization changes, verify paths, links, and the scoped diff. Do not run Unity or unrelated tests merely to validate Markdown changes.
