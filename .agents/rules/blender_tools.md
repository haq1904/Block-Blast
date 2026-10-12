# Block Blast - Blender Tool & Addon Optimization Guidelines

Quy chuẩn hiệu năng, quản lý bộ nhớ và kiến trúc cho Blender Python addons và công cụ tự động hóa (`block_tools.py` và các tool tương lai).

---

## 1. Phân cấp Truy cập Dữ liệu & Hiệu năng
* **Nguyên tắc Vàng**: **CẤM TUYỆT ĐỐI gọi `bpy.ops` trong vòng lặp** (`for v in verts: bpy.ops...` gây đơ viewport vì push undo và trigger depsgraph liên tục).
  * Thao tác dữ liệu trực tiếp qua `bmesh` hoặc mảng `mesh.vertices/polygons`.
  * Chỉ dùng `bpy.ops` cho thao tác vĩ mô một lần (`bpy.ops.export_scene.fbx`, `bpy.ops.object.mode_set`).
* **Ma trận Lựa chọn Kỹ thuật**:
  * **Đọc/ghi tọa độ hàng loạt (10,000+ verts)**: `foreach_get` / `foreach_set` với NumPy (nhanh nhất, copy trực tiếp bộ nhớ C).
  * **Chỉnh sửa topology, selections, UV/Color layers**: `bmesh` (`bmesh.from_edit_mesh` hoặc `bmesh.new()`).
  * **Macro actions một lần**: `bpy.ops`.

---

## 2. Quản lý Context & Chuyển đổi Chế độ (Mode Management)
Hạn chế tối đa chuyển đổi mode (`bpy.ops.object.mode_set`):
* **Edit Mode**: Nếu người dùng đang ở Edit Mode, dùng `bm = bmesh.from_edit_mesh(obj.data)`, thao tác trực tiếp, gọi `bmesh.update_edit_mesh(obj.data)` một lần khi kết thúc. Không chuyển sang Object Mode trừ khi thêm/xóa attribute yêu cầu bắt buộc.
* **Headless / Object Mode**: Tạo `bm = bmesh.new()`, nạp bằng `bm.from_mesh(obj.data)`, thao tác, ghi bằng `bm.to_mesh(obj.data)` và lập tức giải phóng bằng `bm.free()`.

---

## 3. Kiến trúc Color Attributes & UV (Blender 3.4+)
* **`FLOAT_COLOR` vs `BYTE_COLOR`**:
  * `FLOAT_COLOR` (linear HDR): Nằm trong `bm.loops.layers.float_color`.
  * `BYTE_COLOR` (legacy sRGB 8-bit): Nằm trong `bm.loops.layers.color`.
* **Mẫu hàm chuẩn tránh sinh layer trùng (`.001`, `.002`)**:
  ```python
  def get_or_create_bmesh_color_layer(bm, name="BT_TIER_COLORS"):
      layer = bm.loops.layers.float_color.get(name)
      if layer is not None: return layer, 'FLOAT_COLOR'
      layer = bm.loops.layers.color.get(name)
      if layer is not None: return layer, 'BYTE_COLOR'
      return bm.loops.layers.float_color.new(name), 'FLOAT_COLOR'
  ```
* **Cảnh báo Read-Only**: `bm.loops.layers.color.active` là READ-ONLY. Gán layer active trên Object Data: `obj.data.color_attributes.active_color = target_ca`.
* **Xóa Duplicate Layers**: Giữ duy nhất attribute canonical `BT_TIER_COLORS`; xóa bỏ các layer đánh số `.001`, `.002` khi chuẩn hóa.
* **Color Space Matching**: FBX lượng tử hóa vertex color sang sRGB 8-bit. Khi đọc lại (`try_restore_tiers_from_colors`), phải kiểm tra cả Linear và sRGB ($c^{2.2}$) với ngưỡng dung sai (tolerance) $0.38$.

---

## 4. Hình học Tính toán & Tối ưu Thuật toán
* **Bọc Hộp Tối thiểu (2D Convex Hull)**: Khi model bị xoay góc yaw ngẫu nhiên ($15^\circ, 33.7^\circ, 45^\circ$):
  1. Chiếu đỉnh 3D xuống mặt phẳng 2D XY.
  2. Tính 2D Convex Hull qua thuật toán Andrew's Monotone Chain ($O(N \log N)$).
  3. Đánh giá diện tích bounding box căn theo cạnh bao (Freeman & Shapira).
  4. Xoay mesh một góc $-\theta$ để đưa 4 vách về trực giao hoàn hảo với trục X và Y ($0.00^\circ$ sai số, $< 5\text{ms}$).
* **Truy vấn Không gian (KDTree / BVHTree)**: Tìm kiếm đỉnh gần nhất hoặc raycast snapping: dùng `mathutils.kdtree.KDTree` (tránh vòng lặp lồng $O(N \times M)$).

---

## 5. UI Panel & Khả năng Phản hồi
* **CẤM tính toán nặng trong `draw()`**: `Panel.draw()` chạy liên tục mỗi khi rê chuột. Tuyệt đối KHÔNG duyệt đỉnh/mặt hay tính bounding box trong `draw()`. Lưu kết quả tính toán vào `PropertyGroup` (`BlockToolsSettings`) trong lúc `execute()`, và chỉ hiển thị giá trị đã cache trong `draw()`.
* **Non-destructive Baseline**: Lưu tọa độ/UV gốc vào custom vertex layers (`BT_ORIGINAL_CO`, `BT_ORIGINAL_UV`) để có thể reset.

---

## 6. Đăng ký Class & An toàn Trạng thái
* **Unregister**: Hủy đăng ký class theo thứ tự đảo ngược `reversed(classes)`; xóa các thuộc tính đã gắn vào `bpy.types.Scene.<prop_name>`.
* **Undo & Transaction**: Operator thay đổi mesh/scene phải khai báo `bl_options = {'REGISTER', 'UNDO'}`. Bọc file I/O trong `try...except` và báo lỗi qua `self.report({'ERROR'}, message)`.

---

## 7. Quét Dọn Mã Thừa (Dead Code Elimination)
Tuân thủ nghiêm ngặt [standards.md](standards.md) Mục 7: Quét sạch mã cũ, xóa bỏ các hàm helper không dùng, operator lỗi thời và các nhánh fallback khi triển khai giải pháp mới.
