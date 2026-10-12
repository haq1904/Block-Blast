# Block Blast - Quy chuẩn Tổ chức Kế hoạch và Công cụ (Plans & Tools)

Quy định cấu trúc thư mục, quy trình lập kế hoạch, lệnh auto-submit, đối chiếu chồng lấn và dọn dẹp plan cho dự án Block Blast.

---

## 1. Đường dẫn Chuẩn Dự án (Project-Relative Paths)
Mọi đường dẫn giải quyết từ thư mục gốc của project (chứa `Assets/`, `Packages/`, `ProjectSettings/`). Tuyệt đối không hardcode đường dẫn ổ đĩa cục bộ.
* `plans/`: Implementation plans, quyết định task và handoff documents.
* `Tools/`: Script phát triển tái sử dụng và tự động hóa bên ngoài (giữ nguyên chữ hoa `Tools/`).
* `.agents/rules/`: Các quy chuẩn agent; `AGENTS.md` điều hướng tới đây.
* `Assets/`: Mã nguồn và tài nguyên Unity biên dịch/import.

---

## 2. Quy tắc Tạo và Cập nhật Plan
* **Vị trí & Đặt tên**: Bắt buộc lưu tại `plans/<task_name>_plan.md` (hoặc `plans/<task_name>/<task_name>_plan.md` nếu có tài liệu bổ trợ).
  * Tên file dùng tiếng Anh lowercase `snake_case` mô tả rõ tính năng (ví dụ `plans/lobby_ui_layout_plan.md`).
  * Cấm đặt tên chung chung (`plan.md`, `task.md`) hoặc thêm hậu tố `_new`, `_final`, `_v2`.
* **Kế thừa**: Tra cứu `plans/` trước khi tạo mới; tiếp tục cập nhật plan hiện có thay vì tạo bản sao cạnh tranh. Không tạo plan ở project root, `docs/`, hay `Assets/`.
* **Ngôn ngữ**: Nội dung plan viết bằng **tiếng Việt**. Giữ nguyên thuật ngữ kỹ thuật, định danh, tên file và mã code bằng tiếng Anh.
* **Cấu trúc cốt lõi**: Phải bao gồm mục tiêu, phạm vi (được/không được sửa), file ảnh hưởng, các bước triển khai, rule áp dụng, ma trận kiểm thử, và điều kiện hoàn thành.
* **Approval Gate**: Đặt `RequestFeedback: true` khi chờ duyệt và dừng chờ theo [standards.md](standards.md). (Yêu cầu trực tiếp không cần tạo cổng duyệt mới).
* **Bảo vệ**: Không tự ý sửa đè, xóa hoặc chiếm quyền plan của task/chat khác.

---

## 3. Lệnh Auto-Submit Bắt buộc (Plan Delivery)

### A. Nhúng trong File Plan
Mỗi plan Markdown BẮT BUỘC chứa mục `## Auto submit` ở phần đầu với khối `plaintext` chứa đúng đường dẫn của chính nó:
```plaintext
/auto-submit-plan plans/<task_name>_plan.md
```
Cập nhật lại lệnh này nếu file bị đổi tên hoặc di chuyển; không để sót placeholder.

### B. Hiển thị khi Bàn giao Plan trong Chat
Mọi phản hồi trình bày plan mới/cập nhật tới người dùng BẮT BUỘC cung cấp đủ cả hai:
1. Link Markdown có thể click tới file cục bộ (`[tên_plan](file:///path/to/plan)`).
2. Khối mã `plaintext` copyable chứa lệnh auto-submit với đường dẫn tương đối thực tế:
   ```plaintext
   /auto-submit-plan plans/<task_name>_plan.md
   ```
*(Lưu ý: Lệnh này dành cho người dùng copy để kích hoạt skill auto-submit; hiển thị lệnh không tự động thực thi plan và không bỏ qua cổng phê duyệt).*

---

## 4. Rà soát Đối chiếu Chồng lấn Bắt buộc (Cross-Plan Review)
Trước khi tạo plan mới, duyệt đệ quy toàn bộ file `.md` trong `plans/` và đọc trọn vẹn nội dung:
1. **Liệt kê phạm vi ghi**: Mọi file, scene, asset, setting dự kiến tạo/sửa/xóa, bao gồm cả ghi gián tiếp (Prefab Apply, Scene Save, Import settings).
2. **So sánh chồng lấn**: Đối chiếu với từng plan hiện có. Phân biệt rõ **xung đột ghi (conflicting writes)** với **phụ thuộc chỉ đọc (read-only dependencies)**.
3. **Mục đối chiếu trong plan**: Bắt buộc thêm mục `## Đối chiếu với các plan hiện có` ghi nhận danh sách plan đã đọc, file trùng lặp, trạng thái và phương án phối hợp.
4. **Báo cáo trong chat**: Nêu rõ kết quả đối chiếu khi trình bày plan. Nếu không có file ghi trùng lặp, phải nêu rõ ràng là không có overlap.
5. **Giới hạn**: Plan là ý định công việc, không phải bằng chứng live chat hay khóa file. Phối hợp giải quyết xung đột ghi trước khi sửa mã nguồn.

---

## 5. Lưu trữ và Hỏi Xóa Plan Sau Hoàn thành (Retention & Deletion)
Sau khi task và kiểm thử hoàn tất thực tế:
1. Cập nhật trung thực trạng thái và kết quả kiểm thử trong plan.
2. **Hỏi người dùng có muốn xóa plan hay không**, kèm link/đường dẫn chính xác:
   * Ví dụ: *"Task đã hoàn thành. Bạn có muốn xóa plan `plans/<task_name>_plan.md` không?"*
3. **Chỉ xóa khi được người dùng phê duyệt rõ ràng**. Im lặng không phải là đồng ý. Nếu người dùng đã chỉ đạo giữ/xóa trước đó, tuân thủ chỉ đạo đó.
4. Trước khi xóa theo lệnh, đọc lại file để xác nhận đúng file của task đã xong và nằm trong thư mục `plans/`. Không xóa nhầm các tài liệu khác.

---

## 6. Quy chuẩn Bổ trợ
Quy tắc tổ chức và bảo trì công cụ phát triển bên ngoài (`Tools/`) được quy định chi tiết tại [tools.md](tools.md).
