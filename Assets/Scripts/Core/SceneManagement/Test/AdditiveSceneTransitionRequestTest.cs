using System;
using NUnit.Framework;

[TestFixture]
public class AdditiveSceneTransitionRequestTest
{
    [Test]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        var request = new AdditiveSceneTransitionRequest(
            targetSceneBuildIndex: 2,
            loadBuildIndexes: new[] { 4, 2 },
            unloadBuildIndexes: new[] { 1, 3 },
            activeSceneBuildIndex: 4,
            minimumVisibleDuration: 0.5f,
            presentation: SceneTransitionPresentation.LoadingScreen);

        Assert.AreEqual(2, request.TargetSceneBuildIndex);
        Assert.AreEqual(4, request.ActiveSceneBuildIndex);
        Assert.AreEqual(0.5f, request.MinimumVisibleDuration, 0.0001f);
        Assert.AreEqual(SceneTransitionPresentation.LoadingScreen, request.Presentation);
        CollectionAssert.AreEqual(new[] { 4, 2 }, request.LoadBuildIndexes);
        CollectionAssert.AreEqual(new[] { 1, 3 }, request.UnloadBuildIndexes);
    }

    [Test]
    public void Constructor_WithNegativeTargetBuildIndex_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new AdditiveSceneTransitionRequest(-1, new[] { 1 }, new int[0], 1);
        });
    }

    [Test]
    public void Constructor_WithNegativeActiveBuildIndex_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new AdditiveSceneTransitionRequest(1, new[] { 1 }, new int[0], -1);
        });
    }

    [Test]
    public void Constructor_WithNullLoadIndexes_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new AdditiveSceneTransitionRequest(1, null, new int[0], 1);
        });
    }

    [Test]
    public void Constructor_WithNullUnloadIndexes_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new AdditiveSceneTransitionRequest(1, new int[0], null, 1);
        });
    }

    [Test]
    public void Constructor_WithNegativeIndexInLoadList_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new AdditiveSceneTransitionRequest(1, new[] { 1, -2 }, new int[0], 1);
        });
    }

    [Test]
    public void Constructor_WithNegativeIndexInUnloadList_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new AdditiveSceneTransitionRequest(1, new[] { 1 }, new[] { -3 }, 1);
        });
    }

    [Test]
    public void Constructor_WithDuplicateInLoadList_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new AdditiveSceneTransitionRequest(1, new[] { 2, 2 }, new int[0], 1);
        });
    }

    [Test]
    public void Constructor_WithDuplicateInUnloadList_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new AdditiveSceneTransitionRequest(1, new[] { 1 }, new[] { 3, 3 }, 1);
        });
    }

    [Test]
    public void Constructor_WithOverlapBetweenLoadAndUnload_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new AdditiveSceneTransitionRequest(1, new[] { 2, 3 }, new[] { 3, 4 }, 2);
        });
    }

    [Test]
    public void Constructor_WithActiveSceneInUnloadList_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new AdditiveSceneTransitionRequest(1, new[] { 2 }, new[] { 3 }, 3);
        });
    }

    [Test]
    public void Constructor_WithInvalidMinimumDuration_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new AdditiveSceneTransitionRequest(1, new[] { 1 }, new int[0], 1, -0.1f);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new AdditiveSceneTransitionRequest(1, new[] { 1 }, new int[0], 1, float.NaN);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new AdditiveSceneTransitionRequest(1, new[] { 1 }, new int[0], 1, float.PositiveInfinity);
        });
    }

    [Test]
    public void Equals_WithIdenticalRequests_ReturnsTrue()
    {
        var req1 = new AdditiveSceneTransitionRequest(2, new[] { 4, 2 }, new[] { 3 }, 4, 0.5f, SceneTransitionPresentation.None);
        var req2 = new AdditiveSceneTransitionRequest(2, new[] { 4, 2 }, new[] { 3 }, 4, 0.5f, SceneTransitionPresentation.None);

        Assert.IsTrue(req1.Equals(req2));
        Assert.IsTrue(req1.Equals((object)req2));
        Assert.AreEqual(req1.GetHashCode(), req2.GetHashCode());
    }

    [Test]
    public void Equals_WithDifferentProperties_ReturnsFalse()
    {
        var req1 = new AdditiveSceneTransitionRequest(2, new[] { 4, 2 }, new[] { 3 }, 4, 0.5f);
        var req2 = new AdditiveSceneTransitionRequest(3, new[] { 4, 2 }, new[] { 3 }, 4, 0.5f);
        var req3 = new AdditiveSceneTransitionRequest(2, new[] { 4 }, new[] { 3 }, 4, 0.5f);
        var req4 = new AdditiveSceneTransitionRequest(2, new[] { 4, 2 }, new[] { 1 }, 4, 0.5f);

        Assert.IsFalse(req1.Equals(req2));
        Assert.IsFalse(req1.Equals(req3));
        Assert.IsFalse(req1.Equals(req4));
        Assert.IsFalse(req1.Equals(null));
    }

    [Test]
    public void ToString_IncludesDetails()
    {
        var req = new AdditiveSceneTransitionRequest(2, new[] { 4, 2 }, new[] { 3 }, 4, 0.5f, SceneTransitionPresentation.LoadingScreen);
        string str = req.ToString();
        StringAssert.Contains("Target: 2", str);
        StringAssert.Contains("Active: 4", str);
        StringAssert.Contains("Load: [4, 2]", str);
        StringAssert.Contains("Unload: [3]", str);
    }
}
