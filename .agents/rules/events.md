# Block Blast - Event Communication Guidelines

Mô hình giao tiếp sự kiện 2 tầng; Pure C# là mặc định, GameEvent SO là ngoại lệ cần duyệt.

---

## 1. Ma trận Quyết định

| Tiêu chí | Pure C# Event | GameEvent SO |
| :--- | :---: | :---: |
| Chứa payload dữ liệu (struct, class, list)? | **Bắt buộc** | Tránh |
| Tần suất cao / streaming liên tục? | **Bắt buộc** | Cấm |
| Giao tiếp nội bộ feature (`Controller -> View`)? | **Bắt buộc** | Cấm |
| Được bao phủ bởi EditMode Unit Tests? | **Bắt buộc** | Cấm |
| Trao đổi dữ liệu liên hệ thống qua Service Locator? | **Bắt buộc** | Tránh |
| Chuyển trạng thái macro (`OnGameOver`, `OnGameStart`)? | Tùy chọn | **Khuyên dùng** |
| Plug & Play Game Juice (VFX, Shake, SFX)? | Hạn chế | **Khuyên dùng** |
| Giao tiếp cross-scene (`Bootstrap` $\leftrightarrow$ `GamePlay`)? | Service Locator | **Khuyên dùng** |
| **Yêu cầu phê duyệt từ người dùng trước khi dùng?** | **Không (Mặc định)** | **BẮT BUỘC** |

Pure C# event phục vụ type safety và EditMode test mà không phụ thuộc Unity Editor/Object; nó **không tự bảo đảm zero GC**. Mọi allocation từ payload, list, closure hoặc subscriber phải được kiểm tra riêng.

GameEvent SO chỉ dành cho macro game flow (`OnGameStart`, `OnGameOver`, `OnGamePause`, `OnGameResume`, `OnRevive`), signal-only, plug-and-play 1-nhiều, cross-scene hoặc Inspector `UnityEvent` binding.

## 2. Quy tắc Nghiêm ngặt

* **Approval gate**: CẤM tạo mới, tích hợp hoặc chuyển tính năng sang GameEvent SO khi chưa được người dùng phê duyệt rõ ràng; phải đề xuất và chờ xác nhận.
* **Model**: CẤM phát bất kỳ event nào. Model là pure C# class chỉ giữ và biến đổi trạng thái nội bộ.
