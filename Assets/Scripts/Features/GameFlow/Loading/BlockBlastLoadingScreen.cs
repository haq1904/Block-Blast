using System.Threading;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Feature adapter connecting Core SceneTransitionRunner to BlockBlastLoadingAnimationView.
/// </summary>
public sealed class BlockBlastLoadingScreen : BaseLoadingScreen
{
    [SerializeField] private BlockBlastLoadingAnimationView _animationView;

    public override void Prepare(SceneTransitionContext context)
    {
        if (_animationView == null)
        {
            Debug.LogError("[BlockBlastLoadingScreen] Missing required BlockBlastLoadingAnimationView reference.", this);
            return;
        }

        _animationView.ResetOpenImmediate();
    }

    public override async Task ShowAsync(SceneTransitionContext context, CancellationToken cancellationToken)
    {
        if (_animationView == null)
        {
            Debug.LogError("[BlockBlastLoadingScreen] Missing required BlockBlastLoadingAnimationView reference.", this);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        Sequence seq = _animationView.PlayCover();
        if (seq == null)
        {
            return;
        }

        await AwaitSequenceAsync(seq, cancellationToken);
    }

    public override void SetProgress(float normalizedProgress)
    {
        // No-op: Cloud parallax visual does not display a numerical progress bar.
    }

    public override async Task HideAsync(SceneTransitionContext context, CancellationToken cancellationToken)
    {
        if (_animationView == null)
        {
            Debug.LogError("[BlockBlastLoadingScreen] Missing required BlockBlastLoadingAnimationView reference.", this);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        Sequence seq = _animationView.PlayReveal();
        if (seq == null)
        {
            return;
        }

        await AwaitSequenceAsync(seq, cancellationToken);
    }

    public override void ResetImmediate()
    {
        if (_animationView == null)
        {
            return;
        }

        _animationView.ResetOpenImmediate();
    }

    private static async Task AwaitSequenceAsync(Sequence seq, CancellationToken cancellationToken)
    {
        if (seq == null || !seq.IsActive() || seq.IsComplete())
        {
            return;
        }

        var tcs = new TaskCompletionSource<bool>();

        TweenCallback prevOnComplete = seq.onComplete;
        seq.onComplete = () =>
        {
            prevOnComplete?.Invoke();
            tcs.TrySetResult(true);
        };

        TweenCallback prevOnKill = seq.onKill;
        seq.onKill = () =>
        {
            prevOnKill?.Invoke();
            tcs.TrySetResult(false);
        };

        if (!seq.IsActive() || seq.IsComplete())
        {
            tcs.TrySetResult(true);
            return;
        }

        using (cancellationToken.Register(() =>
        {
            if (seq.IsActive())
            {
                seq.Kill();
            }
            tcs.TrySetCanceled(cancellationToken);
        }))
        {
            await tcs.Task;
        }
    }
}
