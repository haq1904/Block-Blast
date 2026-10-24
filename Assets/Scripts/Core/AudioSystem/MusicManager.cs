using System.Threading;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Generic playback engine managing a single persistent AudioSource with smooth DOTween crossfade/transition support.
/// Does not contain domain-specific music semantics.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;

    private Sequence _activeSequence;

    public AudioSource Source => _audioSource;

    private void Awake()
    {
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.volume = 1f;

        DontDestroyOnLoad(gameObject);
    }

    public void PlayImmediate(AudioClip clip)
    {
        KillActiveTransition();

        EnsureAudioSource();

        if (clip == null)
        {
            _audioSource.Stop();
            _audioSource.clip = null;
            return;
        }

        if (_audioSource.clip == clip && _audioSource.isPlaying)
        {
            return;
        }

        _audioSource.clip = clip;
        _audioSource.time = 0f;
        _audioSource.volume = 1f;
        _audioSource.loop = false;
        _audioSource.Play();
    }

    public async Task TransitionToAsync(
        AudioClip nextClip,
        float fadeOutSeconds,
        float fadeInSeconds,
        CancellationToken cancellationToken = default)
    {
        KillActiveTransition();

        EnsureAudioSource();

        if (nextClip == null)
        {
            PlayImmediate(null);
            return;
        }

        if (_audioSource.clip == nextClip && _audioSource.isPlaying)
        {
            _audioSource.volume = 1f;
            return;
        }

        var tcs = new TaskCompletionSource<bool>();
        var ctsRegistration = default(CancellationTokenRegistration);

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(_audioSource);
        seq.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        seq.SetUpdate(true);

        float clampedFadeOut = Mathf.Max(0f, fadeOutSeconds);
        float clampedFadeIn = Mathf.Max(0f, fadeInSeconds);

        if (_audioSource.isPlaying && _audioSource.volume > 0.001f && clampedFadeOut > 0f)
        {
            seq.Append(DOTween.To(() => _audioSource.volume, v => _audioSource.volume = v, 0f, clampedFadeOut)
                .SetEase(Ease.InOutSine));
        }
        else
        {
            _audioSource.volume = 0f;
        }

        seq.AppendCallback(() =>
        {
            _audioSource.clip = nextClip;
            _audioSource.time = 0f;
            _audioSource.volume = 0f;
            _audioSource.loop = false;
            _audioSource.Play();
        });

        if (clampedFadeIn > 0f)
        {
            seq.Append(DOTween.To(() => _audioSource.volume, v => _audioSource.volume = v, 1f, clampedFadeIn)
                .SetEase(Ease.OutSine));
        }
        else
        {
            seq.AppendCallback(() => _audioSource.volume = 1f);
        }

        seq.OnComplete(() =>
        {
            _audioSource.volume = 1f;
            _activeSequence = null;
            tcs.TrySetResult(true);
        });

        seq.OnKill(() =>
        {
            _activeSequence = null;
            tcs.TrySetResult(false);
        });

        _activeSequence = seq;

        if (cancellationToken.CanBeCanceled)
        {
            ctsRegistration = cancellationToken.Register(() =>
            {
                if (_activeSequence != null && _activeSequence.IsActive())
                {
                    _activeSequence.Kill();
                }
                tcs.TrySetCanceled(cancellationToken);
            });
        }

        try
        {
            await tcs.Task;
        }
        finally
        {
            ctsRegistration.Dispose();
            if (_activeSequence == seq)
            {
                _activeSequence = null;
            }
        }
    }

    public bool IsCurrentClip(AudioClip clip)
    {
        return _audioSource != null && _audioSource.clip == clip;
    }

    private void EnsureAudioSource()
    {
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }
    }

    private void KillActiveTransition()
    {
        if (_activeSequence != null)
        {
            if (_activeSequence.IsActive())
            {
                _activeSequence.Kill();
            }
            _activeSequence = null;
        }
    }

    private void OnDisable()
    {
        KillActiveTransition();
    }

    private void OnDestroy()
    {
        KillActiveTransition();
    }
}
