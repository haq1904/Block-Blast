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

    private BlockController blockController;
    private IPoolService poolService;
    private IBlockService blockService;
    private readonly List<GameObject> activeCells = new List<GameObject>();

    private Quaternion targetRotation = Quaternion.identity;
    private bool isDragging;
    private bool isSettling;
    private Sequence settleSequence;

    private void Awake()
    {
        blockController = GetComponent<BlockController>();
        if (blockController != null)
        {
            blockController.OnShapeAssigned += DrawShape;
            blockController.OnDragVelocityUpdated += HandleDragVelocityUpdated;
            blockController.OnDragStarted += HandleDragStarted;
            blockController.OnDragEnded += HandleDragEnded;
        }
    }

    private void Update()
    {
        if (isDragging && !isSettling)
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
        KillTiltTweens();
        transform.localRotation = Quaternion.identity;
        isDragging = false;
        isSettling = false;
        targetRotation = Quaternion.identity;
    }

    private void OnDestroy()
    {
        KillTiltTweens();

        if (blockController != null)
        {
            blockController.OnShapeAssigned -= DrawShape;
            blockController.OnDragVelocityUpdated -= HandleDragVelocityUpdated;
            blockController.OnDragStarted -= HandleDragStarted;
            blockController.OnDragEnded -= HandleDragEnded;
        }

        foreach (var cell in activeCells)
        {
            if (cell != null)
            {
                cell.transform.DOKill();
            }
        }
    }

    private void HandleDragStarted()
    {
        KillTiltTweens();
        isDragging = true;
        isSettling = false;
        targetRotation = Quaternion.identity;
    }

    private void HandleDragVelocityUpdated(Vector3 velocity)
    {
        if (!isDragging) return;

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
        isDragging = false;
        if (isPlaced)
        {
            KillTiltTweens();
            transform.localRotation = Quaternion.identity;
            isSettling = false;
            targetRotation = Quaternion.identity;
        }
        else
        {
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

    private void DrawShape(List<(int x, int y)> offsets)
    {
        if (poolService == null) poolService = ServiceLocator.Get<IPoolService>();
        if (blockService == null) blockService = ServiceLocator.Get<IBlockService>();

        ClearShape();

        Vector2 center = blockController != null ? blockController.CenterOffset : Vector2.zero;
        int[] variantIds = blockController?.Model?.VariantIds;

        for (int i = 0; i < offsets.Count; i++)
        {
            var offset = offsets[i];
            int variantId = (variantIds != null && i < variantIds.Length) ? variantIds[i] : 0;

            GameObject prefabToSpawn = blockService != null ? blockService.GetCellPrefab(variantId) : null;
            if (prefabToSpawn == null)
            {
                Debug.LogError($"[BlockView] No cell prefab returned from IBlockService for variant {variantId} on {gameObject.name}");
                continue;
            }

            GameObject cell = poolService.SpawnObject(prefabToSpawn, Vector3.zero, Quaternion.identity);
            cell.transform.SetParent(this.transform, false);
            cell.transform.localScale = Vector3.one;
            cell.transform.localPosition = new Vector3(offset.x - center.x, 0, offset.y - center.y);
            activeCells.Add(cell);
        }
    }

    private void ClearShape()
    {
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
