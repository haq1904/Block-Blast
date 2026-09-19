using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class AutoReturnToPool : MonoBehaviour
{
    [Tooltip("Pool category used when returning this GameObject to the pool.")]
    [SerializeField] private PoolType poolType = PoolType.ParticleSystem;

    [Tooltip("Fallback lifetime in seconds before returning to pool if no particle system or stop callback fires.")]
    [SerializeField] private float fallbackLifetime = 2.0f;

    private Tween delayTween;
    private IPoolService poolService;

    private void Awake()
    {
        var ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }
    }

    private void OnEnable()
    {
        poolService = ServiceLocator.Get<IPoolService>();

        delayTween?.Kill();
        if (fallbackLifetime > 0f)
        {
            delayTween = DOVirtual.DelayedCall(fallbackLifetime, ReturnToPool)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
    }

    private void OnDisable()
    {
        delayTween?.Kill();
        delayTween = null;
    }

    private void OnParticleSystemStopped()
    {
        ReturnToPool();
    }

    public void ReturnToPool()
    {
        delayTween?.Kill();
        delayTween = null;

        if (poolService == null)
        {
            ServiceLocator.TryGet<IPoolService>(out poolService);
        }

        if (gameObject.activeSelf)
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;

            if (poolService != null)
            {
                poolService.ReturnObjectToPool(gameObject, poolType);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
