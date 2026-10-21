using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(BlockController))]
public class BlockView : MonoBehaviour
{
    [Header("3D Tilt Inertia")]
    [Tooltip("Maximum tilt angle in degrees allowed along pitch (X) or roll (Z) axes.")]
    [SerializeField] private float maxTiltAngle = 18f;

    [Tooltip("Sensitivity multiplier for converting drag velocity into tilt angle.")]
    [SerializeField] private float tiltSensitivity = 0.8f;

    [Tooltip("Smoothing speed for interpolating rotation towards target tilt angle while dragging.")]
    [SerializeField] private float tiltSmoothSpeed = 12f;

    [Tooltip("Invert the pitch tilt angle (around X axis) if desired.")]
    [SerializeField] private bool invertPitch = false;

    [Tooltip("Invert the roll tilt angle (around Z axis) if desired.")]
    [SerializeField] private bool invertRoll = false;

    [Header("Settle Wobble")]
    [Tooltip("Total duration of the spring wobble settle animation when stopping hand or releasing.")]
    [SerializeField] private float wobbleDuration = 0.28f;

    [Tooltip("Proportion of current tilt angle to overshoot in the opposite direction during settle.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float wobbleOvershootRatio = 0.35f;

    [Tooltip("Velocity threshold below which the block is considered stationary and triggers settle wobble.")]
    [SerializeField] private float stopVelocityThreshold = 0.15f;

    [Header("Pick-up & Place Slide Animation")]
    [Tooltip("Duration of the fast slide up and scale animation when touching a block.")]
    [SerializeField] private float liftDuration = 0.10f;

    [Tooltip("Duration of the decisive slam down animation when placing a block on the grid.")]
    [SerializeField] private float placeDuration = 0.08f;

    [Header("Initial Spawn Entrance")]
    [Tooltip("Delay before the first tray batch starts rising after PlayScene becomes active.")]
    [SerializeField, Min(0f)] private float spawnEntranceDelay = 0.45f;

    [Tooltip("Additional delay between the center, left, and right tray shapes.")]
    [SerializeField, Min(0f)] private float spawnEntranceStagger = 0.08f;

    [Tooltip("World-space distance below the tray from which each initial shape rises.")]
    [SerializeField, Min(0f)] private float spawnEntranceRiseDistance = 1.25f;

    [Tooltip("Duration of the upward motion for each initial shape.")]
    [SerializeField, Min(0.01f)] private float spawnEntranceRiseDuration = 0.38f;

    [Tooltip("Duration of the final scale settle after each shape reaches the tray.")]
    [SerializeField, Min(0.01f)] private float spawnEntranceSettleDuration = 0.10f;

    [Tooltip("Scale multiplier used while each shape starts below the tray.")]
    [SerializeField] private Vector3 spawnEntranceStartScaleMultiplier = new Vector3(0.82f, 1.14f, 0.82f);

    [Tooltip("Scale multiplier at the top of the rise before the final settle.")]
    [SerializeField] private Vector3 spawnEntranceOvershootScaleMultiplier = new Vector3(1.06f, 0.94f, 1.06f);

    [Tooltip("Maximum roll angle used to give the left and right shape a subtle entrance tilt.")]
    [SerializeField, Min(0f)] private float spawnEntranceTiltAngle = 4f;

    private BlockController blockController;
    private IPoolService poolService;
    private IBlockService blockService;
    private readonly List<GameObject> activeCells = new List<GameObject>();

    private Quaternion targetRotation = Quaternion.identity;
    private bool isDragging;
    private bool isSettling;
    private Sequence settleSequence;

    private Tween liftMoveYTween;
    private Tween liftMoveZTween;
    private Tween liftScaleTween;
    private Tween placeTween;
    private Sequence spawnEntranceSequence;
    private bool isLifting;
    private bool isPlacing;

    private void Awake()
    {
        blockController = GetComponent<BlockController>();
        if (blockController != null)
        {
            blockController.OnShapeAssigned += DrawShape;
            blockController.OnDragVelocityUpdated += HandleDragVelocityUpdated;
            blockController.OnDragStarted += HandleDragStarted;
            blockController.OnBlockLiftRequested += HandleBlockLiftRequested;
            blockController.OnBlockPlaceSlideRequested += HandleBlockPlaceSlideRequested;
            blockController.OnDragEnded += HandleDragEnded;
            blockController.OnTrayPositionSet += HandleTrayPositionSet;
            blockController.OnDragPositionUpdated += HandleDragPositionUpdated;
            blockController.OnBlockReset += HandleBlockReset;
            blockController.OnSpawnEntranceRequested += HandleSpawnEntranceRequested;
        }
    }

    private void Update()
    {
        if (isDragging && !isSettling && !isPlacing)
        {
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                targetRotation,
                Time.deltaTime * tiltSmoothSpeed
            );
        }
    }

    private void OnDisable()
    {
        KillSpawnEntrance();
        KillLiftAndPlaceTweens();
        KillTiltTweens();
        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
        isDragging = false;
        isSettling = false;
        targetRotation = Quaternion.identity;
    }

    private void OnDestroy()
    {
        KillSpawnEntrance();
        KillLiftAndPlaceTweens();
        KillTiltTweens();

        if (blockController != null)
        {
            blockController.OnShapeAssigned -= DrawShape;
            blockController.OnDragVelocityUpdated -= HandleDragVelocityUpdated;
            blockController.OnDragStarted -= HandleDragStarted;
            blockController.OnBlockLiftRequested -= HandleBlockLiftRequested;
            blockController.OnBlockPlaceSlideRequested -= HandleBlockPlaceSlideRequested;
            blockController.OnDragEnded -= HandleDragEnded;
            blockController.OnTrayPositionSet -= HandleTrayPositionSet;
            blockController.OnDragPositionUpdated -= HandleDragPositionUpdated;
            blockController.OnBlockReset -= HandleBlockReset;
            blockController.OnSpawnEntranceRequested -= HandleSpawnEntranceRequested;
        }

        foreach (var cell in activeCells)
        {
            if (cell != null)
            {
                cell.transform.DOKill();
            }
        }
    }

    private void HandleTrayPositionSet(Vector3 trayPos)
    {
        KillSpawnEntrance();
        KillLiftAndPlaceTweens();
        transform.position = trayPos;
        transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
        transform.localRotation = Quaternion.identity;
    }

    private void HandleDragPositionUpdated(Vector3 newPos)
    {
        if (isPlacing) return;

        if (isLifting)
        {
            // Player moved finger during initial lift -> track X and Z immediately
            if (liftMoveZTween != null && liftMoveZTween.IsActive())
            {
                liftMoveZTween.Kill();
                liftMoveZTween = null;
            }
            transform.position = new Vector3(newPos.x, transform.position.y, newPos.z);
        }
        else
        {
            transform.position = newPos;
        }
    }

    private void HandleBlockReset()
    {
        KillSpawnEntrance();
        KillLiftAndPlaceTweens();
        KillTiltTweens();
        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
        isDragging = false;
        isSettling = false;
        targetRotation = Quaternion.identity;
    }

    private void HandleDragStarted()
    {
        KillSpawnEntrance();
        KillLiftAndPlaceTweens();
        KillTiltTweens();
        isDragging = true;
        isSettling = false;
        targetRotation = Quaternion.identity;
    }

    private void HandleBlockLiftRequested(Vector3 targetLiftPos)
    {
        KillSpawnEntrance();
        KillLiftAndPlaceTweens();
        KillTiltTweens();

        isLifting = true;
        isPlacing = false;

        transform.position = new Vector3(targetLiftPos.x, transform.position.y, targetLiftPos.z);

        liftMoveYTween = transform.DOMoveY(targetLiftPos.y, liftDuration)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .OnComplete(() =>
            {
                isLifting = false;
                liftMoveYTween = null;
            });

        liftMoveZTween = transform.DOMoveZ(targetLiftPos.z, liftDuration)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .OnComplete(() =>
            {
                liftMoveZTween = null;
            });

        liftScaleTween = transform.DOScale(Vector3.one, liftDuration)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .OnComplete(() =>
            {
                liftScaleTween = null;
            });
    }

    private void HandleBlockPlaceSlideRequested(Vector3 groundPos, Action onComplete)
    {
        KillSpawnEntrance();
        KillLiftAndPlaceTweens();
        KillTiltTweens();

        isPlacing = true;
        isLifting = false;
        isDragging = false;
        isSettling = false;

        // Snap X and Z precisely to target grid center in the air
        transform.position = new Vector3(groundPos.x, transform.position.y, groundPos.z);

        // Straighten tilt rotation rapidly
        transform.DOLocalRotate(Vector3.zero, placeDuration)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);

        // Slam down into the grid
        placeTween = transform.DOMoveY(groundPos.y, placeDuration)
            .SetEase(Ease.InQuad)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .OnComplete(() =>
            {
                isPlacing = false;
                placeTween = null;
                transform.position = groundPos;
                transform.localRotation = Quaternion.identity;
                onComplete?.Invoke();
            });
    }

    private void HandleDragVelocityUpdated(Vector3 velocity)
    {
        if (!isDragging || isPlacing) return;

        float speed = velocity.magnitude;
        if (speed > stopVelocityThreshold)
        {
            if (isSettling)
            {
                KillTiltTweens();
                isSettling = false;
            }

            // Screen/World X movement rolls around Z axis
            float roll = velocity.x * tiltSensitivity;
            if (invertRoll) roll = -roll;
            roll = Mathf.Clamp(roll, -maxTiltAngle, maxTiltAngle);

            // Screen/World Z movement pitches around X axis
            float pitch = -velocity.z * tiltSensitivity;
            if (invertPitch) pitch = -pitch;
            pitch = Mathf.Clamp(pitch, -maxTiltAngle, maxTiltAngle);

            targetRotation = Quaternion.Euler(pitch, 0f, roll);
        }
        else
        {
            // Finger stopped moving on screen while still held
            if (!isSettling && Quaternion.Angle(transform.localRotation, Quaternion.identity) > 0.5f)
            {
                PlaySettleWobbleSequence();
            }
        }
    }

    private void HandleDragEnded(bool isPlaced)
    {
        KillSpawnEntrance();
        isDragging = false;
        if (isPlaced)
        {
            // Settle or place is handled by HandleBlockPlaceSlideRequested
        }
        else
        {
            KillLiftAndPlaceTweens();
            transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            // Dropped back to tray: settle with a wobble
            PlaySettleWobbleSequence();
        }
    }

    private void PlaySettleWobbleSequence()
    {
        KillTiltTweens();
        isSettling = true;
        targetRotation = Quaternion.identity;

        Vector3 currentEuler = transform.localEulerAngles;
        float currentPitch = NormalizeAngle(currentEuler.x);
        float currentRoll = NormalizeAngle(currentEuler.z);

        if (Mathf.Abs(currentPitch) < 0.3f && Mathf.Abs(currentRoll) < 0.3f)
        {
            transform.localRotation = Quaternion.identity;
            isSettling = false;
            return;
        }

        Vector3 overshootEuler = new Vector3(
            -currentPitch * wobbleOvershootRatio,
            0f,
            -currentRoll * wobbleOvershootRatio
        );

        float t1 = wobbleDuration * 0.38f;
        float t2 = wobbleDuration * 0.62f;

        settleSequence = DOTween.Sequence();
        settleSequence.SetTarget(transform);
        settleSequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);

        // Phase 1: Overshoot in opposite direction
        settleSequence.Append(transform.DOLocalRotate(overshootEuler, t1).SetEase(Ease.OutQuad));

        // Phase 2: Settle back upright
        settleSequence.Append(transform.DOLocalRotate(Vector3.zero, t2).SetEase(Ease.OutBack));

        settleSequence.OnComplete(() =>
        {
            transform.localRotation = Quaternion.identity;
            isSettling = false;
        });
    }

    private void KillLiftAndPlaceTweens()
    {
        if (liftMoveYTween != null && liftMoveYTween.IsActive())
        {
            liftMoveYTween.Kill();
            liftMoveYTween = null;
        }
        if (liftMoveZTween != null && liftMoveZTween.IsActive())
        {
            liftMoveZTween.Kill();
            liftMoveZTween = null;
        }
        if (liftScaleTween != null && liftScaleTween.IsActive())
        {
            liftScaleTween.Kill();
            liftScaleTween = null;
        }
        if (placeTween != null && placeTween.IsActive())
        {
            placeTween.Kill();
            placeTween = null;
        }
        isLifting = false;
        isPlacing = false;
    }

    private void HandleSpawnEntranceRequested(int slotIndex, Vector3 trayPos, Action onComplete)
    {
        PlaySpawnEntrance(slotIndex, trayPos, onComplete);
    }

    private void PlaySpawnEntrance(int slotIndex, Vector3 trayPos, Action onComplete)
    {
        KillSpawnEntrance();
        KillLiftAndPlaceTweens();
        KillTiltTweens();

        Vector3 canonicalScale = transform.localScale;
        Vector3 buriedPosition = trayPos + Vector3.down * spawnEntranceRiseDistance;
        Vector3 startScale = Vector3.Scale(canonicalScale, spawnEntranceStartScaleMultiplier);
        Vector3 overshootScale = Vector3.Scale(canonicalScale, spawnEntranceOvershootScaleMultiplier);
        float tilt = GetSpawnEntranceTilt(slotIndex);
        float delay = spawnEntranceDelay + GetSpawnEntranceOrder(slotIndex) * spawnEntranceStagger;

        transform.position = buriedPosition;
        transform.localScale = startScale;
        transform.localRotation = Quaternion.Euler(0f, 0f, tilt);

        spawnEntranceSequence = DOTween.Sequence();
        spawnEntranceSequence.SetTarget(transform);
        spawnEntranceSequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);
        spawnEntranceSequence.AppendInterval(delay);
        spawnEntranceSequence.Append(transform.DOMoveY(trayPos.y, spawnEntranceRiseDuration).SetEase(Ease.OutCubic));
        spawnEntranceSequence.Join(transform.DOScale(overshootScale, spawnEntranceRiseDuration).SetEase(Ease.OutBack));
        spawnEntranceSequence.Join(transform.DOLocalRotate(Vector3.zero, spawnEntranceRiseDuration).SetEase(Ease.OutQuad));
        spawnEntranceSequence.Append(transform.DOScale(canonicalScale, spawnEntranceSettleDuration).SetEase(Ease.OutQuad));
        spawnEntranceSequence.OnComplete(() =>
        {
            transform.position = trayPos;
            transform.localScale = canonicalScale;
            transform.localRotation = Quaternion.identity;
            spawnEntranceSequence = null;
            onComplete?.Invoke();
        });
    }

    private void KillSpawnEntrance()
    {
        if (spawnEntranceSequence != null && spawnEntranceSequence.IsActive())
        {
            spawnEntranceSequence.Kill();
        }

        spawnEntranceSequence = null;
    }

    private static int GetSpawnEntranceOrder(int slotIndex)
    {
        return slotIndex switch
        {
            2 => 0,
            0 => 1,
            _ => 2
        };
    }

    private float GetSpawnEntranceTilt(int slotIndex)
    {
        return slotIndex switch
        {
            0 => -spawnEntranceTiltAngle,
            1 => spawnEntranceTiltAngle,
            _ => 0f
        };
    }

    private void KillTiltTweens()
    {
        if (settleSequence != null && settleSequence.IsActive())
        {
            settleSequence.Kill();
            settleSequence = null;
        }
        transform.DOKill();
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    private void DrawShape(List<(int x, int y)> offsets, Vector2 center, int[] variantIds, int[] rotationsY)
    {
        if (poolService == null) ServiceLocator.TryGet<IPoolService>(out poolService);
        if (blockService == null) ServiceLocator.TryGet<IBlockService>(out blockService);

        ClearShape();

        if (offsets == null) return;

        for (int i = 0; i < offsets.Count; i++)
        {
            var offset = offsets[i];
            int variantId = (variantIds != null && i < variantIds.Length) ? variantIds[i] : 0;
            int rotY = (rotationsY != null && i < rotationsY.Length) ? rotationsY[i] : 0;
            Quaternion cellRotation = Quaternion.Euler(0f, rotY, 0f);

            GameObject prefabToSpawn = blockService != null ? blockService.GetCellPrefab(variantId) : null;
            if (prefabToSpawn == null)
            {
                Debug.LogError($"[BlockView] No cell prefab returned from IBlockService for variant {variantId} on {gameObject.name}");
                continue;
            }

            GameObject cell = poolService.SpawnObject(prefabToSpawn, Vector3.zero, cellRotation);
            cell.transform.SetParent(this.transform, false);
            cell.transform.localScale = Vector3.one;
            cell.transform.localPosition = new Vector3(offset.x - center.x, 0, offset.y - center.y);
            cell.transform.localRotation = cellRotation;
            activeCells.Add(cell);
        }
    }

    private void ClearShape()
    {
        KillSpawnEntrance();
        KillLiftAndPlaceTweens();
        KillTiltTweens();
        transform.localRotation = Quaternion.identity;
        isDragging = false;
        isSettling = false;
        targetRotation = Quaternion.identity;

        if (poolService == null) return;

        foreach (var cell in activeCells)
        {
            if (cell != null)
            {
                cell.transform.DOKill();
                cell.transform.localScale = Vector3.one;
                cell.transform.localRotation = Quaternion.identity;
                poolService.ReturnObjectToPool(cell);
            }
        }
        activeCells.Clear();
    }
}
