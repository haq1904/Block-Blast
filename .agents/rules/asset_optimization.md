# Block Blast - Asset Optimization Requirements

Quy chuẩn bắt buộc về rà soát và tối ưu hóa mọi tài nguyên tạo mới, nhân bản, hoặc import vào dự án (Mesh, Texture, Material, Prefab, VFX, Audio, UI/Font, Scene, ScriptableObject, Scripts).

---

## 1. Phạm vi & Quy trình 5 Bước Bắt buộc
Mọi asset BẮT BUỘC phải được tối ưu cho mục đích sử dụng trước khi bàn giao task:
1. **Trước khi tạo**: Kiểm tra khả năng tái sử dụng asset hiện có; xác định loại asset, nền tảng, góc nhìn camera và số lượng instance đồng thời.
2. **Thiết lập ngân sách theo loại**: Mesh dùng triangle budget trong [modeling.md](modeling.md); texture dùng resolution/memory; VFX dùng particle/overdraw; audio dùng channel/bitrate/duration; script/tool dùng CPU, GC và I/O. Nếu chưa có mốc chuẩn, đề xuất ngân sách có lý do.
3. **Tối ưu nguồn & Import Settings**: Loại bỏ dữ liệu thừa trong file nguồn; cấu hình import settings phù hợp nền tảng di động.
4. **Xác minh kết quả import**: Kiểm tra tham chiếu, cài đặt import và chạy thử với số lượng instance dự kiến.
5. **Báo cáo bằng chứng**: Báo cáo rõ công việc tối ưu đã làm, kết quả đo đạc thực tế và các giới hạn kiểm thử (nếu có).

---

## 2. Yêu cầu Cụ thể theo Loại Asset

| Loại Asset | Yêu cầu Rà soát & Tối ưu hóa |
| :--- | :--- |
| **Mesh / Model** | Tuân thủ ngân sách tam giác ([modeling.md](modeling.md)); xóa hình học khuất và slot material thừa; kiểm tra UVs, pivot, normals, colliders; tắt animation import không dùng. |
| **Texture / Sprite** | Khớp resolution với kích thước hiển thị; định dạng nén phù hợp; chỉ bật Alpha, Mipmaps, Read/Write khi thực sự cần; gom Sprite Atlas cho UI. |
| **Material / Shader** | Tái sử dụng material/shader; xóa pass thừa; **CẤM clone material runtime**; chọn SRP Batcher, MPB hoặc GPU Instancing theo [shaders.md](shaders.md) và đo thực tế. |
| **Prefab / VFX** | Xóa components/renderers/colliders thừa; giới hạn số lượng hạt (max particles) và thời gian sống (lifetime); kiểm soát transparent overdraw; sử dụng Object Pooling với cơ chế reset sạch sẽ. |
| **Audio** | Chọn Mono/Stereo, định dạng nén, sample rate và load mode phù hợp; giới hạn số lượng nguồn phát đồng thời. |
| **Animation / UI / Font** | Xóa keyframes/tracks thừa; tuân thủ lifecycle DOTween ([dotween.md](dotween.md)); tắt `Raycast Target` trên đồ họa tĩnh ([portrait_ui.md](portrait_ui.md)); tái sử dụng font atlas TMP. |
| **Scene** | Tối ưu ánh sáng, bóng đổ, post-processing và Canvas layout theo camera view thực tế. |
| **ScriptableObject / Data** | Tái sử dụng data; tách biệt cấu hình tĩnh với runtime state (Stateless SOs). |
| **Script / Tool** | Tránh lặp tính toán và cấp phát bộ nhớ (GC alloc) trong update loops; cache và giải phóng tài nguyên đúng chuẩn. |

---

## 3. An toàn Tham chiếu & Dự án Dùng chung
* **Bảo vệ GUID & Meta Files**: **CẤM TUYỆT ĐỐI xóa file `.meta`** để tạo lại GUID như một bước tối ưu.
* **Tài nguyên dùng chung**: Phối hợp trước khi sửa đổi Atlas, Material, Prefab hoặc Project Settings dùng chung.
* **Quyền can thiệp Editor**: Chỉ điều khiển Unity qua MCP trong phiên (turn) được phân công.
* **Chính sách kiểm thử**: Asset visual xác minh bằng inspection và visual check; unit tests chỉ áp dụng cho Model/Controller ([standards.md](standards.md)).

---

## 4. Báo cáo & Bằng chứng Hiệu năng
* Phân biệt rõ ràng giữa: Cấu hình tĩnh, quan sát trên Editor, và đo đạc thực tế trên thiết bị mục tiêu.
* **Cấm cam kết suông**: Không khẳng định giảm draw call hay đạt mốc FPS cụ thể nếu chưa có dữ liệu đo đạc từ Unity Profiler / Frame Debugger.
