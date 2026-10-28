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

    [Test]
    public void RunAsync_WhenSetProgressThrowsAfterOperationStarted_ReleasesActivationAndResetsImmediate()
    {
        _loadingScreen.ThrowOnSetProgress = new InvalidOperationException("Progress UI exploded");
        var request = new SceneLoadRequest(1);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _runner.RunAsync(request);
        });

        Assert.IsTrue(_backend.LoadCalled);
        Assert.IsTrue(_backend.OperationToReturn.AllowSceneActivation, "Operation activation must be released in failure path.");
        Assert.AreEqual(1, _loadingScreen.ResetImmediateCount);
        Assert.IsFalse(_runner.IsTransitioning);
    }

    [Test]
    public void RunAsync_WhenTimeProviderDelayThrows_ReleasesActivationAndPreservesOriginalException()
    {
        _timeProvider.ThrowOnDelay = new TimeoutException("Delay clock failure");
        var request = new SceneLoadRequest(1, LoadSceneMode.Single, 2.0f);

        Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await _runner.RunAsync(request);
        });

        Assert.IsTrue(_backend.LoadCalled);
        Assert.IsTrue(_backend.OperationToReturn.AllowSceneActivation, "Operation activation must be released when delay fails.");
        Assert.AreEqual(1, _loadingScreen.ResetImmediateCount);
        Assert.IsFalse(_runner.IsTransitioning);
    }

    [Test]
    public void RunAsync_CancelledDuringShowAsync_DoesNotStartBackend()
    {
        using var cts = new CancellationTokenSource();
        _loadingScreen.OnShowAsync = () => cts.Cancel();
        var request = new SceneLoadRequest(1);

        Assert.CatchAsync<OperationCanceledException>(async () =>
        {
            await _runner.RunAsync(request, cts.Token);
        });

        Assert.IsFalse(_backend.LoadCalled, "Backend must not start if cancelled during ShowAsync.");
        Assert.AreEqual(1, _loadingScreen.ResetImmediateCount);
        Assert.IsFalse(_runner.IsTransitioning);
    }

    [Test]
    public async Task RunAsync_CallerCancelledAfterOperationStarted_StillCompletesActivationAndReveal()
    {
        using var cts = new CancellationTokenSource();
        var request = new SceneLoadRequest(1);

        _backend.OperationToReturn = new FakeSceneLoadOperation
        {
            OnAllowSceneActivationSet = allow =>
            {
                cts.Cancel();
            }
        };

        await _runner.RunAsync(request, cts.Token);

        Assert.IsTrue(_backend.OperationToReturn.AllowSceneActivation);
        Assert.IsTrue(_backend.OperationToReturn.IsDone);
        Assert.IsTrue(_loadingScreen.RecordedEvents.Contains("HideAsync"));
        Assert.IsFalse(_runner.IsTransitioning);
    }

    [Test]
    public async Task RunAsync_SceneLoadSlowerThanMinimumDuration_DoesNotDelayExtra()
    {
        var request = new SceneLoadRequest(1, LoadSceneMode.Single, minimumVisibleDuration: 1.0f);

        _timeProvider.RealtimeSinceStartup = 0f;
        _backend.OperationToReturn = new FakeSceneLoadOperation
        {
            CustomProgressSequence = new[] { 0.45f, 0.9f },
            OnProgressPolled = () =>
            {
                // Advance simulated clock during loading operation past minimum duration
                _timeProvider.RealtimeSinceStartup += 2.5f;
            }
        };

        await _runner.RunAsync(request);

        Assert.AreEqual(0, _timeProvider.DelayedDurations.Count, "No additional delay should occur if load duration exceeded minimum.");
    }

    [Test]
    public async Task RunAsync_OperationRequiresMultipleYieldsAfterActivation_AwaitsUntilIsDone()
    {
        var request = new SceneLoadRequest(1);
        _backend.OperationToReturn = new FakeSceneLoadOperation
        {
            ActivationYieldCount = 3
        };

        await _runner.RunAsync(request);

        Assert.IsTrue(_backend.OperationToReturn.IsDone);
        Assert.IsTrue(_loadingScreen.RecordedEvents.Contains("HideAsync"));
    }

    [Test]
    public async Task RunAsync_ProgressValues_AreAlwaysClampedBetweenZeroAndOne()
    {
        var request = new SceneLoadRequest(1);
        _backend.OperationToReturn = new FakeSceneLoadOperation
        {
            CustomProgressSequence = new[] { -0.5f, 0.45f, 0.89f, 0.95f, 1.2f }
        };

        await _runner.RunAsync(request);

        foreach (float p in _loadingScreen.ProgressValues)
        {
            Assert.GreaterOrEqual(p, 0f);
            Assert.LessOrEqual(p, 1f);
        }
    }

    [Test]
    public async Task RunAsync_ContextDerivesIsReloadCorrectly()
    {
        var reloadRequest = new SceneLoadRequest(0);
        await _runner.RunAsync(reloadRequest);
        Assert.IsTrue(_loadingScreen.LastPreparedContext.IsReload);

        var newSceneRequest = new SceneLoadRequest(1);
        await _runner.RunAsync(newSceneRequest);
        Assert.IsFalse(_loadingScreen.LastPreparedContext.IsReload);
    }

    [Test]
    public async Task RunAsync_WithPresentationNone_BypassesLoadingScreenAndCompletesLoad()
    {
        var request = new SceneLoadRequest(1, LoadSceneMode.Single, 0f, SceneTransitionPresentation.None);

        await _runner.RunAsync(request);

        Assert.AreEqual(0, _loadingScreen.RecordedEvents.Count, "In None presentation mode, custom loading screen must not receive events.");
        Assert.IsTrue(_backend.LoadCalled, "Backend.LoadSceneAsync must still be called.");
        Assert.IsTrue(_backend.OperationToReturn.AllowSceneActivation, "AllowSceneActivation must be true after completion.");
        Assert.IsFalse(_runner.IsTransitioning);
        Assert.AreEqual(0, _loadingScreen.ResetImmediateCount);
    }

    [Test]
    public async Task RunAdditiveAsync_Success_LoadsTargetSetsActiveUnloadsSource()
    {
        _backend.ActiveSceneBuildIndex = 1;
        _backend.LoadedScenes.Add(1);

        var request = new AdditiveSceneTransitionRequest(
            targetSceneBuildIndex: 2,
            loadBuildIndexes: new[] { 4, 2 },
            unloadBuildIndexes: new[] { 1 },
            activeSceneBuildIndex: 4,
            minimumVisibleDuration: 0f);

        await _runner.RunAdditiveAsync(request);

        Assert.IsTrue(_backend.IsSceneLoaded(4), "World scene 4 should be loaded.");
        Assert.IsTrue(_backend.IsSceneLoaded(2), "Lobby scene 2 should be loaded.");
        Assert.IsFalse(_backend.IsSceneLoaded(1), "Intro scene 1 should be unloaded.");
        Assert.AreEqual(4, _backend.ActiveSceneBuildIndex, "Active scene should be World scene 4.");
        Assert.IsFalse(_runner.IsTransitioning);
        Assert.IsTrue(_loadingScreen.RecordedEvents.Contains("Prepare"));
        Assert.IsTrue(_loadingScreen.RecordedEvents.Contains("ShowAsync"));
        Assert.IsTrue(_loadingScreen.RecordedEvents.Contains("HideAsync"));
    }

    [Test]
    public async Task RunAdditiveAsync_NoOp_WhenDesiredStateAlreadyMet()
    {
        _backend.ActiveSceneBuildIndex = 4;
        _backend.LoadedScenes.Add(4);
        _backend.LoadedScenes.Add(2);

        var request = new AdditiveSceneTransitionRequest(
            targetSceneBuildIndex: 2,
            loadBuildIndexes: new[] { 4, 2 },
            unloadBuildIndexes: new[] { 1, 3 },
            activeSceneBuildIndex: 4,
            minimumVisibleDuration: 0f);

        await _runner.RunAdditiveAsync(request);

        Assert.AreEqual(0, _loadingScreen.RecordedEvents.Count, "No-op transition must not show curtain.");
        Assert.IsFalse(_runner.IsTransitioning);
    }

    [Test]
    public void RunAdditiveAsync_PreCommitFailure_RollsBackOnlyNewlyLoadedScenes()
    {
        _backend.ActiveSceneBuildIndex = 2;
        _backend.LoadedScenes.Add(2);
        _backend.LoadedScenes.Add(4); // Scene 4 was already loaded before request

        var request = new AdditiveSceneTransitionRequest(
            targetSceneBuildIndex: 3,
            loadBuildIndexes: new[] { 4, 3 },
            unloadBuildIndexes: new[] { 2 },
            activeSceneBuildIndex: 4,
            minimumVisibleDuration: 0f);

        _backend.ThrowOnSetActive = true; // Commit will fail

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _runner.RunAdditiveAsync(request);
        });

        Assert.IsTrue(_backend.IsSceneLoaded(4), "Pre-existing scene 4 must NOT be unloaded during rollback.");
        Assert.IsTrue(_backend.IsSceneLoaded(2), "Source scene 2 must NOT be unloaded when commit failed.");
        Assert.IsFalse(_backend.IsSceneLoaded(3), "Newly loaded scene 3 must be rolled back (unloaded).");
        Assert.AreEqual(2, _backend.ActiveSceneBuildIndex, "Previous active scene must be preserved.");
        Assert.IsFalse(_runner.IsTransitioning);
        Assert.AreEqual(1, _loadingScreen.ResetImmediateCount);
    }

    [Test]
    public void RunAdditiveAsync_InvalidBuildIndex_ThrowsArgumentException()
    {
        var request = new AdditiveSceneTransitionRequest(
            targetSceneBuildIndex: 99,
            loadBuildIndexes: new[] { 4 },
            unloadBuildIndexes: new int[0],
            activeSceneBuildIndex: 4);

        Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _runner.RunAdditiveAsync(request);
        });
        Assert.IsFalse(_runner.IsTransitioning);
    }

    [Test]
    public async Task RunAdditiveAsync_WhileAlreadyTransitioning_ThrowsInvalidOperationException()
    {
        _backend.LoadedScenes.Add(1);
        _backend.ActiveSceneBuildIndex = 1;

        var tcs = new TaskCompletionSource<bool>();
        _loadingScreen.ShowTaskOverride = tcs.Task;

        var request = new AdditiveSceneTransitionRequest(
            targetSceneBuildIndex: 2,
            loadBuildIndexes: new[] { 4, 2 },
            unloadBuildIndexes: new int[0],
            activeSceneBuildIndex: 4);

        var firstRun = _runner.RunAdditiveAsync(request);
        Assert.IsTrue(_runner.IsTransitioning);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _runner.RunAdditiveAsync(request);
        });

        tcs.SetResult(true);
        await firstRun;
    }

    [Test]
    public async Task RunAdditiveAsync_SequentialLoading_SecondOperationDoesNotStartBeforeFirstIsDone()
    {
        _backend.ActiveSceneBuildIndex = 1;
        _backend.LoadedScenes.Add(1);

        int loadOrderCount = 0;
        int scene4CompletedOrder = -1;
        int scene2StartedOrder = -1;

        var op4 = new FakeSceneLoadOperation();
        var op2 = new FakeSceneLoadOperation();

        op4.OnAllowSceneActivationSet = (val) =>
        {
            if (val) scene4CompletedOrder = ++loadOrderCount;
        };

        _backend.LoadOperationFactory = (idx) =>
        {
            if (idx == 4) return op4;
            if (idx == 2)
            {
                scene2StartedOrder = ++loadOrderCount;
                return op2;
            }
            return new FakeSceneLoadOperation();
        };

        var request = new AdditiveSceneTransitionRequest(
            targetSceneBuildIndex: 2,
            loadBuildIndexes: new[] { 4, 2 },
            unloadBuildIndexes: new[] { 1 },
            activeSceneBuildIndex: 4);

        await _runner.RunAdditiveAsync(request);

        Assert.AreEqual(2, _backend.LoadedRecord.Count);
        Assert.AreEqual(4, _backend.LoadedRecord[0]);
        Assert.AreEqual(2, _backend.LoadedRecord[1]);
        Assert.Greater(scene4CompletedOrder, 0);
        Assert.Greater(scene2StartedOrder, scene4CompletedOrder, "Second operation must start only after first operation activation.");
    }

    [Test]
    public async Task RunAdditiveAsync_Progress_IsMonotonicAndReachesOne()
    {
        _backend.ActiveSceneBuildIndex = 1;
        _backend.LoadedScenes.Add(1);

        var request = new AdditiveSceneTransitionRequest(
            targetSceneBuildIndex: 2,
            loadBuildIndexes: new[] { 4, 2 },
            unloadBuildIndexes: new[] { 1 },
            activeSceneBuildIndex: 4);

        await _runner.RunAdditiveAsync(request);

        float prev = -1f;
        foreach (float p in _loadingScreen.ProgressValues)
        {
            Assert.GreaterOrEqual(p, prev, "Additive progress must be monotonic.");
            Assert.LessOrEqual(p, 1f, "Progress must not exceed 1.");
            prev = p;
        }
        Assert.AreEqual(1f, _loadingScreen.LastProgress, "Final progress must reach 1.");
    }

    [Test]
    public void RunAdditiveAsync_NullUnloadAfterCommit_ThrowsWithIndexAndUnloadsOthers()
    {
        _backend.ActiveSceneBuildIndex = 1;
        _backend.LoadedScenes.Add(1);
        _backend.LoadedScenes.Add(5); // Another source scene

        var request = new AdditiveSceneTransitionRequest(
            targetSceneBuildIndex: 2,
            loadBuildIndexes: new[] { 4, 2 },
            unloadBuildIndexes: new[] { 1, 5 },
            activeSceneBuildIndex: 4);

        // Scene 1 unload returns null, Scene 5 unload succeeds
        _backend.UnloadOperationFactory = (idx) => idx == 1 ? null : new FakeSceneUnloadOperation();

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _runner.RunAdditiveAsync(request);
        });

        StringAssert.Contains("1", ex.Message, "Exception message must identify failed unload index.");
        Assert.IsFalse(_backend.IsSceneLoaded(5), "Other source scenes must still be unloaded.");
        Assert.AreEqual(4, _backend.ActiveSceneBuildIndex, "Target active scene must remain committed.");
    }

    [Test]
    public void RunAdditiveAsync_CancellationDuringLoad_RollsBackNewlyLoadedAndRestoresActive()
    {
        _backend.ActiveSceneBuildIndex = 1;
        _backend.LoadedScenes.Add(1);

        using var cts = new CancellationTokenSource();

        var op = new FakeSceneLoadOperation();
        op.OnProgressPolled = () =>
        {
            cts.Cancel();
        };
        _backend.LoadOperationFactory = _ => op;

        var request = new AdditiveSceneTransitionRequest(
            targetSceneBuildIndex: 2,
            loadBuildIndexes: new[] { 4, 2 },
            unloadBuildIndexes: new[] { 1 },
            activeSceneBuildIndex: 4);

        Assert.CatchAsync<OperationCanceledException>(async () =>
        {
            await _runner.RunAdditiveAsync(request, cts.Token);
        });

        Assert.IsFalse(_backend.IsSceneLoaded(2), "Newly loaded scene 2 must be unloaded.");
        Assert.AreEqual(1, _backend.ActiveSceneBuildIndex, "Previous active scene must be preserved.");
        Assert.AreEqual(1, _loadingScreen.ResetImmediateCount, "Curtain must be reset on cancellation.");
    }

    [Test]
    public void RunAdditiveAsync_CancellationAfterActivationBeforeCommit_RollsBackNewlyLoadedAndRestoresActive()
    {
        _backend.ActiveSceneBuildIndex = 1;
        _backend.LoadedScenes.Add(1);

        using var cts = new CancellationTokenSource();

        var op = new FakeSceneLoadOperation();
        op.OnAllowSceneActivationSet = (allow) =>
        {
            if (allow) cts.Cancel();
        };
        _backend.LoadOperationFactory = _ => op;

        var request = new AdditiveSceneTransitionRequest(
            targetSceneBuildIndex: 2,
            loadBuildIndexes: new[] { 4, 2 },
            unloadBuildIndexes: new[] { 1 },
            activeSceneBuildIndex: 4);

        Assert.CatchAsync<OperationCanceledException>(async () =>
        {
            await _runner.RunAdditiveAsync(request, cts.Token);
        });

        Assert.IsFalse(_backend.IsSceneLoaded(4), "Scene 4 must be rolled back on cancellation after activation.");
        Assert.IsFalse(_backend.IsSceneLoaded(2), "Scene 2 must be rolled back on cancellation after activation.");
        Assert.AreEqual(1, _backend.ActiveSceneBuildIndex, "Previous active scene must be preserved.");
        Assert.AreEqual(1, _loadingScreen.ResetImmediateCount, "Curtain must be reset on cancellation.");
    }

    [Test]
    public void RunAdditiveAsync_CancellationDuringMinimumDurationDelay_RollsBackNewlyLoadedAndRestoresActive()
    {
        _backend.ActiveSceneBuildIndex = 1;
        _backend.LoadedScenes.Add(1);

        using var cts = new CancellationTokenSource();

        var request = new AdditiveSceneTransitionRequest(
            targetSceneBuildIndex: 2,
            loadBuildIndexes: new[] { 4, 2 },
            unloadBuildIndexes: new[] { 1 },
            activeSceneBuildIndex: 4,
            minimumVisibleDuration: 2.0f);

        _timeProvider.ThrowOnDelay = new OperationCanceledException(cts.Token);

        Assert.CatchAsync<OperationCanceledException>(async () =>
        {
            await _runner.RunAdditiveAsync(request, cts.Token);
        });

        Assert.IsFalse(_backend.IsSceneLoaded(4), "Scene 4 must be rolled back on delay cancellation.");
        Assert.IsFalse(_backend.IsSceneLoaded(2), "Scene 2 must be rolled back on delay cancellation.");
        Assert.AreEqual(1, _backend.ActiveSceneBuildIndex, "Previous active scene must be preserved.");
        Assert.AreEqual(1, _loadingScreen.ResetImmediateCount, "Curtain must be reset on cancellation.");
    }

    [Test]
    public void FakeBackend_LoadAndUnloadChangeSceneStateOnlyAfterCompletion()
    {
        var load = _backend.LoadSceneAsync(4, LoadSceneMode.Additive);
        Assert.IsFalse(_backend.IsSceneLoaded(4));
        Assert.IsFalse(_backend.TrySetActiveScene(4));

        load.AllowSceneActivation = true;
        Assert.IsTrue(load.IsDone);
        Assert.IsTrue(_backend.IsSceneLoaded(4));
        Assert.IsTrue(_backend.TrySetActiveScene(4));

        _backend.UnloadOperationFactory = _ => new FakeSceneUnloadOperation { PendingPolls = 2 };
        var unload = _backend.UnloadSceneAsync(4);
        Assert.IsTrue(_backend.IsSceneLoaded(4));
        Assert.IsFalse(unload.IsDone);
        Assert.IsTrue(_backend.IsSceneLoaded(4));
        Assert.IsFalse(unload.IsDone);
        Assert.IsTrue(unload.IsDone);
        Assert.IsFalse(_backend.IsSceneLoaded(4));
    }

    [Test]
    public void FakeBackend_AdditiveLoadsHaveIndependentActivation()
    {
        var world = _backend.LoadSceneAsync(4, LoadSceneMode.Additive);
        var lobby = _backend.LoadSceneAsync(2, LoadSceneMode.Additive);

        world.AllowSceneActivation = true;
        Assert.IsTrue(world.IsDone);
        Assert.IsTrue(_backend.IsSceneLoaded(4));
        Assert.IsFalse(lobby.IsDone);
        Assert.IsFalse(_backend.IsSceneLoaded(2));

        lobby.AllowSceneActivation = true;
        Assert.IsTrue(lobby.IsDone);
        Assert.IsTrue(_backend.IsSceneLoaded(2));
    }

    [Test]
    public void RunAdditiveAsync_CancelWhileActivationPending_RollsBackBeforeCommit()
    {
        _backend.ActiveSceneBuildIndex = 1;
        _backend.LoadedScenes.Add(1);
        using var cts = new CancellationTokenSource();
        var op = new FakeSceneLoadOperation { ActivationYieldCount = 3 };
        _backend.LoadOperationFactory = _ => op;
        _timeProvider.OnYield = () =>
        {
            if (op.AllowSceneActivation) cts.Cancel();
        };

        var request = new AdditiveSceneTransitionRequest(4, new[] { 4 }, new[] { 1 }, 4);
        Assert.CatchAsync<OperationCanceledException>(async () => await _runner.RunAdditiveAsync(request, cts.Token));

        Assert.IsTrue(op.IsDone);
        Assert.IsFalse(_backend.IsSceneLoaded(4));
        Assert.IsTrue(_backend.IsSceneLoaded(1));
        Assert.AreEqual(1, _backend.ActiveSceneBuildIndex);
        Assert.IsFalse(_backend.Events.Contains("ActiveSet:4"));
    }

    [Test]
    public void RunAdditiveAsync_CancelAfterFinalLoadBeforeCommit_RollsBackBothScenes()
    {
        _backend.ActiveSceneBuildIndex = 1;
        _backend.LoadedScenes.Add(1);
        using var cts = new CancellationTokenSource();
        _loadingScreen.OnSetProgress = progress =>
        {
            if (progress >= 1f) cts.Cancel();
        };

        var request = new AdditiveSceneTransitionRequest(2, new[] { 4, 2 }, new[] { 1 }, 4);
        Assert.CatchAsync<OperationCanceledException>(async () => await _runner.RunAdditiveAsync(request, cts.Token));

        Assert.IsFalse(_backend.IsSceneLoaded(4));
        Assert.IsFalse(_backend.IsSceneLoaded(2));
        Assert.IsTrue(_backend.IsSceneLoaded(1));
        Assert.AreEqual(1, _backend.ActiveSceneBuildIndex);
        Assert.IsFalse(_backend.Events.Contains("ActiveSet:4"));
    }

    [Test]
    public async Task RunAdditiveAsync_CancelAfterCommit_StillUnloadsAndReveals()
    {
        _backend.ActiveSceneBuildIndex = 1;
        _backend.LoadedScenes.Add(1);
        using var cts = new CancellationTokenSource();
        _backend.OnSetActive = _ => cts.Cancel();

        var request = new AdditiveSceneTransitionRequest(4, new[] { 4 }, new[] { 1 }, 4);
        await _runner.RunAdditiveAsync(request, cts.Token);

        Assert.AreEqual(4, _backend.ActiveSceneBuildIndex);
        Assert.IsFalse(_backend.IsSceneLoaded(1));
        Assert.IsTrue(_backend.IsSceneLoaded(4));
        Assert.IsTrue(_loadingScreen.RecordedEvents.Contains("HideAsync"));
    }

    [Test]
    public void RunAdditiveAsync_SecondLoadFails_RollsBackFirstOnly()
    {
        _backend.ActiveSceneBuildIndex = 1;
        _backend.LoadedScenes.Add(1);
        _backend.LoadOperationFactory = index => index == 4 ? new FakeSceneLoadOperation() : null;

        var request = new AdditiveSceneTransitionRequest(2, new[] { 4, 2 }, new[] { 1 }, 4);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await _runner.RunAdditiveAsync(request));

        Assert.IsFalse(_backend.IsSceneLoaded(4));
        Assert.IsTrue(_backend.IsSceneLoaded(1));
        Assert.AreEqual(1, _backend.ActiveSceneBuildIndex);
    }

    [Test]
    public void RunAdditiveAsync_RollbackUnloadFails_ReportsOriginalAndCleanupError()
    {
        _backend.ActiveSceneBuildIndex = 1;
        _backend.LoadedScenes.Add(1);
        _backend.ThrowOnSetActive = true;
        _backend.UnloadOperationFactory = _ => null;

        var request = new AdditiveSceneTransitionRequest(4, new[] { 4 }, new[] { 1 }, 4);
        var error = Assert.ThrowsAsync<AggregateException>(async () => await _runner.RunAdditiveAsync(request));

        Assert.AreEqual(2, error.InnerExceptions.Count);
        StringAssert.Contains("set active scene", error.InnerExceptions[0].Message);
        StringAssert.Contains("scene 4", error.InnerExceptions[1].Message);
        Assert.IsTrue(_backend.IsSceneLoaded(4));
        Assert.IsTrue(_backend.IsSceneLoaded(1));
    }

    [Test]
    public async Task RunAdditiveAsync_EventOrder_RespectsLoadCommitAndUnload()
    {
        _backend.ActiveSceneBuildIndex = 1;
        _backend.LoadedScenes.Add(1);
        var request = new AdditiveSceneTransitionRequest(2, new[] { 4, 2 }, new[] { 1 }, 4);

        await _runner.RunAdditiveAsync(request);

        Assert.Less(_backend.Events.IndexOf("SceneLoaded:4"), _backend.Events.IndexOf("LoadStarted:2"));
        Assert.Less(_backend.Events.IndexOf("SceneLoaded:2"), _backend.Events.IndexOf("ActiveSet:4"));
        Assert.Less(_backend.Events.IndexOf("ActiveSet:4"), _backend.Events.IndexOf("UnloadStarted:1"));
        Assert.Less(_backend.Events.IndexOf("UnloadStarted:1"), _backend.Events.IndexOf("SceneUnloaded:1"));
    }

    [Test]
    public async Task RunCoveredOperationAsync_Success_ExecutesStrictSequence()
    {
        bool operationRan = false;
        await _runner.RunCoveredOperationAsync(async ct =>
        {
            Assert.Contains("Prepare", _loadingScreen.RecordedEvents);
            Assert.Contains("ShowAsync", _loadingScreen.RecordedEvents);
            Assert.IsFalse(_loadingScreen.RecordedEvents.Contains("HideAsync"));
            operationRan = true;
            await Task.Yield();
        });

        Assert.IsTrue(operationRan);
        var expected = new[] { "Prepare", "ShowAsync", "HideAsync" };
        CollectionAssert.AreEqual(expected, _loadingScreen.RecordedEvents);
        Assert.IsFalse(_runner.IsTransitioning);
        Assert.AreEqual(0, _loadingScreen.ResetImmediateCount);
    }

    [Test]
    public void RunCoveredOperationAsync_ThrowsWhenAlreadyTransitioning()
    {
        var request = new SceneLoadRequest(1);
        _loadingScreen.ShowTaskOverride = new TaskCompletionSource<bool>().Task;
        _ = _runner.RunAsync(request);

        Assert.IsTrue(_runner.IsTransitioning);
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _runner.RunCoveredOperationAsync(ct => Task.CompletedTask);
        });
    }

    [Test]
    public void RunCoveredOperationAsync_OnError_ResetsScreenAndRethrows()
    {
        var expectedEx = new InvalidOperationException("Operation failed");
        var actualEx = Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _runner.RunCoveredOperationAsync(ct => throw expectedEx);
        });

        Assert.AreSame(expectedEx, actualEx);
        Assert.AreEqual(1, _loadingScreen.ResetImmediateCount);
        Assert.IsFalse(_runner.IsTransitioning);
    }

    // --- Fakes ---

    private sealed class FakeLoadingScreen : ILoadingScreen
    {
        public readonly List<string> RecordedEvents = new List<string>();
        public readonly List<float> ProgressValues = new List<float>();
        public float LastProgress { get; private set; } = -1f;
        public int ResetImmediateCount { get; private set; }
        public SceneTransitionContext LastPreparedContext { get; private set; }

        public Action OnShowAsync;
        public Exception ThrowOnShow;
        public Exception ThrowOnHide;
        public Exception ThrowOnSetProgress;
        public Action<float> OnSetProgress;
        public Task ShowTaskOverride;

        public void Prepare(SceneTransitionContext context)
        {
            LastPreparedContext = context;
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
            OnSetProgress?.Invoke(normalizedProgress);
            if (ThrowOnSetProgress != null) throw ThrowOnSetProgress;
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
        public int ActiveSceneBuildIndex { get; set; } = 0;
        public string ActiveSceneName => $"Scene_{ActiveSceneBuildIndex}";
        public bool LoadCalled { get; private set; }
        public ISceneLoadOperation OperationToReturn { get; set; } = new FakeSceneLoadOperation();
        public Func<int, ISceneLoadOperation> LoadOperationFactory { get; set; }
        public Func<int, ISceneUnloadOperation> UnloadOperationFactory { get; set; }
        public Action<int> OnSetActive { get; set; }

        public readonly HashSet<int> LoadedScenes = new HashSet<int> { 0 };
        public readonly List<int> LoadedRecord = new List<int>();
        public readonly List<int> UnloadedRecord = new List<int>();
        public readonly List<string> Events = new List<string>();
        public bool ThrowOnSetActive { get; set; }
        public bool ThrowOnLoad { get; set; }

        public bool IsBuildIndexValid(int buildIndex) => buildIndex >= 0 && buildIndex <= 5;

        public string GetSceneNameByBuildIndex(int buildIndex) => $"Scene_{buildIndex}";

        public ISceneLoadOperation LoadSceneAsync(int buildIndex, LoadSceneMode mode)
        {
            if (ThrowOnLoad) throw new InvalidOperationException("Forced LoadSceneAsync exception");
            LoadCalled = true;
            LoadedRecord.Add(buildIndex);
            Events.Add($"LoadStarted:{buildIndex}");
            ISceneLoadOperation op = LoadOperationFactory != null
                ? LoadOperationFactory(buildIndex)
                : mode == LoadSceneMode.Additive ? new FakeSceneLoadOperation() : OperationToReturn;
            return op == null ? null : new TrackedLoadOperation(op, buildIndex, this);
        }

        public bool IsSceneLoaded(int buildIndex) => LoadedScenes.Contains(buildIndex);

        public ISceneUnloadOperation UnloadSceneAsync(int buildIndex)
        {
            UnloadedRecord.Add(buildIndex);
            Events.Add($"UnloadStarted:{buildIndex}");
            ISceneUnloadOperation op = UnloadOperationFactory != null
                ? UnloadOperationFactory(buildIndex)
                : new FakeSceneUnloadOperation();
            return op == null ? null : new TrackedUnloadOperation(op, buildIndex, this);
        }

        public bool TrySetActiveScene(int buildIndex)
        {
            if (ThrowOnSetActive) return false;
            if (!LoadedScenes.Contains(buildIndex)) return false;
            ActiveSceneBuildIndex = buildIndex;
            Events.Add($"ActiveSet:{buildIndex}");
            OnSetActive?.Invoke(buildIndex);
            return true;
        }

        private sealed class TrackedLoadOperation : ISceneLoadOperation
        {
            private readonly ISceneLoadOperation _inner;
            private readonly int _buildIndex;
            private readonly FakeSceneLoadingBackend _backend;
            private bool _recorded;

            public TrackedLoadOperation(ISceneLoadOperation inner, int buildIndex, FakeSceneLoadingBackend backend)
            {
                _inner = inner;
                _buildIndex = buildIndex;
                _backend = backend;
            }

            public float Progress => _inner.Progress;

            public bool IsDone
            {
                get
                {
                    bool done = _inner.IsDone;
                    if (done && _inner.AllowSceneActivation && !_recorded)
                    {
                        _recorded = true;
                        _backend.LoadedScenes.Add(_buildIndex);
                        _backend.Events.Add($"SceneLoaded:{_buildIndex}");
                    }
                    return done;
                }
            }

            public bool AllowSceneActivation
            {
                get => _inner.AllowSceneActivation;
                set
                {
                    _inner.AllowSceneActivation = value;
                    if (value) _backend.Events.Add($"ActivationReleased:{_buildIndex}");
                }
            }
        }

        private sealed class TrackedUnloadOperation : ISceneUnloadOperation
        {
            private readonly ISceneUnloadOperation _inner;
            private readonly int _buildIndex;
            private readonly FakeSceneLoadingBackend _backend;
            private bool _recorded;

            public TrackedUnloadOperation(ISceneUnloadOperation inner, int buildIndex, FakeSceneLoadingBackend backend)
            {
                _inner = inner;
                _buildIndex = buildIndex;
                _backend = backend;
            }

            public float Progress => _inner.Progress;

            public bool IsDone
            {
                get
                {
                    bool done = _inner.IsDone;
                    if (done && !_recorded)
                    {
                        _recorded = true;
                        _backend.LoadedScenes.Remove(_buildIndex);
                        _backend.Events.Add($"SceneUnloaded:{_buildIndex}");
                    }
                    return done;
                }
            }
        }
    }

    private sealed class FakeSceneUnloadOperation : ISceneUnloadOperation
    {
        private bool _done;
        public int PendingPolls { get; set; }
        public float Progress => _done ? 1f : 0.5f;
        public bool IsDone
        {
            get
            {
                if (_done) return true;
                if (PendingPolls > 0)
                {
                    PendingPolls--;
                    return false;
                }
                _done = true;
                return true;
            }
        }
    }

    private sealed class FakeSceneLoadOperation : ISceneLoadOperation
    {
        private int _progressIndex;
        private bool _allowActivation;
        private int _yieldsRemainingAfterActivation;
        private bool _done;

        public float[] CustomProgressSequence { get; set; } = new[] { 0.45f, 0.9f };
        public Action<bool> OnAllowSceneActivationSet { get; set; }
        public Action OnProgressPolled { get; set; }
        public int ActivationYieldCount { get; set; } = 0;

        public float Progress
        {
            get
            {
                OnProgressPolled?.Invoke();
                if (CustomProgressSequence != null && _progressIndex < CustomProgressSequence.Length)
                {
                    return CustomProgressSequence[_progressIndex++];
                }
                return 0.9f;
            }
        }

        public bool IsDone
        {
            get
            {
                if (_done) return true;
                if (!_allowActivation) return false;
                if (_yieldsRemainingAfterActivation > 0)
                {
                    _yieldsRemainingAfterActivation--;
                    return false;
                }
                _done = true;
                return true;
            }
        }

        public bool AllowSceneActivation
        {
            get => _allowActivation;
            set
            {
                _allowActivation = value;
                if (value && !_done)
                {
                    _yieldsRemainingAfterActivation = ActivationYieldCount;
                }
                OnAllowSceneActivationSet?.Invoke(value);
            }
        }
    }

    private sealed class FakeSceneTimeProvider : ISceneTimeProvider
    {
        public readonly List<float> DelayedDurations = new List<float>();
        public float RealtimeSinceStartup { get; set; } = 0f;
        public Exception ThrowOnDelay;
        public Exception ThrowOnYield;
        public Action OnYield;

        public Task DelayAsync(float seconds, CancellationToken cancellationToken)
        {
            DelayedDurations.Add(seconds);
            RealtimeSinceStartup += seconds;
            if (ThrowOnDelay != null) throw ThrowOnDelay;
            return Task.CompletedTask;
        }

        public Task YieldAsync(CancellationToken cancellationToken)
        {
            OnYield?.Invoke();
            if (ThrowOnYield != null) throw ThrowOnYield;
            return Task.CompletedTask;
        }
    }
}
