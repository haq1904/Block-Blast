# Block Blast - Quy chuẩn Tổ chức & Bảo trì Công cụ (Tools)

Quy định cấu trúc, đặt tên, tài liệu và bảo trì cho các công cụ phát triển bên ngoài (`Tools/`) của dự án Block Blast. (Đi kèm với [plans_and_tools.md](plans_and_tools.md)).

---

## 1. Vị trí & Đặt tên Công cụ (Tool Locations & Naming)
* **Vị trí**: Mọi công cụ phát triển bên ngoài (Python, PowerShell, scripts tự động hóa) BẮT BUỘC lưu trong thư mục `Tools/` ở project root (hoặc thư mục con theo domain như `Tools/Blender/`, `Tools/Audio/`).
* **Quy ước đặt tên**: Tên file dùng tiếng Anh lowercase snake_case mô tả rõ hành động và đối tượng: `<action>_<task_or_subject>.<ext>`
  * Ví dụ chuẩn: `Tools/Blender/generate_pet_clear_props.py`, `Tools/validate_lobby_scene_references.py`.
  * **Nghiêm cấm**: Cấm đặt tên mơ hồ (`tool.py`, `script.ps1`, `helper.py`, `temp.py`).
* **Mã biên dịch Unity**: Scripts cần Unity biên dịch BẮT BUỘC đặt trong `Assets/` (runtime code theo [architecture.md](architecture.md); công cụ Editor đặt trong `Assets/.../Editor/`). **Tuyệt đối KHÔNG đưa vào `Tools/`** vì Unity sẽ không import.
* **Cấm để script rác tại Project Root**: Các script kiểm tra tạm thời một lần phải dùng thư mục tạm OS hoặc dọn dẹp ngay sau khi chạy.

---

## 2. Tài liệu Hóa & Đường dẫn (Documentation & Paths)
* **Tài liệu `README.md`**: Cung cấp hoặc cập nhật `README.md` trong thư mục công cụ chứa: mục đích, điều kiện tiên quyết, câu lệnh chạy từ project root, tham số, đường dẫn input/output, và ví dụ kiểm chứng.
* **Đường dẫn độc lập môi trường**: Giải quyết đường dẫn từ vị trí script hoặc project root; không phụ thuộc CWD cố định, không hardcode đường dẫn ổ đĩa cá nhân.
* **Đầu ra**: Asset game xuất ra phải đưa vào đúng thư mục trong `Assets/` kèm file `.meta`. Log/cache tạm thời không lưu vào source tree của `Tools/`.

---

## 3. Bảo trì & An toàn Vận hành
* Tái sử dụng hoặc mở rộng công cụ hiện có trước khi tạo mới.
* Áp dụng [asset_optimization.md](asset_optimization.md) và [blender_tools.md](blender_tools.md) cho công cụ và asset tạo ra.
* Việc tạo công cụ không cấp quyền chạy lệnh phá hủy dữ liệu, không bypass cổng duyệt, không commit và không can thiệp phiên Editor của chat khác.
