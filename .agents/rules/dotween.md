# Block Blast - DOTween Management Guidelines

Quy chuẩn quản lý DOTween ngăn ngừa rò rỉ bộ nhớ, `MissingReferenceException`, stale callbacks và lệch tọa độ Transform:

---

## 1. Bắt buộc DOTween cho View & UI (Cấm Coroutine)
* Mọi hoạt ảnh, Game Juice và chuyển động UI (ScoreView, GridView, đếm số, nảy nút, mở popup) **BẮT BUỘC** dùng DOTween (`Tween` hoặc `Sequence`).
* **CẤM TUYỆT ĐỐI**: Dùng Unity Coroutines cho animation/visual UI.
* **Cổng phê duyệt ngoại lệ**: Chỉ được dùng Coroutine cho animation khi có sự phê duyệt rõ ràng từ người dùng.

---

## 2. Kill Trước Khi Tạo Tween Mới (Kill Before Tweening)
Luôn dọn dẹp tween cũ trên target trước khi chạy tween mới:
```csharp
target.DOKill(); // hoặc: tweenInstance?.Kill();
```

---

## 3. Ràng buộc Vòng đời Bắt buộc (`OnDisable` / `OnDestroy`)
Mọi component kích hoạt tween BẮT BUỘC liên kết vòng đời với GameObject:
```csharp
.SetLink(gameObject, LinkBehaviour.KillOnDisable); // hoặc LinkBehaviour.KillOnDestroy
```

---

## 4. An toàn Object Pooling & Khôi phục Canonical Transform
Khi trả GameObject về Pool (`IPoolService.ReturnObjectToPool`) hoặc hủy hiệu ứng preview:
1. Kill toàn bộ tween: `target.DOKill();`
2. Đưa các thuộc tính biến đổi về giá trị chuẩn mực ban đầu:
   ```csharp
   target.localScale = Vector3.one;
   target.position = defaultPosition;
   target.rotation = Quaternion.identity;
   ```
3. Tuyệt đối không để tween tiếp tục chạy ngầm trên object đang disable hoặc nằm trong pool.

---

## 5. ScriptableObject Phi Trạng Thái (Stateless SOs)
ScriptableObjects (ví dụ: `PreClearEffectSO`) **CẤM TUYỆT ĐỐI** lưu trữ tham chiếu active `Tween`, `Transform` hoặc từ điển runtime state trong instance fields. SO phải thuần túy là bộ cấu hình và thực thi phi trạng thái.

---

## 6. Theo dõi & Dập tắt Vòng lặp Vô hạn (Infinite Loops)
Bất kỳ tween nào dùng loop vô hạn (`.SetLoops(-1)`) BẮT BUỘC phải được lưu vết và lập tức kill (`target.DOKill()`) ngay khi trạng thái kích hoạt kết thúc (ví dụ hủy preview đặt khối).

---

## 7. Cấm Cấp phát Tween Trùng lặp trong Update
Tránh tạo mới tween liên tục trong `Update()` mà không kiểm tra `DOTween.IsTweening(target)` hoặc cache lại biến tham chiếu `Tween`.

---

## 8. Bắt buộc Gán Target cho Sequence (`.SetTarget`)
`DOTween.Sequence()` mặc định có `target == null`. Nếu thiếu `.SetTarget(target)`, lệnh `target.DOKill()` chỉ kill các tween con bên trong mà **bỏ sót lớp bọc Sequence bên ngoài**, gây rò rỉ callback và pool nội bộ.
* **Quy tắc Bắt buộc**: Luôn gán target và link khi tạo Sequence:
  ```csharp
  Sequence seq = DOTween.Sequence();
  seq.SetTarget(target);
  seq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
  ```
