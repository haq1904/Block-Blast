---
trigger: always_on
description: Tool locations, task-specific naming, documentation, and maintenance; companion to plans_and_tools.md.
---

# Block Blast - Tool Organization and Maintenance

Read [plans_and_tools.md](plans_and_tools.md) for the shared project-relative paths and plan requirements. This file continues its original tool and maintenance sections without changing their content. Read both documents before creating/updating tools or reorganizing files.

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
