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

## 2.2. Required Cross-plan File Review

Before creating a new plan, enumerate Markdown plans recursively under `plans/` and read the complete contents of the other existing plans, including their task status, affected files, implementation steps, and dependencies. Listing filenames, reading only titles/status, or searching for matching names alone is not sufficient.

1. Determine the files/assets/settings the proposed task intends to create, modify, move, or delete, including indirect writes such as prefab Apply, scene Save, import settings, and shared configuration.
2. Compare this scope with the work described in the other plans. Resolve filenames to project-relative paths when possible. Include overlapping file paths, overlapping folder-wide edits, and shared dependencies whose modification affects either task.
3. Distinguish conflicting writes from read-only dependencies. Reading the same rule or script does not itself mean both tasks will edit it.
4. Record a Vietnamese `## Đối chiếu với các plan hiện có` section in the new plan: list the reviewed plans and, for overlaps, the other plan path, its recorded status, affected file/path, planned operations, and proposed coordination.
5. Report the overlap findings to the user when presenting the plan, linking the relevant plans/files. If no overlapping writes are found, say so explicitly. Report unreadable plans, incomplete file lists, ambiguous filenames, or unknown status as limitations; do not claim the review is complete when it is not.
6. A plan describes intended work, not live chat activity or file ownership. Do not infer that a chat is currently editing a file merely because its plan exists. Identify completed plans as historical overlap, and pending/in-progress/unknown plans as potential overlap requiring coordination before conflicting edits.
7. Do not automatically edit, cancel, delete, or take ownership of another plan/task. Planning and independent work may continue; resolve overlapping writes before implementation of those portions.
8. Recheck the relevant plans and report new overlap if the proposed scope changes or new/updated plans become known before implementation. Do not claim automatic cross-chat notifications or a file lock.

## 2.3. Completed Plan Retention and Deletion

After actually completing a task that has an implementation plan, update that task's plan status and validation accurately, then ask the user whether to delete that specific completed plan. Include a clickable link or exact path so the user knows which file is being discussed.

- Ask only after the task and its required validation are complete. Creating a plan, waiting for approval, pausing, or leaving required checks pending does not count as completion.
- Example question: "Task đã hoàn thành. Ông có muốn xóa plan `plans/<task_name>_plan.md` không?" Replace the placeholder with the actual completed plan path.
- Retain the plan until the user explicitly authorizes deletion. Silence is not approval. If the user already gave explicit deletion/retention instructions for that exact completed plan, follow them instead of asking again.
- Do not delete other plans, handoff documents, evidence, or an entire task folder as part of deleting one plan. Check inbound references and explain any required link updates before removal.
- Before an authorized deletion, reread the plan to confirm it is still the completed task's file and has not gained another chat's active work. Verify the resolved path is within the intended project `plans/` directory.
- If there is no plan for the completed task, do not invent one merely to offer deletion.

## Companion Rule

This file preserves the original shared-path and plan sections. The original tool and maintenance sections continue in [tools.md](tools.md). Read both documents for tool creation/updates and file organization; together they retain all original requirements.
