# Block Blast - Workflow & Coding Standards

Quy chuẩn quy trình phát triển, viết mã, lập kế hoạch, kiểm thử và cam kết Git cho dự án Block Blast.

---

## 1. Hiệu quả Token & Giao tiếp (Token Efficiency)
* **Brevity First**: Trả lời trực diện, đúng trọng tâm. Loại bỏ hoàn toàn các câu chào hỏi xã giao, lời dẫn rườm rà.
* **Token Saving**: Tiết kiệm token tối đa. Chỉ cung cấp thông tin cần thiết nhất để giải quyết công việc.
* **Efficient Code Edits**: Không in toàn bộ nội dung file lớn vào chat; dùng tool sửa code chính xác và tóm tắt ngắn gọn.

---

## 2. Phong cách Viết mã & Ngôn ngữ (Code Style)
* **English Only**: Toàn bộ code comments, `[Tooltip]`, debug logs, tên biến, phương thức và lớp **BẮT BUỘC** viết hoàn toàn bằng tiếng Anh.

---

## 3. Lập Kế hoạch
Toàn bộ tiêu chí tạo plan, cấu trúc, cross-plan review, auto-submit, approval và retention tuân thủ **duy nhất** [plan_authoring.md](plan_authoring.md); không sao chép hoặc biến thể quy trình tại file khác.

---

## 4. Chính sách Kiểm thử Đơn vị (Unit Testing Policy)
* **Chỉ chạy cho Model / Controller / Core Logic**: NUnit EditMode unit tests (`unityMCP:run_tests`) **CHỈ ĐƯỢC CHẠY** khi thay đổi business logic, thuật toán toán học, data models hoặc controllers (ví dụ: `GridModel`, `GridController`, `BlockSpawnGenerator`).
* **Bỏ qua cho View / Visual / Game Juice**: Các thay đổi thuần View, shaders, materials, audio, particle VFX hoặc DOTween (`GridView`, `ScoreView`, `PreClearEffectSO`) **KHÔNG ĐƯỢC CHẠY UNIT TESTS**. Xác minh bằng compiler diagnostics (`read_console`) và kiểm thử trực quan trên PlayMode để tối ưu tốc độ.

---

## 5. Quy chuẩn Commit Git (Conventional Commits)
Định dạng bắt buộc: `<type>(<scope>): <short description in imperative mood>`

* **Allowed Types**:
  * `feat`: Tính năng mới, cơ chế, game juice, hoặc tính năng người chơi.
  * `fix`: Sửa lỗi, khắc phục regression.
  * `refactor`: Tái cấu trúc mã nguồn không đổi hành vi ngoài và không thêm tính năng.
  * `chore`: Bảo trì, cấu hình asset, Inspector wiring, tinh chỉnh tham số.
  * `docs`: Cập nhật tài liệu, rule, walk-throughs, kiến trúc.
  * `test`: Thêm hoặc sửa EditMode/PlayMode unit tests.
  * `data`: Cấu hình game balance, shapes, test assets.
* **Scope**: Tên phân hệ viết thường trong ngoặc đơn (ví dụ: `(grid)`, `(block)`, `(spawn)`, `(score)`, `(assets)`, `(rules)`, `(effect)`).
* **Quy tắc nghiêm ngặt**:
  1. **Cổng duyệt bắt buộc**: **CẤM TUYỆT ĐỐI** tạo commit nếu chưa có chỉ đạo hoặc phê duyệt tường minh từ người dùng.
  2. **English Only**: Commit message 100% bằng tiếng Anh.
  3. **Thể mệnh lệnh (Imperative Mood)**: Bắt đầu bằng động từ mệnh lệnh viết thường (`implement`, `add`, `fix`, `configure`, `refactor`, `remove`).
  4. **Không có dấu chấm cuối câu**: Không đặt dấu `.` ở cuối dòng commit subject.
  5. **Ngắn gọn & Đơn nhiệm**: Mỗi commit là một thay đổi logic nguyên tử (atomic); subject không quá 72 ký tự.

---

## 6. Workflow
* **Task cần plan**: Áp dụng tiêu chí và approval gate trong [plan_authoring.md](plan_authoring.md). Sau khi tạo plan phải dừng; cấm sửa code trước khi người dùng duyệt tường minh.
* **Sau khi duyệt**: Thực thi plan liền mạch đến hết và tự kiểm tra compiler/diagnostics; không xin duyệt từng bước trung gian.
* **Direct task**: Chỉ làm ngay khi task không thuộc tiêu chí bắt buộc tạo plan trong `plan_authoring.md`.
* **Retention**: Sau khi task và validation hoàn tất, cập nhật plan rồi hỏi quyền xóa; giữ file đến khi được duyệt.

---

## 7. Quét Dọn Mã Thừa Bắt buộc (Dead Code Elimination)
Mỗi khi triển khai tính năng mới, chỉnh sửa hoặc sửa lỗi trong BẤT KỲ thành phần nào (C#, Python, Shaders, Editor tools):
* **Quét và Dọn dẹp**: Quét toàn bộ luồng logic của phân hệ bị ảnh hưởng để chủ động phát hiện và xóa mã thừa, không sử dụng hoặc bị bỏ qua.
* **Không để lại rác kỹ thuật**: Xóa ngay các helper methods obsolete, properties/fields deprecated, enums/assets không tham chiếu, nhánh rẽ dead code, và code bị comment-out.
* **Cấm giữ lại giải pháp cũ song song**: Không lưu giữ workaround cũ song song với giải pháp mới. Mã nguồn phải luôn tinh gọn và đơn nhiệm.
