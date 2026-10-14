# Block Blast - Quy chuẩn Tổ chức Thư mục & Đường dẫn (Project Organization & Paths)

Quy định cấu trúc thư mục chuẩn, đường dẫn tương đối và phân định thẩm quyền tài liệu cho dự án Block Blast.

---

## 1. Đường dẫn Chuẩn Dự án (Project-Relative Paths)
Mọi đường dẫn giải quyết từ thư mục gốc của project (chứa `Assets/`, `Packages/`, `ProjectSettings/`). Tuyệt đối không hardcode đường dẫn ổ đĩa cục bộ.
* `plans/`: Implementation plans, quyết định task và handoff documents.
* `Tools/`: Script phát triển tái sử dụng và tự động hóa bên ngoài (giữ nguyên chữ hoa `Tools/`).
* `.agents/rules/`: Các quy chuẩn agent; `AGENTS.md` điều hướng tới đây.
* `Assets/`: Mã nguồn và tài nguyên Unity biên dịch/import.

---

## 2. Định tuyến Thẩm quyền Chuyên môn (Single Source of Truth Routing)
Để tối ưu context token và tránh mâu thuẫn quy chuẩn giữa các file:
* **Vòng đời Kế hoạch (Planning Lifecycle)**: Quy cách đặt tên, form mẫu 14 mục tích hợp, lệnh auto-submit, rà soát đối chiếu chồng lấn (cross-plan review), phê duyệt và hỏi xóa plan tuân thủ **DUY NHẤT** tại [plan_authoring.md](plan_authoring.md).
* **Công cụ Phát triển Bên ngoài (External Tools)**: Quy chuẩn viết mã, vị trí, đặt tên, tài liệu hóa `README.md` cho script Python, PowerShell trong `Tools/` tuân thủ **DUY NHẤT** tại [tools.md](tools.md).
* **Quy trình Phát triển & Phê duyệt (Workflow & Standards)**: Cổng duyệt, chính sách unit test, quy chuẩn commit Git tuân thủ **DUY NHẤT** tại [standards.md](standards.md).

## 3. Thứ tự Ưu tiên
Trong phạm vi tài liệu dự án: chỉ đạo trực tiếp của người dùng → `AGENTS.md` → rule chuyên môn đúng scope → baseline workflow trong `standards.md`. Rule chuyên môn không được tự nâng mình cao hơn router hoặc ghi đè rule thuộc domain khác.
