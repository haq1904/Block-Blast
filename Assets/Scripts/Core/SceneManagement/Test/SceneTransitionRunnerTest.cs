using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.SceneManagement;

[TestFixture]
public class SceneTransitionRunnerTest
{
    private FakeLoadingScreen _loadingScreen;
    private FakeSceneLoadingBackend _backend;
    private FakeSceneTimeProvider _timeProvider;
    private SceneTransitionRunner _runner;

    [SetUp]
    public void SetUp()
    {
        _loadingScreen = new FakeLoadingScreen();
        _backend = new FakeSceneLoadingBackend();
        _timeProvider = new FakeSceneTimeProvider();
        _runner = new SceneTransitionRunner(_backend, _loadingScreen, _timeProvider);
    }

    [Test]
    public async Task RunAsync_Success_ExecutesStrictSequence()
    {
        var request = new SceneLoadRequest(1, LoadSceneMode.Single, 0f);

        await _runner.RunAsync(request);

        var expectedSequence = new[]
        {
            "Prepare",
            "ShowAsync",
            "SetProgress:0.50",
            "SetProgress:1.00",
            "HideAsync"
        };

        CollectionAssert.AreEqual(expectedSequence, _loadingScreen.RecordedEvents);
        Assert.IsTrue(_backend.LoadCalled, "Backend.LoadSceneAsync must have been called.");
        Assert.IsTrue(_backend.OperationToReturn.AllowSceneActivation, "AllowSceneActivation must be true after completion.");
        Assert.IsFalse(_runner.IsTransitioning);
        Assert.AreEqual(0, _loadingScreen.ResetImmediateCount, "ResetImmediate must NOT be called on success.");
    }

    [Test]
    public async Task RunAsync_BackendDoesNotStartBeforeShowCompletes()
    {
        var request = new SceneLoadRequest(1);
        bool backendStartedWhileShowing = false;

        _loadingScreen.OnShowAsync = () =>
        {
            if (_backend.LoadCalled)
            {
                backendStartedWhileShowing = true;
            }
        };

        await _runner.RunAsync(request);

        Assert.IsFalse(backendStartedWhileShowing, "Backend must not start before ShowAsync completes.");
    }

    [Test]
    public async Task RunAsync_NormalizesRawProgressCorrectly()
    {
        var request = new SceneLoadRequest(1);

        _backend.OperationToReturn = new FakeSceneLoadOperation
        {
            CustomProgressSequence = new[] { 0.0f, 0.45f, 0.9f }
        };

        await _runner.RunAsync(request);

        Assert.IsTrue(_loadingScreen.ProgressValues.Contains(0.0f));
        Assert.IsTrue(_loadingScreen.ProgressValues.Contains(0.5f)); // 0.45 / 0.9 = 0.5
        Assert.IsTrue(_loadingScreen.ProgressValues.Contains(1.0f));
    }

    [Test]
    public async Task RunAsync_SendsFinalProgressOneBeforeActivation()
    {
        var request = new SceneLoadRequest(1);
        float progressAtActivation = -1f;

        _backend.OperationToReturn = new FakeSceneLoadOperation
        {
            OnAllowSceneActivationSet = allow =>
            {
                if (allow)
                {
                    progressAtActivation = _loadingScreen.LastProgress;
                }
            }
        };

        await _runner.RunAsync(request);

        Assert.AreEqual(1f, progressAtActivation, 0.0001f, "Progress 1.0f must be sent before AllowSceneActivation is set to true.");
    }

    [Test]
    public async Task RunAsync_RespectsMinimumVisibleDurationUsingRealtime()
    {
        var request = new SceneLoadRequest(1, LoadSceneMode.Single, minimumVisibleDuration: 2.5f);

        await _runner.RunAsync(request);

        Assert.IsTrue(_timeProvider.DelayedDurations.Contains(2.5f), "Must delay for remaining MinimumVisibleDuration.");
    }

    [Test]
    public void RunAsync_WithInvalidTargetBuildIndex_ThrowsArgumentExceptionAndDoesNotPrepare()
    {
        var request = new SceneLoadRequest(999);

        Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _runner.RunAsync(request);
        });

        Assert.IsFalse(_backend.LoadCalled);
        Assert.AreEqual(0, _loadingScreen.RecordedEvents.Count);
        Assert.IsFalse(_runner.IsTransitioning);
    }

    [Test]
    public void RunAsync_WhenShowThrows_CallsResetImmediateAndDoesNotLoad()
    {
        _loadingScreen.ThrowOnShow = new InvalidOperationException("Show failed");
        var request = new SceneLoadRequest(1);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _runner.RunAsync(request);
        });

        Assert.IsFalse(_backend.LoadCalled);
        Assert.AreEqual(1, _loadingScreen.ResetImmediateCount);
        Assert.IsFalse(_runner.IsTransitioning);
    }

    [Test]
    public void RunAsync_WhenBackendFails_CallsResetImmediateAndDoesNotHide()
    {
        _backend.OperationToReturn = null; // Simulates backend failure returning null
        var request = new SceneLoadRequest(1);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _runner.RunAsync(request);
        });

        Assert.IsFalse(_loadingScreen.RecordedEvents.Contains("HideAsync"));
        Assert.AreEqual(1, _loadingScreen.ResetImmediateCount);
        Assert.IsFalse(_runner.IsTransitioning);
    }

    [Test]
    public void RunAsync_WhenHideThrows_CallsResetImmediateAndPropagates()
    {
        _loadingScreen.ThrowOnHide = new InvalidOperationException("Hide failed");
        var request = new SceneLoadRequest(1);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _runner.RunAsync(request);
        });

        Assert.AreEqual(1, _loadingScreen.ResetImmediateCount);
        Assert.IsFalse(_runner.IsTransitioning);
    }

    [Test]
    public void RunAsync_CancelledBeforeLoad_CallsResetImmediateAndDoesNotLoad()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var request = new SceneLoadRequest(1);

        Assert.CatchAsync<OperationCanceledException>(async () =>
        {
            await _runner.RunAsync(request, cts.Token);
        });

        Assert.IsFalse(_backend.LoadCalled);
        Assert.AreEqual(1, _loadingScreen.ResetImmediateCount);
        Assert.IsFalse(_runner.IsTransitioning);
    }

    [Test]
    public void RunAsync_SecondTransitionWhileFirstIsActive_ThrowsInvalidOperationException()
    {
        var tcs = new TaskCompletionSource<bool>();
        _loadingScreen.ShowTaskOverride = tcs.Task;

        var request1 = new SceneLoadRequest(1);
        var request2 = new SceneLoadRequest(1);

        var firstTask = _runner.RunAsync(request1);

        Assert.IsTrue(_runner.IsTransitioning);
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _runner.RunAsync(request2);
        });

        tcs.SetResult(true);
        Assert.DoesNotThrowAsync(async () => await firstTask);
        Assert.IsFalse(_runner.IsTransitioning);
    }

    // --- Fakes ---

    private sealed class FakeLoadingScreen : ILoadingScreen
    {
        public readonly List<string> RecordedEvents = new List<string>();
        public readonly List<float> ProgressValues = new List<float>();
        public float LastProgress { get; private set; } = -1f;
        public int ResetImmediateCount { get; private set; }

        public Action OnShowAsync;
        public Exception ThrowOnShow;
        public Exception ThrowOnHide;
        public Task ShowTaskOverride;

        public void Prepare(SceneTransitionContext context)
        {
            RecordedEvents.Add("Prepare");
        }

        public async Task ShowAsync(SceneTransitionContext context, CancellationToken cancellationToken)
        {
            RecordedEvents.Add("ShowAsync");
            OnShowAsync?.Invoke();

            if (ThrowOnShow != null) throw ThrowOnShow;
            if (ShowTaskOverride != null) await ShowTaskOverride;
        }

        public void SetProgress(float normalizedProgress)
        {
            LastProgress = normalizedProgress;
            ProgressValues.Add(normalizedProgress);
            RecordedEvents.Add($"SetProgress:{normalizedProgress:F2}");
        }

        public Task HideAsync(SceneTransitionContext context, CancellationToken cancellationToken)
        {
            RecordedEvents.Add("HideAsync");
            if (ThrowOnHide != null) throw ThrowOnHide;
            return Task.CompletedTask;
        }

        public void ResetImmediate()
        {
            ResetImmediateCount++;
            RecordedEvents.Add("ResetImmediate");
        }
    }

    private sealed class FakeSceneLoadingBackend : ISceneLoadingBackend
    {
        public int ActiveSceneBuildIndex => 0;
        public string ActiveSceneName => "BootstrapScene";
        public bool LoadCalled { get; private set; }
        public ISceneLoadOperation OperationToReturn { get; set; } = new FakeSceneLoadOperation();

        public bool IsBuildIndexValid(int buildIndex) => buildIndex >= 0 && buildIndex <= 5;

        public string GetSceneNameByBuildIndex(int buildIndex) => $"Scene_{buildIndex}";

        public ISceneLoadOperation LoadSceneAsync(int buildIndex, LoadSceneMode mode)
        {
            LoadCalled = true;
            return OperationToReturn;
        }
    }

    private sealed class FakeSceneLoadOperation : ISceneLoadOperation
    {
        private int _progressIndex;
        private bool _allowActivation;

        public float[] CustomProgressSequence { get; set; } = new[] { 0.45f, 0.9f };
        public Action<bool> OnAllowSceneActivationSet { get; set; }

        public float Progress
        {
            get
            {
                if (CustomProgressSequence != null && _progressIndex < CustomProgressSequence.Length)
                {
                    return CustomProgressSequence[_progressIndex++];
                }
                return 0.9f;
            }
        }

        public bool IsDone => _allowActivation;

        public bool AllowSceneActivation
        {
            get => _allowActivation;
            set
            {
                _allowActivation = value;
                OnAllowSceneActivationSet?.Invoke(value);
            }
        }
    }

    private sealed class FakeSceneTimeProvider : ISceneTimeProvider
    {
        public readonly List<float> DelayedDurations = new List<float>();
        public float RealtimeSinceStartup { get; set; } = 0f;

        public Task DelayAsync(float seconds, CancellationToken cancellationToken)
        {
            DelayedDurations.Add(seconds);
            RealtimeSinceStartup += seconds;
            return Task.CompletedTask;
        }

        public Task YieldAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
