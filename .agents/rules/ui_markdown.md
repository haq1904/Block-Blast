# Block Blast - Quy chuẩn Soạn thảo Tài liệu & Thiết kế UI Spritesheet (UI Markdown Rule)

Nguồn chuẩn cho tài liệu UI Markdown và đặc tả UI spritesheet.

---

## 1. Phạm vi & Định tuyến

File này sở hữu taxonomy, metadata, concept, quyết định sheet, grid, centering, padding, slicing và oversized-component handling. Không sao chép rule chuyên môn khác:

| Domain | Nguồn chuẩn |
| --- | --- |
| Runtime layout, SafeArea, Canvas, touch target | [portrait_ui.md](portrait_ui.md) |
| Texture/import/atlas và số liệu hiệu năng | [asset_optimization.md](asset_optimization.md) |
| MVC, events, animation, material | [architecture.md](architecture.md), [events.md](events.md), [dotween.md](dotween.md), [shaders.md](shaders.md) |
| Plan, paths, workflow, tools | [plan_authoring.md](plan_authoring.md), [plans_and_tools.md](plans_and_tools.md), [standards.md](standards.md), [tools.md](tools.md) |

Thứ tự ưu tiên tuân thủ [plans_and_tools.md](plans_and_tools.md).

---

## 2. Phân loại Taxonomy cho Tài liệu UI Markdown

Mỗi tài liệu UI Markdown phải thuộc một loại:

| Document Type | Mục đích sử dụng | Vị trí khuyến nghị |
| :--- | :--- | :--- |
| `rule` | Quy chuẩn nguồn vĩnh viễn điều chỉnh hành vi của agent và dự án. | `.agents/rules/` |
| `plan` | Kế hoạch triển khai kỹ thuật có cấu trúc 14 mục tích hợp. | `plans/` |
| `design_spec` | Đặc tả thiết kế UI chi tiết: visual language, component catalog, bảng màu, typography, responsive behavior. | `plans/` hoặc thư mục feature UI |
| `asset_brief` | Bản tóm tắt kỹ thuật dùng để tạo asset (prompt cho AI generator, kích thước lưới, danh mục icon/button). | `plans/` hoặc thư mục asset UI |
| `handoff` | Tài liệu bàn giao kỹ thuật giữa các chat/agent (wireframe, prefab hierarchy, binding contract). | `plans/` |
| `validation_report` | Báo cáo kiểm thử thực tế kèm số liệu đo đạc, bằng chứng Profiler/Frame Debugger. | `plans/` hoặc kèm trong plan |
| `decision_log` | Bản ghi lại các quyết định kỹ thuật/kiến trúc quan trọng đã được người dùng phê duyệt. | `plans/` hoặc phần quyết định trong plan |

---

## 3. Khung Metadata Bắt buộc cho Tài liệu UI

Mọi tài liệu UI Markdown trừ `plan` và chính các file `rule` phải mở đầu bằng metadata sau. Plan dùng form trong [plan_authoring.md](plan_authoring.md); rule được nhận diện bằng vị trí `.agents/rules/`.

```markdown
# [Tên tài liệu UI]

DocumentType: [design_spec | asset_brief | handoff | validation_report | decision_log]
Status: [draft | pending_approval | approved | in_progress | completed | deprecated]
Owner/chat: [Tên chat phụ trách]
LastUpdated: [YYYY-MM-DD]
SourceRules: [Link tới các rule liên quan trong .agents/rules/]
Scope: [Hệ thống, màn hình hoặc asset chịu tác động]
Baseline: [File, commit hoặc hiện trạng trước khi tài liệu áp dụng]
AffectedFiles: [Danh sách file dự kiến tạo/sửa/xóa]
ApprovalState: [chờ duyệt | đã duyệt bởi user]
ValidationState: [pending | pass | blocked]
```

---

## 4. Cổng Quyết định Bắt buộc: Chiến lược UI Spritesheet

Khi bắt đầu một tác vụ tạo mới hoặc tái cấu trúc UI Spritesheet, agent **BẮT BUỘC PHẢI HỎI Ý KIẾN NGƯỜI DÙNG** để lựa chọn một trong hai phương án cấu trúc file:

1. **Single Master Spritesheet**: Một PNG chứa Buttons, Panels, Icons, Sliders và Badges; quản lý tập trung nhưng canvas lớn và một thay đổi buộc xuất lại sheet.
2. **Multi-sheet by Category**: Tách `buttons`, `panels`, `icons`, `controls`; dễ bảo trì và tối ưu cell size nhưng có thêm file/import/atlas grouping.

Không phương án nào tự bảo đảm draw-call count; kết quả phụ thuộc Canvas, material, mask, sorting và atlas page, phải đo bằng Frame Debugger.

> [!CAUTION]
> Agent **CẤM TUYỆT ĐỐI tự ý chốt phương án** dựa trên suy đoán về draw calls hay sự tiện lợi của bản thân.
> Nếu người dùng chưa đưa ra phản hồi lựa chọn, agent CHỈ ĐƯỢC PHÉP soạn thảo concept/spec ở trạng thái `draft`, KHÔNG ĐƯỢC CHỐT cấu trúc lưới hoặc tạo file asset.

---

## 5. Thu thập Concept & Tiền kỳ Thiết kế (Pre-Design Requirements)

Trước khi vẽ/sinh sheet, spec phải ghi đủ 8 mục:

1. `Game Theme & Visual Mood`.
2. `Visual References`: path ảnh/moodboard/asset dự án.
3. `Component Catalog`: đủ component, ID tiếng Anh (`Btn_Play_Normal`, `Icon_Settings`...).
4. `Color Palette`: Hex/RGBA cho Primary, Secondary, Accent, Neutral và text.
5. `Typography & Font`: font/style/size tương thích TMP theo [portrait_ui.md](portrait_ui.md).
6. `Lighting Intent & Shading`: một hướng sáng/shading thống nhất cho mọi icon/nút trên sheet.
7. `Background Transparency`: PNG alpha sạch (`A = 0`), không halo/fringe.
8. `Target Platform & Scale`: portrait `1080 × 1920` theo [portrait_ui.md](portrait_ui.md).

---

## 6. Tiêu chuẩn Kỹ thuật của UI Spritesheet (Mathematical Grid Standard)

### A. Grid

* `canvas_width = columns × cell_width`
* `canvas_height = rows × cell_height`

`columns`/`rows` là số nguyên dương; mọi cell trong một sheet có cùng `cell_width`/`cell_height`. POT (`512`, `1024`, `2048`, `4096`) chỉ bắt buộc khi platform compression/import profile đã chọn yêu cầu; source sheet có thể NPOT nếu import validation chứng minh không bị rescale hoặc tăng memory ngoài dự kiến theo [asset_optimization.md](asset_optimization.md).

### B. Component Map

Mỗi `design_spec`/`asset_brief` phải kê khai:

| Component Name | Row (0-indexed) | Col (0-indexed) | Occupied Cells ($W \times H$) | Bounding Box ($X, Y, W, H$ px) | Expected Center ($X, Y$ px) |
| :--- | :---: | :---: | :---: | :---: | :---: |
| `Btn_Play_Normal` | 0 | 0 | $1 \times 1$ | `(16, 16, 224, 224)` | `(128, 128)` |
| `Btn_Play_Pressed` | 0 | 1 | $1 \times 1$ | `(272, 16, 224, 224)` | `(384, 128)` |
| `Panel_Dialog_Frame` | 1 | 0 | $2 \times 2$ | `(16, 272, 480, 480)` | `(256, 512)` |

---

## 7. Alignment, Centering, Padding & Slicing

* Ranh giới mọi hàng/cột phải thẳng, liên tục xuyên suốt sheet; cấm cong, ngắt hoặc lệch cục bộ.
* Component phải nằm đúng tâm hình học của cell/khối cell occupied:
  $$\text{center}_x = \text{cell\_origin}_x + \frac{\text{cell\_width}}{2}$$
  $$\text{center}_y = \text{cell\_origin}_y + \frac{\text{cell\_height}}{2}$$
* CẤM dịch component khỏi tâm để né mép hoặc tiện vẽ.
* Padding từ nét vẽ đến cell boundary: tối thiểu `4–8 px` với canvas 1024, `8–16 px` với canvas 2048; phải ngăn texture bleeding khi scale/filter/mipmap.
* `Sprite Mode = Multiple` → `Slice` → `Grid by Cell Count` hoặc `Grid by Cell Size` phải cắt đúng 100% bằng một lần Slice, không chỉnh boundary thủ công.

---

## 8. Xử lý Component Quá khổ (Oversized Components)

Áp dụng đúng thứ tự:

1. **Scale to Fit**: giữ aspect ratio và padding; chỉ dùng nếu không mất chi tiết hoặc biến dạng text.
2. **Multi-Cell Block**: chiếm khối liền kề `M × N`, khai báo `Occupied Cells`, căn tâm toàn khối:
     $$\text{center}_x = \text{block\_origin}_x + \frac{M \times \text{cell\_width}}{2}$$
     $$\text{center}_y = \text{block\_origin}_y + \frac{N \times \text{cell\_height}}{2}$$
3. **Resize/Split**: nếu multi-cell lãng phí, hỏi người dùng chọn tăng cell size toàn sheet hoặc tách sheet chuyên biệt.

CẤM tràn cell khi chưa khai báo multi-cell, crop component hoặc dịch ranh giới lưới cục bộ.

---

## 9. Quy chuẩn Soạn thảo Văn bản UI Markdown

1. **Ngôn ngữ**: Nội dung mô tả, kế hoạch, nhận xét viết bằng **tiếng Việt**. Tên file, component ID, biến, class, shader property và code snippet **giữ nguyên tiếng Anh**.
2. **Đường dẫn chuẩn**: Trong tài liệu dùng project-relative path/link (`Assets/Art/UI/...`, `.agents/rules/...`). Khi bàn giao trong chat, dùng absolute clickable local link theo client; không ghi `file:///` vào tài liệu dự án.
3. **Thuật ngữ chuẩn hóa**: Dùng đúng thuật ngữ thống nhất: `Canvas`, `SafeArea`, `Reference Resolution`, `Primary CTA`, `Touch Target`, `Sprite Atlas`, `Raycast Target`, `Slice by Count`, `Cell Size`, `Padding`, `Bleeding`.
4. **Phân tách thực tế và đo đạc**:
   * Phân biệt rõ giữa *Yêu cầu lý thuyết*, *Giả định*, *Quyết định đã duyệt*, và *Dữ liệu đo đạc thực tế*.
   * Không khẳng định về FPS, draw call hay dung lượng RAM nếu chưa có số liệu trích xuất từ Unity Profiler hoặc Frame Debugger theo [asset_optimization.md](asset_optimization.md).
5. **Trách nhiệm phối hợp**:
   * Mọi tài liệu UI Markdown phải có danh sách file ảnh hưởng (`AffectedFiles`) và tên chat phụ trách.
   * Không chỉnh sửa tài liệu UI hoặc scene/prefab đang thuộc quyền sở hữu của chat khác mà không có sự điều phối từ người dùng.

---

## 10. Quy trình Vòng đời 9 Bước cho Task UI Spritesheet

Thực hiện đúng thứ tự; các bước 3 và 5 là approval gates riêng biệt:

1. **Source review**: đọc rule chuyên môn, kiểm tra trùng/mâu thuẫn.
2. **Concept**: ghi đủ 8 thành tố tại Mục 5.
3. **Sheet strategy**: hỏi Single hay Multi-sheet; dừng chờ lựa chọn.
4. **Grid spec**: chốt canvas, rows/columns, cell size, component map và expected centers.
5. **Spec approval**: trình bày đặc tả và dừng chờ duyệt.
6. **Generate & slice**: tạo asset đúng spec đã duyệt.
7. **Import validation**: áp dụng [asset_optimization.md](asset_optimization.md), Slice by Count/Size và kiểm từng sprite rect.
8. **Report**: cập nhật tài liệu, bằng chứng và `validation_report`.
9. **Retention**: hỏi lưu hay xóa plan theo [plan_authoring.md](plan_authoring.md).

Nghiệm thu chỉ đạt khi taxonomy/metadata hợp lệ, routing không sao chép rule khác, hai approval gates có bằng chứng, phương trình grid đúng, cell đồng nhất, component đúng tâm/padding, oversized component theo đúng thứ tự ưu tiên, slicing không cần chỉnh rect thủ công, đường dẫn/ngôn ngữ đúng Mục 9 và mọi claim hiệu năng có số liệu.
