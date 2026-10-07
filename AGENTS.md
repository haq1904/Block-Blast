# Block Blast — Agent Instructions

## Rule sources

The existing documents in `.agents/rules/` contain the detailed project rules.
Read and follow the applicable documents before working on a task. This file
routes agents to those documents; it does not replace or rewrite their contents.
Resolve all paths below relative to this project root.

At the beginning of a new chat's first project task, explicitly open and read
all ten rule documents listed below, including `standards.md`. Listing file
names or reading only frontmatter is not sufficient. Use filesystem tools to
read these documents; their contents are not automatically included by this
index. Keep them in context and apply the relevant rules throughout the task.
If a document changes, or its contents are no longer available in context,
read the applicable document again before continuing that work.

Read `.agents/rules/standards.md` at the start of each task for workflow,
planning, approval, testing, language, and commit requirements.

Read `.agents/rules/plans_and_tools.md` before creating or updating plans or
project tools. New plans use `plans/<task_name>_plan.md`; external development
tools belong under `Tools/` with filenames describing their action and task.
Optional task/domain subfolders follow the detailed rule. Whenever presenting
a newly created plan, provide its clickable file link and a copyable
`/auto-submit-plan` command with the actual project-relative plan path.
Every created or updated plan Markdown file must also include that command
in a `## Auto submit` section near the top, using its own actual path.

Before any task creates, duplicates, generates, or imports an asset, read
`.agents/rules/asset_optimization.md` and apply its optimization and validation
requirements alongside the relevant specialized rules.

| Task scope | Required documents |
| --- | --- |
| Plan creation/updates, handoff documents, project tools, automation scripts, file organization | `.agents/rules/plans_and_tools.md` |
| Any asset creation, duplication, generation, or import, including scenes and scripts/tools | `.agents/rules/asset_optimization.md` |
| Code changes, architecture, MVC, services, models, controllers, views | `.agents/rules/architecture.md` |
| Events, callbacks, communication between systems, game flow signals | `.agents/rules/events.md` |
| Animation, UI transitions, DOTween, tween lifecycle | `.agents/rules/dotween.md` |
| Placement, pre-clear, clear effects, visual feedback strategies | `.agents/rules/visual_effects.md` |
| Shaders, materials, renderer properties, visual rendering optimization | `.agents/rules/shaders.md` |
| 3D modeling, mesh optimization, texturing, asset import/export | `.agents/rules/modeling.md` |
| Blender Python scripts, add-ons, automation tools | `.agents/rules/blender_tools.md` |

A task may require several documents. For example, a clear animation normally
requires architecture, visual effects, DOTween, and any material rules that
apply. Blender asset automation may require both modeling and Blender tool
rules. Read additional documents when the scope expands, and follow references
inside the applicable documents when needed. Do not assume the rules directory
is automatically loaded merely because the files exist.

Keep the rule sources intact unless the user explicitly requests changes to
them. If a required document is missing, report it rather than inventing its
contents. Direct user instructions take precedence over project documents.

## Shared project coordination

The agreed setup uses multiple chats in the same project directory and branch.

- Do not create worktrees or switch branches unless the user requests it.
- Work on the scenes and files assigned to your chat. Coordinate with the user
  or the responsible chat before editing shared resources or another chat's
  files. Do not overwrite or revert changes made by other chats.
- Only the chat assigned the current Editor turn may control Unity through MCP.
  Before Editor operations, verify that Unity is open on this project directory.
- Other chats may continue editing their assigned files while one chat tests.
  Unity compilation and PlayMode state are shared across all chats.
- Follow the commit and approval requirements in `standards.md`. When a commit
  is authorized, include only the intended changes; do not stage unrelated work
  from other chats.
