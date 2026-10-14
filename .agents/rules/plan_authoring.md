# Block Blast - Quy chuẩn Soạn thảo Kế hoạch (Plan Authoring Rule)

Quy chuẩn cấu trúc và quy trình tạo implementation plan cho dự án Block Blast.

---

## 1. Khi nào Cần Tạo Plan & Handoff Plan Mặc định
* **Khi cần tạo**: Khi người dùng yêu cầu, khi công việc có nhiều bước/dependencies, động chạm nhiều file/scene/asset, cần cổng phê duyệt, hoặc cần bàn giao giữa các chat. Nhiệm vụ nhỏ trực tiếp làm theo direct-task workflow trong [standards.md](standards.md).
* **Handoff Plan Mặc định**: Khi người dùng yêu cầu lập plan mà không chỉ định loại cụ thể, **mặc định tạo handoff plan**.
  * Bắt buộc có chi tiết triển khai cụ thể: đường dẫn file ảnh hưởng, class/method, chữ ký hàm, luồng điều khiển và đoạn code snippet/patch outline trọng tâm. Không bịa code chưa kiểm chứng.
* **Tên file**: Tiếng Anh lowercase snake_case: `plans/<task_name>_plan.md`. Không tạo bản sao cạnh tranh; cập nhật plan hiện có nếu cùng tính năng.

---

## 2. Metadata Bắt buộc & Auto Submit
Mỗi plan Markdown BẮT BUỘC chứa các trường metadata trên cùng và mục auto submit:
* Tiêu đề cụ thể
* `RequestFeedback: true` (khi đang chờ phê duyệt)
* Trạng thái, Owner/chat, Ngày cập nhật
* Mục `## Auto submit` với lệnh thực tế của file:
  ```plaintext
  /auto-submit-plan plans/<task_name>_plan.md
  ```
* **Bàn giao trong Chat**: Mọi phản hồi trình bày plan mới/cập nhật BẮT BUỘC cung cấp đủ:
  1. Link Markdown có thể click tới absolute local path thực tế theo client; không ghi `file:///` vào tài liệu dự án.
  2. Khối mã `plaintext` copyable chứa lệnh auto-submit với đường dẫn tương đối thực tế.

---

## 3. Rà soát Đối chiếu Chồng lấn (Cross-Plan Review)
Trước khi lưu plan, đọc trọn vẹn toàn bộ các plan trong `plans/`:
1. Liệt kê toàn bộ file/asset dự kiến tạo/sửa/xóa (kể cả ghi gián tiếp: Save scene, Prefab apply, Import settings).
2. So sánh với các plan hiện có; phân biệt **xung đột ghi (conflicting write)** với **phụ thuộc chỉ đọc (read-only)**.
3. Thêm mục tiếng Việt `## Đối chiếu với các plan hiện có` ghi nhận bảng so sánh.
4. Báo cáo rõ kết quả trong chat khi trình bày plan (nêu rõ nếu không có overlap ghi).

---

## 4. Mẫu Cấu trúc Plan Tích hợp (Integrated Form)
Sao chép cấu trúc 14 mục sau vào `plans/<task_name>_plan.md`, thay thế placeholder:

```markdown
# [Tên task cụ thể]

RequestFeedback: true
Trạng thái: chờ duyệt.
Owner/chat: [nếu có]
Cập nhật: [YYYY-MM-DD]

## Auto submit

```plaintext
/auto-submit-plan plans/[task_name]_plan.md
```

## 1. Mục tiêu
[Kết quả cần đạt và lý do]

## 2. Phạm vi
### Được phép thay đổi
- [file/scene/asset/system]
### Không được thay đổi
- [out-of-scope hoặc thuộc chat khác]

## 3. Rule, skill và tool áp dụng
| Nguồn | Lý do đọc và yêu cầu liên quan |
| --- | --- |
| [rule/path] | [yêu cầu áp dụng] |

## 4. Baseline và giả định
- Baseline đã kiểm tra: [file/commit/hiện trạng]
- Giả định chưa xác minh: [ghi rõ]
- Giới hạn công cụ/thiết bị: [ghi rõ]

## 5. Đối chiếu với các plan hiện có
- Đã đọc: [toàn bộ file plan trong plans/]
- Không có overlap ghi: [hoặc điền bảng bên dưới]
| Plan liên quan | Trạng thái | File trùng | Loại thao tác | Cách phối hợp |
| --- | --- | --- | --- | --- |
| [path] | [status] | [path] | [write/read] | [phối hợp] |

## 6. File và quyền phụ trách
| Path | Tạo/sửa/xóa | Chat phụ trách | File dùng chung? | Tác động gián tiếp |
| --- | --- | --- | --- | --- |
| [path] | [thao tác] | [chat] | [yes/no] | [Save/Apply/import] |

## 7. Thiết kế và các bước triển khai
1. [Bước có input/output rõ ràng]
2. [Bước tiếp theo và dependencies]

### Code handoff (khi có thay đổi code)
```csharp
// [Proposed code contract / patch outline]
```
- File/class/method: [symbol]
- Contract / State changes: [mô tả]

## 8. Phối hợp file chung
- Baseline/diff trước khi sửa: [cách kiểm tra]
- Điều kiện dừng khi patch thất bại: [mô tả]

## 9. Unity Editor và test
- Cần lượt Editor cho: [PlayMode/scene/test hoặc Không cần]
- Việc độc lập khi Editor bận: [danh sách]

## 10. Asset optimization và dead-code review
- Asset cần tối ưu: [loại, budget, checks]
- Dead code/redundancy review: [phạm vi]

## 11. Validation matrix
| Kiểm tra | Công cụ/cách chạy | Kết quả mong đợi | Trạng thái |
| --- | --- | --- | --- |
| [check] | [command/Editor/manual] | [expected] | [pending/pass/blocked] |

## 12. Rủi ro và quyết định cần người dùng
- [Rủi ro/đánh đổi kỹ thuật]
- [Câu hỏi/quyết định cần duyệt]

## 13. Bàn giao và điều kiện hoàn thành
- [Bàn giao file/validation report]
- [Definition of done]

## 14. Retention
Sau khi task và validation hoàn thành, cập nhật status rồi hỏi người dùng có muốn xóa plan này không. Giữ plan cho đến khi người dùng cho phép xóa.
```

---

## 5. Nguyên tắc Thực thi & Nghiệm thu
* **Dừng chờ duyệt**: Giữ `RequestFeedback: true` và DỪNG THỰC THI cho đến khi người dùng phê duyệt tường minh.
* **Thực thi tuần tự**: Sắp xếp các bước theo thứ tự dependency; xác định rõ bước nào chạy độc lập khi Editor bận.
* **Ghi nhận trung thực**: Đánh dấu kiểm thử là `pending`, `pass`, hoặc `blocked` kèm bằng chứng; cấm báo cáo kiểm thử chưa chạy.
* **Hỏi xóa plan sau khi xong**: Cập nhật trạng thái xong thì hỏi người dùng có muốn xóa file plan cụ thể đó không; giữ nguyên nếu chưa được duyệt xóa.
