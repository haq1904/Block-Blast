---
trigger: always_on
description: Required workflow and integrated form for creating Block Blast implementation plans.
---

# Block Blast - Plan Authoring Rule

Read this rule before creating or substantially updating an implementation plan. It defines the required plan structure; it does not replace `standards.md`, `plans_and_tools.md`, `tools.md`, or applicable feature rules.

## 1. When to Create a Plan

Create a plan when the user asks for one, when work has multiple implementation steps or dependencies, when several files/scenes/assets are involved, when an approval gate is required, or when another chat may need to continue the work. A small direct task with a clear scope may follow the direct-task workflow in `standards.md` without inventing a plan.

Before creating a plan, search `plans/` for the same task. Continue the existing plan instead of creating a competing copy. Use an English lowercase snake_case task name that describes the feature and work, such as `lobby_ui_layout_plan.md`.

## 1.1. Handoff Plan Default

When the user asks to create a plan without specifying a type, create a **handoff plan** by default. Focus on the exact work the user requested so another chat can execute it or continue it. Do not create additional architecture, design, asset, testing, migration, or sub-plans unless the user explicitly asks for that plan type.

A handoff plan MUST contain concrete implementation details. If the task changes code, include the affected file paths, relevant classes/methods, proposed signatures, control flow, data changes, and a focused code snippet or patch outline when the implementation is sufficiently known. If exact code cannot be determined before inspection, include the code contract and the inspection step needed to finalize it; never invent code and label it as verified.

## 2. Required Plan Metadata

Every new task plan MUST contain:

- a specific title;
- `RequestFeedback: true` while waiting for approval;
- a truthful status, owner/chat, and last-updated note when those are known;
- a `## Auto submit` section near the top with the exact command for that file:

```plaintext
/auto-submit-plan plans/<task_name>_plan.md
```

Replace every placeholder in a real plan before saving it. The command in the plan, the command shown in chat, and the clickable file link must identify the same file. The command is for the user to copy; it does not execute the plan or bypass approval rules.

## 3. Mandatory Cross-plan Review

Before creating a new plan, recursively enumerate all Markdown plans under `plans/` and read their complete contents. Do not rely on filenames, headings, or status alone.

1. List every file, scene, asset, setting, folder-wide edit, indirect Save/Apply/import write, and shared dependency the proposed task may create, modify, move, or delete.
2. Compare that scope with every existing plan. Distinguish a conflicting write from a read-only dependency.
3. Add a Vietnamese section named `## Đối chiếu với các plan hiện có` to the new plan. Record the plans reviewed, overlaps, recorded status, affected path, planned operation, proposed coordination, unreadable plans, and unknown information.
4. Report the same findings to the user when presenting the plan. Say explicitly when no overlapping writes are found.
5. Treat plan content as intended work, not proof of live chat activity, ownership, or release. Pending/in-progress/unknown plans indicate potential overlap; completed plans are historical overlap.
6. Recheck relevant plans if the proposed scope changes or a new/updated plan becomes known before implementation. Never take ownership, cancel, edit, or delete another task's plan automatically.

## 4. Integrated Plan Form

Copy the following structure into `plans/<task_name>_plan.md`, replace all bracketed placeholders, and remove instructions that do not apply. Keep the plan in Vietnamese; retain technical terms, identifiers, paths, code, and commands in English.

```markdown
# [Tên task cụ thể]

RequestFeedback: true
Trạng thái: chờ duyệt.
Owner/chat: [nếu đã được phân công]
Cập nhật: [YYYY-MM-DD]

## Auto submit

```plaintext
/auto-submit-plan plans/[task_name]_plan.md
```

## 1. Mục tiêu

[Kết quả cần đạt và lý do.]

## 2. Phạm vi

### Được phép thay đổi

- [file/scene/asset/system]

### Không được thay đổi

- [out-of-scope và thay đổi thuộc chat khác]

## 3. Rule, skill và tool áp dụng

| Nguồn | Lý do đọc và yêu cầu liên quan |
| --- | --- |
| [rule/skill/tool path] | [yêu cầu áp dụng] |

## 4. Baseline và giả định

- Baseline đã kiểm tra: [file/scene/commit hoặc hiện trạng thực tế]
- Giả định chưa xác minh: [ghi rõ]
- Giới hạn thông tin/tool/device: [ghi rõ]

## 5. Đối chiếu với các plan hiện có

- Đã đọc: [danh sách toàn bộ plan]
- Không có overlap ghi: [hoặc bảng bên dưới]

| Plan liên quan | Trạng thái ghi nhận | File/path trùng | Loại thao tác | Cách phối hợp |
| --- | --- | --- | --- | --- |
| [path] | [status/unknown] | [path] | [write/read/dependency] | [owner/bàn giao] |

## 6. File và quyền phụ trách

| Path | Tạo/sửa/xóa | Chat phụ trách | File dùng chung? | Tác động gián tiếp |
| --- | --- | --- | --- | --- |
| [project-relative path] | [operation] | [chat] | [yes/no] | [Save/Apply/import/dependency] |

## 7. Thiết kế và các bước triển khai

1. [Bước có input/output rõ]
2. [Bước tiếp theo và dependency]

### Code handoff (khi có thay đổi code)

```csharp
// [Proposed file/class/method contract or focused patch outline]
```

- File/class/method: [path and symbol]
- Input/output and state changes: [contract]
- Exact implementation still requiring inspection: [state honestly]

## 8. Phối hợp file chung

- Baseline/diff trước khi sửa: [cách kiểm tra]
- File chỉ một chat được ghi: [path/owner]
- Điều kiện dừng khi patch thất bại hoặc thay đổi ngoài dự kiến: [mô tả]

## 9. Unity Editor và test

- Cần lượt Editor cho: [scene/Inspector/Save/Apply/PlayMode/test]
- Việc độc lập khi Editor bận: [danh sách]
- Điều kiện nhận/release lượt: [trạng thái cần báo]
- Validation window: [compile/import/test stability]

## 10. Asset optimization và dead-code review

- Asset cần tối ưu: [loại, budget, import/runtime checks]
- Dead code/dependency cần rà soát: [phạm vi và bằng chứng]
- Không xóa thay đổi không rõ nguồn gốc hoặc reference chưa xác minh.

## 11. Validation matrix

| Kiểm tra | Công cụ/cách chạy | Kết quả mong đợi | Trạng thái |
| --- | --- | --- | --- |
| [check] | [command/Editor/manual] | [expected] | [pending/pass/blocked] |

## 12. Rủi ro và quyết định cần người dùng

- [risk/trade-off]
- [question/approval needed]

## 13. Bàn giao và điều kiện hoàn thành

- [files/status/validation to report]
- [Editor release and next-chat handoff]
- [definition of done]

## 14. Retention

Sau khi task và validation hoàn thành, cập nhật status/truthful results rồi hỏi người dùng có muốn xóa plan này không. Giữ plan cho đến khi người dùng cho phép xóa.
```

The nested `plaintext` block above is part of the form. When copied into a real plan, use a valid Markdown fence layout and the real path; do not leave `[task_name]` unresolved.

## 5. Implementation and Validation Rules

- Keep the plan as a plan until the user explicitly approves it. `RequestFeedback: true` is an approval gate, not evidence of approval.
- Write implementation steps in dependency order and state which steps can run while Editor is busy. Do not promise a test that requires a released Editor.
- Mark validation as `pending`, `pass`, or `blocked` with evidence. Never report unrun tests, unverified performance, or assumed file ownership as complete.
- Include asset optimization and dead-code review only within the task scope. Do not delete code, assets, references, or another chat's work merely because a call site is not immediately visible.
- After a planned task is actually complete, update the plan and ask whether the user wants to delete that exact plan. Retain it until explicit deletion approval.
- If the plan is handed to another chat, report files changed, validation, unresolved limits, Editor state, and next action. The receiving chat must reread the current files before editing.
- When a plan request did not specify another type, verify that the plan is a handoff plan and includes concrete code details whenever code is involved. Do not add an unrequested second plan.

## 6. Boundaries

This rule does not create file locks, a live chat registry, Editor queue, automatic notifications, branches, worktrees, commits, or push operations. It improves plan quality and coordination; it cannot prove that another chat is idle or prevent every race condition.
