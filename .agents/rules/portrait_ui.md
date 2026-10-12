# Block Blast - Nguyên tắc Thiết kế & Triển khai UI Portrait Mobile

Quy chuẩn nguồn cho thiết kế, xây dựng và tối ưu UI màn hình dọc (Portrait) trên di động (`LobbyScene`, `IntroScene`, `LoadingScreen`, HUD, popups, modals, overlays).

---

## 1. Phạm vi, Thuật ngữ & Thứ bậc Ưu tiên

### A. Thứ bậc Ưu tiên (Precedence)
Tuân thủ các rule kiến trúc liên quan:
* [architecture.md](architecture.md): Phân tách MVC; Controller quản lý intent/state, View thụ động hiển thị.
* [events.md](events.md): View $\leftrightarrow$ Controller dùng Pure C# Events; cấm SO Game Events khi chưa duyệt.
* [dotween.md](dotween.md): Quản lý lifecycle tween UI; cấm Coroutine cho animation.
* [asset_optimization.md](asset_optimization.md): Tối ưu Canvas, Font, Sprite Atlas, Raycast Targets, Draw calls.
* [shaders.md](shaders.md): Cấm `image.material` runtime; tái sử dụng Material và Sprite Batching.
Chỉ đạo trực tiếp của người dùng và [standards.md](standards.md) giữ quyền ưu tiên cao nhất theo [AGENTS.md](../../AGENTS.md).

### B. Thuật ngữ Chuẩn hóa
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
Mọi Canvas UI portrait phức tạp phải áp dụng cấu trúc 2 lớp phân tách:
```
[ Canvas (Screen Space) ] (CanvasScaler: 1080x1920)
   ├── [ Background Layer ] (Stretch Full Bleed: Anchors (0,0) to (1,1)) -> Art, Gradient, Ambient VFX
   └── [ SafeArea Container ] (RectTransform điều khiển bởi SafeArea script đọc Screen.safeArea)
         ├── [ Top Header Area ] (Anchors Top)
         ├── [ Main Content Area ] (Anchors Center/Stretch)
         └── [ Bottom Action / Nav Area ] (Anchors Bottom)
```
* **Background Full Bleed**: Stretch toàn màn hình tránh viền đen (letterboxing) trên $19.5:9$ và $20:9$.
* **SafeArea Container**: Chuyển đổi `Screen.safeArea` pixel sang `anchorMin`/`anchorMax` của RectTransform; xử lý thay đổi độ phân giải không rách layout.
* **Insets**: Chừa đệm Top Inset tránh notch/dynamic island; chừa đệm Bottom Inset tránh thanh Home Indicator.

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
2. **Phân tầng Canvas Sorting Order**:
   * Background Canvas: `0 - 5`
   * Gameplay / Main HUD: `10 - 20`
   * Overlay Panels / Side Drawers: `30 - 40`
   * Popup / Modal Dialogs: `50 - 60`
   * Toast / Floating Notifications: `80 - 90`
   * Loading Screen / Curtain: `100+`

---

## 4. Tương tác & Khả năng Tiếp cận

### A. Kích thước Touch Target Tối thiểu
* Hit area tối thiểu cho nút tương tác: **`88 × 88` px** trên Reference Resolution `1080 × 1920` ($\approx 44 \times 44$ pt iOS / $48 \times 48$ dp Android).
* Icon nhỏ ($< 88$ px, ví dụ nút X close $48 \times 48$ px): Mở rộng RectTransform hoặc thêm padding/transparent image để đạt tối thiểu $88 \times 88$ px.

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
* **UI View (`MonoBehaviour`)**: Lớp trình diễn; tham chiếu serialize UI components; phát **Pure C# Event** lên Controller:
  ```csharp
  public sealed class LobbyMainMenuView : MonoBehaviour
  {
      [SerializeField] private Button playButton;
      [SerializeField] private TextMeshProUGUI scoreText;
      public event Action PlayClicked;
      private void Awake() => playButton.onClick.AddListener(() => PlayClicked?.Invoke());
      public void UpdateScore(int score) => scoreText.text = score.ToString("#,##0");
  }
  ```
  *Cấm View tự ý sửa Model, tính toán điểm số hay can thiệp luật chơi.*
* **UI Controller (`Pure C#` hoặc `Coordinator MonoBehaviour`)**: Nhận event từ View/ServiceLocator, quản lý state, chuẩn bị dữ liệu đã format rồi truyền cho View.
* **Quy chuẩn Event**: View $\leftrightarrow$ Controller dùng **Pure C# Event (`event Action`)**. **Cấm tuyệt đối** dùng SO Game Events nếu chưa có sự phê duyệt tường minh.

### B. Quy chuẩn DOTween cho UI
* **Cấm Coroutine**: Mọi chuyển động UI **BẮT BUỘC** dùng DOTween.
* **Kill trước khi Tween**: Luôn gọi `target.DOKill()` trước khi tạo tween mới.
* **Target & Link bắt buộc**:
  ```csharp
  Sequence seq = DOTween.Sequence();
  seq.SetTarget(panelRect);
  seq.SetLink(gameObject, LinkBehaviour.KillOnDisable);
  seq.Append(panelRect.DOScale(1f, 0.25f).From(0.8f).SetEase(Ease.OutBack));
  seq.Join(canvasGroup.DOFade(1f, 0.2f).From(0f));
  ```
* **Khóa tương tác khi chuyển cảnh**: Tắt `canvasGroup.blocksRaycasts = false` trong lúc tween, mở lại ở `OnComplete`.
* **Phục hồi OnDisable**: Reset visual về trạng thái chuẩn (`scale = Vector3.one`, `alpha = 1f`).

---

## 6. Hiệu năng & Tối ưu Hoá Tài nguyên (Asset Hygiene)

* **Canvas Slicing**: Tách riêng **Static Canvas** (nền, logo, khung cố định) và **Dynamic Canvas / Sub-Canvas** (điểm số, progress bar, timer) để cô lập vùng re-batch.
* **Tắt Raycast Target**: Bỏ tích `Raycast Target` trên toàn bộ `Image`, `RawImage`, `TextMeshProUGUI` không nhận click trực tiếp (background, icon trang trí, nhãn chữ).
* **Giảm Transparent Overdraw**: Hạn chế xếp chồng lớp sprite trong suốt full-screen; ưu tiên nền opaque.
* **Gom Sprite Atlas & Font**: Toàn bộ icon, nút của một màn hình phải nằm trong **1 Sprite Atlas duy nhất** (1 Draw Call); tái sử dụng chung 1 font TMP asset.
* **Shader/Material**: **Cấm truy cập `image.material` ở runtime** (gây rò rỉ RAM và hỏng SRP Batching); dùng `MaterialPropertyBlock` hoặc `Graphic.color`.

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

## 8. Bàn giao & Ghi nhớ khi Thực hiện Task UI

1. Đọc tài liệu này trước khi dựng scene hoặc code UI portrait (`LobbyScene`, `IntroScene`, `LoadingScreen`).
2. Mọi nhận định về draw calls, FPS hoặc RAM phải dựa trên đo đạc thực tế từ Unity Profiler / Frame Debugger.
3. Không chỉnh sửa file rule này nếu chưa có yêu cầu cụ thể từ người dùng.
