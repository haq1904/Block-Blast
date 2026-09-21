---
trigger: always_on
description: Strict DOTween management guidelines, coroutine ban for animations, and transform lifecycle safety.
---

# Block Blast - DOTween Management Guidelines

DOTween is widely used for Game Juice, animations, and transitions. To prevent memory leaks, `MissingReferenceException`, stale callbacks, and transform desynchronization, you MUST adhere to the following rules:

## 1. Mandatory Tweening for View & UI (Coroutine Ban)
*   All animations, visual game juice, and UI transitions (e.g., `ScoreView`, `GridView`, number rolling, punch scale, screen transitions) MUST be implemented using DOTween (`Tween` or `Sequence`).
*   **Strict Prohibition**: Using Unity Coroutines for visual/UI animations is STRICTLY PROHIBITED.
*   **Exception Gate**: If an exceptional edge case strictly requires a Coroutine for animation or timing, you MUST ask the user for explicit approval first.

## 2. Kill Before Tweening
*   Always kill any active tween on a target before starting a new one:
    ```csharp
    target.DOKill();
    // or: tweenInstance?.Kill();
    ```

## 3. Mandatory Cleanup (`OnDisable` / `OnDestroy`)
*   Any component launching tweens MUST kill them in `OnDisable()` or `OnDestroy()`.
*   Always bind tweens to their GameObject lifecycle using:
    ```csharp
    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    // or: .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    ```

## 4. Object Pooling & Canonical Transform Safety
When returning a GameObject or Transform to the Object Pool (`IPoolService.ReturnObjectToPool`), or canceling a preview effect:
1. Kill all active tweens on that object (`target.DOKill()`).
2. Reset all modified properties (scale, position, rotation, alpha) back to their canonical default values:
   ```csharp
   target.localScale = Vector3.one;
   target.position = defaultPosition;
   target.rotation = Quaternion.identity;
   ```
3. Never leave an active tween running on a disabled or pooled object.

## 5. Stateless ScriptableObjects
*   ScriptableObjects (e.g., `PreClearEffectSO`) must NEVER store active `Tween` references, `Transform` references, or runtime state dictionaries in instance fields.
*   ScriptableObjects must remain purely stateless configurators and executors.

## 6. Infinite Loops Tracking
*   Any tween configured with infinite loops (`.SetLoops(-1)`) MUST be tracked and explicitly killed (`target.DOKill()`) immediately when its trigger state ends (e.g., canceling pre-clear preview).

## 7. No Redundant Allocations in Update
*   Avoid instantiating new tweens inside `Update()` without checking `DOTween.IsTweening(target)` or caching the active `Tween` reference.

## 8. Mandatory Sequence Target Binding (`.SetTarget(target)`)
*   Unlike individual tweens (e.g., `target.DOScale(...)`) where DOTween automatically assigns `target` as the tween owner, a `Sequence` created via `DOTween.Sequence()` has `target == null` by default.
*   If `.SetTarget(target)` is omitted: calling `target.DOKill()` will ONLY kill individual inner tweens, but will **orphan the outer `Sequence` wrapper**, allowing its intervals and callbacks to linger, leak in internal pools, or fire on recycled objects.
*   **Mandatory Rule**: Whenever creating a `Sequence` for a specific GameObject, Transform, or UI component, you MUST explicitly bind its target:
    ```csharp
    Sequence seq = DOTween.Sequence();
    seq.SetTarget(target);
    seq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
    ```

