# Block Blast - Shader & Material Optimization Guidelines

Quy chuẩn tối ưu hóa Shader, Material và vẽ khối (SRP Batcher / GPU Instancing) trên thiết bị di động.

---

## 1. Cấm Clone Material Runtime

**CẤM** truy cập `renderer.material` trong runtime code vì tạo Material instance và phá batching. Dùng shared material, `MaterialPropertyBlock` (MPB) hoặc GPU Instancing theo ma trận Mục 2. Với uGUI `Graphic`, dùng `Graphic.color` hoặc shared UI material; không coi MPB là API chung cho `Image`.

---

## 2. Chọn Đường Render

| Nhu cầu | Cách dùng | Ràng buộc |
| --- | --- | --- |
| Nhiều renderer dùng shared material, không cần override riêng | **SRP Batcher** | Shader phải tương thích; renderer không dùng MPB. |
| Override màu/flash/alpha theo từng renderer, không clone material | **MPB** | Chấp nhận renderer đó không đi qua SRP Batcher; đo bằng Profiler/Frame Debugger. |
| Nhiều mesh giống nhau với cùng material | **GPU Instancing** | Chỉ dùng khi shader/data hỗ trợ và phép đo chứng minh phù hợp hơn. |

Không tuyên bố lựa chọn nào nhanh hơn nếu chưa đo trên workload và thiết bị mục tiêu.

## 3. Quy trình MPB

Cache Shader ID; không lookup chuỗi trong update loop/tween:
```csharp
private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
```

Tái sử dụng MPB, không cấp phát liên tục:
```csharp
private static MaterialPropertyBlock mpbCache;
private static MaterialPropertyBlock MPB => mpbCache ??= new MaterialPropertyBlock();
```

```csharp
MPB.Clear();
renderer.GetPropertyBlock(MPB);
MPB.SetColor(EmissionColorId, emissionHdrColor);
MPB.SetColor(BaseColorId, tintColor);
renderer.SetPropertyBlock(MPB);
```

Khi trả pool, disable hoặc hủy animation, bắt buộc reset:
```csharp
renderer.SetPropertyBlock(null);
```
Không để override tồn tại trên object đã tắt hoặc tái chế.

## 4. Shader Compatibility & Keywords

* Custom shader phải tương thích URP SRP Batcher: đặt mọi material property trong `CBUFFER_START(UnityPerMaterial) ... CBUFFER_END` và tránh pass/property phá compatibility.
* CẤM gọi `material.EnableKeyword`/`DisableKeyword` hoặc `Shader.EnableKeyword` lúc chơi. Dùng numeric controls (`_FlashAmount`, `_EmissionIntensity`) thay runtime keyword để tránh variant stall và batch break.

---

## 5. Master Shader

Gameplay blocks và board dùng chung **1 Master Shader**, không tạo nhiều shader nhỏ lẻ. Các kênh chuẩn:
* `_BaseMap` / `_BaseColor`: Texture atlas hoặc tint color.
* `_EmissionColor` / `_EmissionIntensity`: Phát sáng, xung nhịp, quá tải điện.
* `_FlashColor` / `_FlashAmount`: Flash khi va chạm hoặc nổ.
* `_RimColor` / `_RimIntensity`: Hiệu ứng viền fresnel.
* `_DissolveAmount` / `_DissolveMap`: Phân rã, tan biến.
Master Shader giúp giảm state changes nhưng **không bảo đảm** 64 ô thành một batch/draw call; phải xác minh bằng Frame Debugger/Profiler.

---

## 6. Texture & Overdraw
* **Texture Atlasing**: UV của toàn bộ block models phải map vào bảng màu chung `palette.png` để gom Draw Call.
* **Debris Mesh**: Mảnh vỡ 3D (`Mesh_MicroDebris_*`) phải dùng đúng material có UV phù hợp; không ép texture atlas nguyên khối vào mesh vỡ vụn.
* **Giới hạn Overdraw**: Cắt gọn kích thước quad của hạt trong suốt (particles); tránh hiệu ứng phủ kín màn hình gây nghẽn băng thông GPU.
