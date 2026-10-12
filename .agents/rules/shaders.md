# Block Blast - Shader & Material Optimization Guidelines

Quy chuẩn tối ưu hóa Shader, Material và vẽ khối (SRP Batcher / GPU Instancing) trên thiết bị di động.

---

## 1. Cấm Tuyệt đối Clone Material Runtime (`renderer.material` Ban)
* **CẤM TUYỆT ĐỐI**: Truy cập `renderer.material` trong runtime code (animation, đổi màu, hover, tweens, visual feedback).
  ```csharp
  // STRICTLY PROHIBITED - Tạo Material (Instance) rò rỉ RAM và phá vỡ SRP Batching:
  renderer.material.color = Color.white;
  ```
* **Bắt buộc**: Luôn sử dụng **`MaterialPropertyBlock` (MPB)** để thay đổi thuộc tính hiển thị mà không nhân bản Material.

---

## 2. Quy trình Chuẩn với MaterialPropertyBlock (MPB)

### A. Cache Static Shader ID
Không dùng chuỗi string trong update loops hoặc tweens. Luôn cache ID tĩnh:
```csharp
private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
```

### B. Tái sử dụng Instance MPB Chung
Không cấp phát `new MaterialPropertyBlock()` liên tục; dùng chung một static instance:
```csharp
private static MaterialPropertyBlock mpbCache;
private static MaterialPropertyBlock MPB => mpbCache ??= new MaterialPropertyBlock();
```

### C. Pattern Áp dụng Chuẩn
```csharp
MPB.Clear();
renderer.GetPropertyBlock(MPB);
MPB.SetColor(EmissionColorId, emissionHdrColor);
MPB.SetColor(BaseColorId, tintColor);
renderer.SetPropertyBlock(MPB);
```

### D. Reset Bắt buộc khi Trả Pool / Disable
Khi GameObject được trả về Object Pool (`IPoolService.ReturnObjectToPool`), bị disable hoặc hủy animation:
```csharp
renderer.SetPropertyBlock(null);
```
Không để sót override thuộc tính trên đối tượng đã tắt hoặc tái chế.

---

## 3. Tương thích URP SRP Batcher & GPU Instancing

### A. Chuẩn SRP Batcher
Mọi custom shader (Shader Graph hoặc HLSL) BẮT BUỘC tương thích với **URP SRP Batcher**:
* Mọi material property phải khai báo bên trong Constant Buffer: `CBUFFER_START(UnityPerMaterial) ... CBUFFER_END`.
* Tránh các pass không tương thích SRP Batcher hoặc property toàn cục phá vỡ batch.

### B. Cấm Bật/Tắt Shader Keyword ở Runtime (Runtime Keyword Ban)
* **CẤM** gọi `material.EnableKeyword` / `DisableKeyword` hoặc `Shader.EnableKeyword` trong lúc chơi.
* Việc bật/tắt keyword gây biên dịch biến thể shader (variant stall) và làm vỡ batching.
* Dùng các slider số thực (`_FlashAmount: 0..1`, `_EmissionIntensity: 0..N`) để điều tiết hiệu ứng mượt mà thay vì phân nhánh keyword.

---

## 4. Kiến trúc Master Shader Thống nhất
Không tạo hàng chục shader nhỏ lẻ. Toàn bộ gameplay blocks và bảng chơi sử dụng chung **1 Master Shader** với các kênh chuẩn:
* `_BaseMap` / `_BaseColor`: Texture atlas hoặc tint color.
* `_EmissionColor` / `_EmissionIntensity`: Phát sáng, xung nhịp, quá tải điện.
* `_FlashColor` / `_FlashAmount`: Flash khi va chạm hoặc nổ.
* `_RimColor` / `_RimIntensity`: Hiệu ứng viền fresnel.
* `_DissolveAmount` / `_DissolveMap`: Phân rã, tan biến.
*Chia sẻ chung Master Shader cho phép toàn bộ 64 ô lưới được render trong **đúng 1 SRP Batch**.*

---

## 5. Atlas Hóa Texture & Giới hạn Overdraw Di động
* **Texture Atlasing**: UV của toàn bộ block models phải map vào bảng màu chung `palette.png` để gom Draw Call.
* **Debris Mesh**: Mảnh vỡ 3D (`Mesh_MicroDebris_*`) phải dùng đúng material có UV phù hợp; không ép texture atlas nguyên khối vào mesh vỡ vụn.
* **Giới hạn Overdraw**: Cắt gọn kích thước quad của hạt trong suốt (particles); tránh hiệu ứng phủ kín màn hình gây nghẽn băng thông GPU.
