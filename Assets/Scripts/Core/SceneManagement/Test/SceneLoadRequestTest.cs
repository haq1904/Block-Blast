using System;
using NUnit.Framework;
using UnityEngine.SceneManagement;

[TestFixture]
public class SceneLoadRequestTest
{
    [Test]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        var request = new SceneLoadRequest(1, LoadSceneMode.Single, 0.5f, SceneTransitionPresentation.None);

        Assert.AreEqual(1, request.SceneBuildIndex);
        Assert.AreEqual(LoadSceneMode.Single, request.LoadMode);
        Assert.AreEqual(0.5f, request.MinimumVisibleDuration, 0.0001f);
        Assert.AreEqual(SceneTransitionPresentation.None, request.Presentation);
    }

    [Test]
    public void Constructor_WithDefaultParameters_SetsDefaultsCorrectly()
    {
        var request = new SceneLoadRequest(2);

        Assert.AreEqual(2, request.SceneBuildIndex);
        Assert.AreEqual(LoadSceneMode.Single, request.LoadMode);
        Assert.AreEqual(0f, request.MinimumVisibleDuration, 0.0001f);
        Assert.AreEqual(SceneTransitionPresentation.LoadingScreen, request.Presentation);
    }

    [Test]
    public void Constructor_WithNegativeBuildIndex_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new SceneLoadRequest(-1, LoadSceneMode.Single, 0f);
        });
    }

    [Test]
    public void Constructor_WithNegativeMinimumDuration_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new SceneLoadRequest(1, LoadSceneMode.Single, -0.1f);
        });
    }

    [Test]
    public void Constructor_WithAdditiveMode_PreservesMode()
    {
        var request = new SceneLoadRequest(3, LoadSceneMode.Additive, 1.0f);

        Assert.AreEqual(LoadSceneMode.Additive, request.LoadMode);
    }

    [Test]
    public void Constructor_WithNaNMinimumDuration_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new SceneLoadRequest(1, LoadSceneMode.Single, float.NaN);
        });
    }

    [Test]
    public void Constructor_WithPositiveInfinityMinimumDuration_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new SceneLoadRequest(1, LoadSceneMode.Single, float.PositiveInfinity);
        });
    }

    [Test]
    public void Constructor_WithNegativeInfinityMinimumDuration_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new SceneLoadRequest(1, LoadSceneMode.Single, float.NegativeInfinity);
        });
    }

    [Test]
    public void Equals_WithIdenticalValues_ReturnsTrue()
    {
        var req1 = new SceneLoadRequest(1, LoadSceneMode.Single, 0.5f, SceneTransitionPresentation.LoadingScreen);
        var req2 = new SceneLoadRequest(1, LoadSceneMode.Single, 0.5f, SceneTransitionPresentation.LoadingScreen);

        Assert.IsTrue(req1.Equals(req2));
        Assert.IsTrue(req1.Equals((object)req2));
        Assert.AreEqual(req1.GetHashCode(), req2.GetHashCode());
    }

    [Test]
    public void Equals_WithDifferentMinimumDuration_ReturnsFalse()
    {
        var req1 = new SceneLoadRequest(1, LoadSceneMode.Single, 0.5f);
        var req2 = new SceneLoadRequest(1, LoadSceneMode.Single, 0.50005f);

        Assert.IsFalse(req1.Equals(req2));
        Assert.IsFalse(req1.Equals((object)req2));
    }

    [Test]
    public void Equals_WithDifferentPresentation_ReturnsFalse()
    {
        var req1 = new SceneLoadRequest(1, LoadSceneMode.Single, 0.5f, SceneTransitionPresentation.LoadingScreen);
        var req2 = new SceneLoadRequest(1, LoadSceneMode.Single, 0.5f, SceneTransitionPresentation.None);

        Assert.IsFalse(req1.Equals(req2));
        Assert.IsFalse(req1.Equals((object)req2));
    }

    [Test]
    public void Equals_WithDifferentIndexOrMode_ReturnsFalse()
    {
        var req1 = new SceneLoadRequest(1, LoadSceneMode.Single, 0.5f);
        var req2 = new SceneLoadRequest(2, LoadSceneMode.Single, 0.5f);
        var req3 = new SceneLoadRequest(1, LoadSceneMode.Additive, 0.5f);

        Assert.IsFalse(req1.Equals(req2));
        Assert.IsFalse(req1.Equals(req3));
        Assert.IsFalse(req1.Equals(null));
        Assert.IsFalse(req1.Equals("some string"));
    }

    [Test]
    public void ToString_IncludesPresentation()
    {
        var request = new SceneLoadRequest(1, LoadSceneMode.Single, 0f, SceneTransitionPresentation.None);
        StringAssert.Contains("Presentation: None", request.ToString());
    }
}
