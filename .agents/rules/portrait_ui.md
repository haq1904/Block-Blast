# Block Blast - Nguyên tắc Thiết kế & Triển khai UI Portrait Mobile

Quy chuẩn nguồn cho thiết kế, xây dựng và tối ưu UI màn hình dọc (Portrait) trên di động (`LobbyScene`, `IntroScene`, `LoadingScreen`, HUD, popups, modals, overlays).

---

## 1. Phạm vi & Thuật ngữ

Thứ tự ưu tiên theo [plans_and_tools.md](plans_and_tools.md). Phần code/UI phải đồng thời tuân thủ [architecture.md](architecture.md), [events.md](events.md), [dotween.md](dotween.md), [asset_optimization.md](asset_optimization.md) và [shaders.md](shaders.md).
* **Portrait Orientation**: Tỉ lệ màn hình dọc ($W/H < 1.0$), bao gồm $9:16$ (chuẩn), $19.5:9, 20:9$ (smartphone dài) và $3:4, 2:3$ (tablet).
* **SafeArea**: Vùng hiển thị/tương tác an toàn sau khi trừ notch, Dynamic Island, góc bo và thanh Home Indicator.
* **Reference Resolution**: Chuẩn Canvas **`1080 × 1920`** ($9:16$).
* **Full Bleed**: Mỹ thuật nền (Background sprite, Gradient, Vignette) stretch tràn viền $(0,0) \rightarrow (1,1)$, vượt ngoài SafeArea.
* **Interactive Region**: Toàn bộ thành phần tương tác (Buttons, Inputs, Toggles, Sliders, CTAs) **BẮT BUỘC** nằm trọn trong SafeArea.
* **Visual Hierarchy**: Phân cấp thị giác: Primary CTA $\rightarrow$ Status/Title $\rightarrow$ Secondary Actions $\rightarrow$ Bottom Nav.

---

## 2. Quy tắc Layout & Responsive Multi-Screen

### A. Cấu hình Chuẩn `CanvasScaler`
1. **UI Scale Mode**: `Scale With Screen Size`.
2. **Reference Resolution**: `X = 1080, Y = 1920`.
3. **Screen Match Mode**: `Match Width Or Height`.
4. **Match Value**:
   * Mặc định: `Match = 0` (Match Width) cho smartphone portrait.
   * Tablet ($3:4, 2:3$): Điều chỉnh linh hoạt `Match = 0` đến `0.5` hoặc dùng layout co giãn thích ứng để không tràn chiều dọc.
5. **Reference Pixels Per Unit**: `100`.

### B. Kiến trúc 2 Lớp SafeArea
Canvas portrait phức tạp phải tách:

- `Background Layer`: stretch full screen `(0,0)→(1,1)` cho art/gradient/VFX, không letterbox trên `19.5:9`/`20:9`.
- `SafeArea Container`: script đổi `Screen.safeArea` pixel thành `anchorMin/anchorMax`; chứa Top Header, Main Content và Bottom Action/Nav, phản ứng khi resolution đổi và chừa top/bottom insets.

### C. Kiểm soát Layout Group & Hiệu năng
1. **Giới hạn lồng ghép**: Tối đa 2 tầng `VerticalLayoutGroup`/`HorizontalLayoutGroup` đi kèm `ContentSizeFitter` (tránh tính toán đệ quy $O(N^2)$).
2. **Ưu tiên RectTransform tĩnh**: Panel cố định (Header, Footer, Popup Card) dùng Anchor Min/Max và Offset; chỉ dùng `LayoutGroup` cho danh sách động (Items, Shop, Leaderboard).
3. **Cấm Force Rebuild trong Update**: Tuyệt đối KHÔNG gọi `LayoutRebuilder.ForceRebuildLayoutImmediate()` trong `Update()`, `LateUpdate()` hoặc callback tần suất cao.

---

## 3. Thứ bậc Thị giác & Trải nghiệm Người chơi

### A. Nguyên tắc 1 Primary CTA Duy nhất
Mỗi màn hình/popup chỉ có **tối đa một nút hành động chính (Primary CTA)** có kích thước lớn nhất, tương phản mạnh nhất (ví dụ "PLAY", "START", "CLAIM"). Nút phụ (Settings, Shop) dùng màu dịu hơn, cỡ nhỏ hơn hoặc outline style.

### B. Phân vùng Dọc Tham khảo (One-handed Thumb Zone)
| Vùng (Zone) | Chiều cao | Thành phần tiêu biểu | Tính chất tương tác |
| :--- | :---: | :--- | :--- |
| **Top Zone** | $\approx 10\% - 15\%$ | Avatar, Currency, Settings, Title | Vùng khó với; thông tin quan sát, nút ít bấm. |
| **Middle Zone** | $\approx 50\% - 60\%$ | Bàn cờ, 3D Hero/Cube, Banner, Cards | Vùng quan sát trọng tâm; nội dung chính. |
| **Action Zone** | $\approx 20\% - 25\%$ | Primary CTA, Nút Start, Khay khối gạch | Vùng ngón cái chạm tự nhiên nhất; nút quan trọng. |
| **Bottom Nav** | $\approx 8\% - 12\%$ | Tab bar điều hướng (Home, Shop) | Vùng chân màn hình; đặt trên đệm Home Indicator. |

### C. Ngăn Chặn Bấm Xuyên (Input Bleed) & Canvas Sorting
1. **Lớp Chắn Popup/Modal**: Mọi popup/modal BẮT BUỘC có lớp dimmer phủ toàn màn hình chứa `GraphicRaycaster` và `Image` (`alpha 0.5 - 0.7`) với `Raycast Target = true` để chặn tương tác xuyên xuống dưới.
2. **Canvas Sorting Order**: Background `0–5`; Gameplay/HUD `10–20`; Overlay/Drawer `30–40`; Popup/Modal `50–60`; Toast `80–90`; Loading/Curtain `100+`.

---

## 4. Tương tác & Khả năng Tiếp cận

### A. Kích thước Touch Target Tối thiểu
Hit area tối thiểu: **`88 × 88` px** tại reference `1080 × 1920` ($\approx 44 × 44$ pt iOS / $48 × 48$ dp Android). Icon nhỏ phải mở rộng RectTransform hoặc dùng transparent hit area đủ chuẩn.

### B. Trạng thái Nút & Phản hồi Trực quan
* Các trạng thái bắt buộc: `Normal` (tĩnh), `Pressed` (thu nhỏ $0.95 \times$ hoặc tối màu), `Disabled` (alpha $0.4 - 0.5$ hoặc xám, `interactable = false`).
* Sử dụng DOTween punch scale nhẹ (`transform.DOPunchScale(...)`) khi bấm thành công.

### C. TextMeshPro (TMP) Responsive
1. **Auto-Sizing có giới hạn**: Bật `Auto Size` với khoảng Min/Max hợp lý (ví dụ: `Min: 24, Max: 48`).
2. **Text Wrapping & Truncation**: Cấu hình `Wrapping = Enabled` và `Overflow = Truncate` hoặc `Ellipsis`; cấm để text tràn khung.
3. **Tương phản Thị giác**: Dùng text outline hoặc underlay shadow khi đặt text trên nền phức tạp.

---

## 5. Kiến trúc Code & Animation UI

### A. Tuân thủ MVC & Giao tiếp Sự kiện
* **UI View (`MonoBehaviour`)**: Chỉ giữ serialized UI references, hiển thị và phát Pure C# Event lên Controller; cấm sửa Model, tính điểm hoặc can thiệp luật chơi.
* **UI Controller (`Pure C#` hoặc coordinator `MonoBehaviour`)**: Nhận event từ View/ServiceLocator, quản lý state, format dữ liệu rồi truyền cho View.
* View $\leftrightarrow$ Controller dùng `event Action` theo [events.md](events.md); GameEvent SO cần người dùng duyệt.

### B. Quy chuẩn DOTween cho UI
* **Vòng đời & An toàn Tween**: Tuân thủ nghiêm ngặt [dotween.md](dotween.md) (bắt buộc DOTween, cấm Coroutine, `target.DOKill()` trước khi tween, `.SetTarget()` và `.SetLink(gameObject, LinkBehaviour.KillOnDisable)`).
* **Khóa tương tác khi chuyển cảnh**: Tắt `canvasGroup.blocksRaycasts = false` trong lúc tween, mở lại ở `OnComplete`.
* **Phục hồi OnDisable**: Reset visual về trạng thái chuẩn (`scale = Vector3.one`, `alpha = 1f`).

---

## 6. Hiệu năng & Tối ưu Hoá Tài nguyên (Asset Hygiene)

* **Canvas Slicing**: Tách riêng **Static Canvas** (nền, logo, khung cố định) và **Dynamic Canvas / Sub-Canvas** (điểm số, progress bar, timer) để cô lập vùng re-batch.
* **Tắt Raycast Target**: Bỏ tích `Raycast Target` trên toàn bộ `Image`, `RawImage`, `TextMeshProUGUI` không nhận click trực tiếp (background, icon trang trí, nhãn chữ).
* **Giảm Transparent Overdraw**: Hạn chế xếp chồng lớp sprite trong suốt full-screen; ưu tiên nền opaque.
* **Sprite Atlas & Font**: Gom icon/nút cùng màn hình vào atlas phù hợp và tái sử dụng font TMP. Đây là mục tiêu giảm state change/draw call, không bảo đảm `1 Draw Call`; xác minh bằng Frame Debugger.
* **Shader/Material**: Cấm tạo material instance runtime. Với uGUI ưu tiên `Graphic.color` hoặc shared UI material; xem [shaders.md](shaders.md).

---

## 7. Bảng Kiểm Tra Nghiệm Thu UI Portrait

| Hạng mục kiểm tra | Tiêu chuẩn đạt | Phương pháp kiểm tra |
| :--- | :--- | :--- |
| **Tương thích $9:16$ (Baseline)** | Layout cân đối tại $1080 \times 1920$, không méo hình, đúng anchor. | Device Simulator $1080 \times 1920$. |
| **Màn hình dài ($19.5:9, 20:9$)** | Background Full Bleed không viền đen; nút/text trọn trong SafeArea. | Device Simulator (iPhone 14/15, S22). |
| **Màn hình rộng / Tablet ($3:4$)** | Không bị cắt cụt đầu/đáy; tương tác không ép sát nhau. | Device Simulator (iPad 9.7"). |
| **Chống Bấm Xuyên** | Mở Popup/Modal không click được các nút nền phía sau. | Click thử ngoài popup trong PlayMode. |
| **Touch Target Size** | Mọi nút có hit area $\ge 88 \times 88$ px trên reference resolution. | Kiểm tra Inspector & click thử. |
| **DOTween Safety** | Đóng/mở panel hoặc chuyển scene không sinh `MissingReferenceException`. | Kiểm tra Unity Console. |
| **Raycast Target Sweep** | Image/Text trang trí đã tắt `Raycast Target`. | Rà soát Inspector Prefab. |
| **Compile & Diagnostics** | Không có lỗi biên dịch hoặc cảnh báo phát sinh. | Unity Console (`read_console`). |

---

## 8. Bàn giao

Đọc rule trước khi dựng/code UI portrait. Claim về draw calls, FPS hoặc RAM phải có số liệu Unity Profiler/Frame Debugger. Không sửa rule khi chưa được người dùng yêu cầu.
