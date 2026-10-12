# Block Blast - Event Communication Guidelines

Mô hình giao tiếp sự kiện 2 tầng (Dual-Tier) nhằm tối đa hóa hiệu năng, an toàn kiểu (type safety), phục vụ Unit Testing và phân tách phụ thuộc linh hoạt:

---

## 1. Tầng 1: Pure C# Events (`event Action`) [Chuẩn Mặc định]
BẮT BUỘC sử dụng khi thỏa mãn **BẤT KỲ** điều kiện nào sau đây:
1. **Mang theo Dữ liệu (Payload)**: Event truyền structs, classes, danh sách tọa độ, danh sách placement, tham chiếu model.
2. **Tần suất cao & Streaming**: Bắn liên tục trong gameplay (mỗi frame, cử chỉ vuốt kéo, đếm số, cập nhật preview).
3. **Trao đổi Dữ liệu Liên hệ thống**: Giao tiếp giữa các hệ thống qua Service Locator (`IGridService`, `IScoreService`, `ISpawnService`).
4. **Nội bộ Phân hệ MVC**: Giao tiếp trực tiếp từ `Controller -> View` trong một feature.
5. **Logic Cốt lõi & Unit Testing**: Thuật toán bàn cờ, tính điểm, spawning thuộc phạm vi NUnit EditMode tests (chạy thuần RAM, zero GC, không phụ thuộc Unity Editor).

---

## 2. Tầng 2: ScriptableObject Game Events (`GameEvent SO`) [Cần Phê duyệt]
CHỈ ĐƯỢC PHÉP dùng trong các trường hợp ngoại lệ sau:
1. **Chuyển đổi Trạng thái Macro (Game Flow)**: Các mốc vòng đời cấp cao (`OnGameStart`, `OnGameOver`, `OnGamePause`, `OnGameResume`, `OnRevive`).
2. **Tín hiệu Thuần túy (Signal-Only)**: Thông báo bắn-và-quên không mang theo payload phức tạp.
3. **Game Juice Cắm-và-Chạy (Plug & Play)**: Broadcast 1-nhiều tới các hệ thống ngoại vi độc lập (Rung Camera via CinemachineImpulse, Rung haptic, Particle, SFX) mà không sửa code Controller.
4. **Tách rời Liên Scene**: Giao tiếp giữa các Scene khác nhau (`GamePlayScene` $\leftrightarrow$ `BootstrapScene`) mà không dùng Singleton hay `FindObjectOfType`.
5. **Designer Bindings**: Hỗ trợ thiết kế Inspector bằng `UnityEvent` không cần đụng mã nguồn C#.

---

## 3. Quy tắc Nghiêm ngặt (Strict Rules)
* **CỔNG DUYỆT BẮT BUỘC**: **CẤM TUYỆT ĐỐI** tạo mới, tích hợp hoặc chuyển đổi bất kỳ tính năng nào sang dùng ScriptableObject Game Events nếu chưa có sự phê duyệt rõ ràng từ người dùng. Luôn đề xuất và chờ xác nhận trước.
* **CẤM MODEL PHÁT SỰ KIỆN**: **Model CẤM TUYỆT ĐỐI phát ra bất kỳ event nào**. Model là C# class thuần túy chỉ lưu trữ dữ liệu và trạng thái.

---

## 4. Ma trận Quyết định (Decision Matrix)

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
