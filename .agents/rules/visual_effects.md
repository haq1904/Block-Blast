# Block Blast - Visual Feedback & Strategy Pattern Guidelines

Quy chuẩn kiến trúc ScriptableObject Strategy Pattern cho hiệu ứng hình ảnh (Placement, Pre-Clear, Clear effects). Phân định nghiêm ngặt trách nhiệm qua 4 tầng:

```
[ Scene GridView ] (Humble Scene Holder & Dispatcher)
   ├──> [ ClearStaggerSO ] (Wave Orchestrator: Timing, Rhythm & Prop Spawning)
   ├──> [ ClearPropBase ] (Prop Choreographer: Movement, Finite Spin, Alpha & VFX)
   └──> [ ClearAnimationSO ] (Cell Specialist: Mesh Morphing & 3-Tier Debris)
```

---

## 1. Animation Strategy (`ClearAnimationSO`, `PreClearAnimationSO`, `PlacementAnimationSO`)
* **Vai trò**: Chuyên trách Theme & Cell (Xác định **CÁI GÌ** xảy ra trên cell mesh và asset theo chủ đề).
* **Trách nhiệm cốt lõi**:
  1. **Mesh Morphing**: Điều khiển `Transform` của cell bằng DOTween (scale punch, squeeze, jump, twist, dissolve).
  2. **VFX 3 Tầng**: Bao đóng 3 tầng hạt theo theme (`layer1_BurstPrefab`, `layer2_DynamicsPrefab`, `layer3_DebrisPrefab`) và flash shader.
  3. **Cung cấp Prop theo Theme**: Chứa tham chiếu prop (`ClearPropBase`, ví dụ SawBlade, Pickaxe, Drill); hỗ trợ chọn đơn hoặc ngẫu nhiên.
* **Điều cấm nghiêm ngặt**:
  * **CẤM** tính toán timing, delay lan truyền sóng hoặc quỹ đạo quét dòng.
  * **CẤM** khởi tạo hoặc điều khiển prop quét ngang/dọc qua các hàng/cột.
  * **CẤM** duyệt ma trận grid 8x8.

---

## 2. Stagger Strategy (`ClearStaggerSO`, `SequentialForwardStaggerSO`,...)
* **Vai trò**: Điều phối Sóng & Nhịp điệu (Xác định **KHI NÀO & CÁCH THỨC** làn sóng hình ảnh lan truyền).
* **Trách nhiệm cốt lõi**:
  1. **Nhịp điệu thời gian**: Tính toán độ trễ từng ô (`CalculateDelay`) theo thuật toán sóng (Sequential, Center-Outward, Checkerboard, Random, Instant).
  2. **Điều phối Sóng & Prop (`Play`)**:
     ```csharp
     public virtual void Play(
         List<ClearCellItem> lineCells, 
         ClearAnimationSO clearAnimation, 
         IPoolService poolService,
         Action<Vector2Int> onCellExploded)
     ```
     * **Ủy quyền Prop**: Spawn prop qua `IPoolService`, tính tọa độ đầu/cuối và hướng (`dir`), ủy quyền chuyển động cho `propData.AnimatePropSequence(prop, firstPos, lastPos, dir, sweepDuration, leadOffset, poolService)`.
     * **Phát nổ Cell**: Duyệt `lineCells`, gán delay, kích hoạt `clearAnimation.Play(...)`. Khi nổ: spawn step VFX (`stepParticlePrefabs`), khôi phục transform chuẩn, trả block về pool, và gọi `onCellExploded(gridPos)` để giải phóng ô trong `GridModel`.
* **Điều cấm**:
  * **CẤM** lưu trữ tham chiếu `Tween`, `Transform`, `GameObject` trong instance fields của SO (Stateless SO).
  * Base class mặc định phải là virtual no-op an toàn nếu hiệu ứng không có prop.

---

## 3. Prop Strategy (`ClearPropBase`) & Kiến trúc Pure-Prefab
* **Vai trò**: Biên đạo chuyển động Prop (Xác định **CÁCH** đạo cụ di chuyển và tương tác).
* **Trách nhiệm cốt lõi**:
  1. **Pure Prefab Mandate**: Prop prefabs (`Axe.prefab`, `SawBlade.prefab`) là visual thuần túy, CHỈ chứa Unity standard components (`Transform`, `MeshFilter`, `MeshRenderer`, `ParticleSystem`) và `AutoReturnToPool`. **CẤM TUYỆT ĐỐI** gắn MonoBehaviour tùy biến chuyên biệt lên prop (KHÔNG tạo `AxePropFeedback`, `SawBladeSpinner`).
  2. **Stateless SO Choreographer**: Mọi hành vi (tịnh tiến, góc nghiêng, xoay, rung lắc, kích hoạt hạt, fade alpha) do subclass `ClearPropBase` điều khiển qua DOTween trong `AnimatePropSequence()`. SO không lưu runtime state.
  3. **Vòng đời hữu hạn (Cấm Loop vô hạn)**: Xoay liên tục (SawBlade) phải tính góc quay hữu hạn theo tổng thời gian (`totalDuration = entry + sweep + exit`), **CẤM dùng `.SetLoops(-1)`**.
   4. **MaterialPropertyBlock**: Fade alpha tuân thủ [shaders.md](shaders.md) (dùng static cached `MaterialPropertyBlock`, không clone material; reset `renderer.SetPropertyBlock(null)` khi trả pool hoặc khi `alpha >= 0.999f`).
   5. **Helper dùng chung trong Base**: `SetPropAlpha(GameObject prop, float alpha)` và `SetPropParticles(GameObject prop, bool play, Vector3? sprayDir)`.

---

## 4. View Layer (`GridView`) - Humble Dispatcher
* **Vai trò**: Quản lý Scene & Điều phối thụ động.
* **Trách nhiệm cốt lõi**:
  1. Sở hữu instance scene (`visualGrid[col, row]`).
  2. **Gỡ khỏi ma trận ngay lập tức**: Đặt `visualGrid[col, row] = null` ngay khi bắt đầu nổ để giải phóng logic gameplay.
  3. **Ủy quyền hoàn toàn**: Đóng gói danh sách ô vào `List<ClearCellItem>` và gọi một lệnh duy nhất:
     ```csharp
     stagger?.Play(lineCells, clearEffect, poolService, ReleaseCellOnGrid);
     ```
  4. **Dọn dẹp an toàn (`OnDisable`)**: Quản lý danh sách `activeClearingBlocks` để lập tức kill tween, phục hồi transform chuẩn và trả block về pool nếu unload/reset scene giữa chừng.
* **Điều cấm (Manual Wiring Ban)**:
  * **CẤM** chạy vòng lặp `for` thủ công để tự nổ từng cell trong View.
  * **CẤM** tự ráp nối các loose particle prefabs riêng lẻ từ theme, stagger và clearAnimation trong View.
  * **CẤM** tự tính toán đường bay prop hoặc DOTween paths trong View.
  * **CẤM** phát âm thanh đơn (single-shot flat audio) ở đầu hàng khi hiệu ứng nổ lan truyền theo nhiều bước trễ.

---

## 5. Quy tắc An toàn Multi-Line & Cross-Clear
1. **Waypoints vs Real GameObjects**: Khi 2 đường cắt nhau, cung cấp đủ 8 waypoints cho cả 2 đường để prop quét hết từ biên sang biên. Nhưng GameObject khối gạch tại tọa độ giao điểm `(col, row)` **CHỈ ĐƯỢC PHÉP NỔ ĐÚNG 1 LẦN** (tránh lỗi double-return trong `IPoolService`).
2. **Object Pool & DOTween Safety**:
   * Mọi prop spawn ra BẮT BUỘC có component `AutoReturnToPool` làm timeout an toàn.
   * Mọi tween trên cell hoặc prop BẮT BUỘC tuân thủ [dotween.md](dotween.md) (gán `.SetTarget(obj)` và `.SetLink(obj, LinkBehaviour.KillOnDisable)`).
