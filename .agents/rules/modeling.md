# Block Blast - 3D Modeling & Blender Optimization Guidelines

Quy chuẩn tối ưu hóa mô hình 3D, vật liệu, UV và xuất file FBX từ Blender cho dự án Block Blast (chuẩn 60/120 FPS mobile).

---

## 1. Giới hạn Phạm vi: Chỉ Model Đúng Vật phẩm Yêu cầu
* **Quy tắc nghiêm ngặt**: CHỈ model chính xác đối tượng mà người dùng yêu cầu.
* **CẤM TUYỆT ĐỐI**: Không tự ý tạo thêm đạo cụ phụ, nền cảnh, chi tiết trang trí hoặc biến thể ngoài yêu cầu.

---

## 2. Ngân sách Đa giác & Tối ưu Lưới (Mesh Budget)

### A. Ngân sách Tam giác theo Loại Asset
| Phân loại Asset | Ngân sách Mục tiêu | Giới hạn Cứng | Ví dụ |
| :--- | :---: | :---: | :--- |
| **Gameplay Cell Blocks** | 100 – 300 tris | 500 tris | `WoodCrate`, `JellyBlock`, `StoneBlock` |
| **Micro-Debris / Particles** | 12 – 40 tris | 60 tris | `Mesh_MicroDebris_Wood`, `JellyDrop` |
| **Spinning Tools & Props** | 200 – 500 tris | 800 tris | `SawBlade`, `DrillBit`, `RotatingGear` |
| **Machinery & Complex Props** | 600 – 1,000 tris | 1,500 tris | `CircularSawMachine`, `Chainsaw`, `Bomb` |

### B. Quy tắc Hình học & Topology
1. **CẤM N-Gons**: Lưới CHỈ được chứa **Quads và Triangles**. Triangulate (`Ctrl + T`) các mặt phức tạp trước khi xuất.
2. **Xóa mặt khuất (Hidden Face Culling)**: Luôn xóa các mặt không bao giờ thấy (mặt đáy block tiếp xúc bàn cờ, mặt trong vỏ máy).
3. **Phân đoạn Cylinder & Wheel**: Giới hạn **12 đến 16 đỉnh** (tối đa 16). **CẤM dùng mặc định 32 hoặc 64 đỉnh**.
4. **Giới hạn Bevel**: Chỉ dùng **bevel 1 segment** (`Profile: 0.5`). Cấm dùng bevel 3-4 segments trên asset mobile.

---

## 3. UV Mapping & Bảng Màu Tập trung (1 Draw Call Standard)
* **Point-Collapsed UV Mapping**: Thu nhỏ toàn bộ UV island của mặt về 1 điểm kích thước 0 (`S` $\rightarrow$ `0` $\rightarrow$ `Enter`), đặt vào ô màu tương ứng trên bảng màu chung:
  `Assets/Art/LeapLand/tex/palette.png`
* **Chuẩn 1 Material duy nhất**: Mọi model CHỈ dùng **đúng 1 slot Material** gán tới `DefaultMat` (`Assets/Art/Blocks/DefaultMat.mat`). Không gán nhiều material cho các phần khác nhau của prop.
* **Cấm Texture 1K/2K riêng lẻ**: Không tạo texture diffuse/normal riêng cho từng prop trừ khi được duyệt cho hero VFX.

---

## 4. Điểm Đặt Pivot & Phân tách Thành phần
* **Chi tiết Xoay (Lưỡi cưa, bánh xe, bánh răng)**:
  * Pivot BẮT BUỘC đặt **chính xác tại tâm hình học của trục xoay** để DOTween quay không bị đảo trục (`Origin to 3D Cursor`).
  * Tách phân cấp đối tượng: Root Empty (Pos 0,0,0) $\rightarrow$ Body (thân máy tĩnh) + Blade (lưỡi xoay có pivot tại tâm đĩa). Cấm gộp chung thành 1 mesh phẳng.
* **Gameplay Blocks & Vật phẩm Đặt sàn**:
  * Pivot tại **đáy trung tâm**: $X = 0, Z = 0, Y = 0$. Chiều cao mở rộng từ $Y = 0$ lên $Y = 1.0$ (Unit bounding box: $1 \times 1 \times 1$).

---

## 5. Shading & Pháp tuyến (Normals)
* **Auto Smooth**: Luôn bật `Shade Auto Smooth` góc **$30^\circ - 45^\circ$** (hoặc modifier `Smooth by Angle` trong Blender 4.x).
* **Đánh dấu Sharp**: Các cạnh cắt cơ khí hoặc răng cưa phải đánh dấu rõ `Mark Sharp` (`Ctrl + E`).
* **Tính lại Normals**: Luôn quay pháp tuyến ra ngoài trước khi xuất (`Shift + N` - Recalculate Outside).

---

## 6. Freeze Transforms & Cấu hình Xuất FBX
* **Freeze Transforms**: Trước khi xuất, trong Object Mode: nhấn **`Ctrl + A` $\rightarrow$ `All Transforms`** (đảm bảo Position `0,0,0`, Rotation `0,0,0`, Scale `1,1,1`).
* **Cấu hình Xuất FBX (`File` $\rightarrow$ `Export` $\rightarrow$ `FBX`)**:
  * **Include**: Bật `Limit to: Selected Objects`; Object Types chỉ chọn `Armature` và `Mesh`.
  * **Transform**: Scale `1.0`, Apply Scalings: `FBX Units Scale`, Forward: `-Z Forward`, Up: `Y Up`, Bật `Apply Transform`.
  * **Geometry**: Smoothing: `Face` hoặc `Normals Only`, Bật `Apply Modifiers`.
  * **Animation**: Tắt export animation baking cho static/DOTween meshes.

---

## 7. Checklist Kiểm tra 5 Điểm Trước khi Xuất (5-Point Verification)
- [ ] **Triangle Budget**: Số tris nằm trong ngân sách (< 500 cho block, < 1,000 cho prop).
- [ ] **Zero N-Gons**: Toàn bộ mặt là tris hoặc quads.
- [ ] **UV Mapping**: Đã collapse điểm và map đúng vào `palette.png`.
- [ ] **Pivot Alignment**: Pivot tại tâm trục xoay (với chi tiết quay) hoặc tại đáy trung tâm (với block).
- [ ] **Transforms Baked**: Scale `(1, 1, 1)` và Rotation `(0, 0, 0)` đã freeze hoàn toàn.
