using System;
using NUnit.Framework;
using UnityEngine.SceneManagement;

[TestFixture]
public class SceneLoadRequestTest
{
    [Test]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        var request = new SceneLoadRequest(1, LoadSceneMode.Single, 0.5f);

        Assert.AreEqual(1, request.SceneBuildIndex);
        Assert.AreEqual(LoadSceneMode.Single, request.LoadMode);
        Assert.AreEqual(0.5f, request.MinimumVisibleDuration, 0.0001f);
    }

    [Test]
    public void Constructor_WithDefaultParameters_SetsDefaultsCorrectly()
    {
        var request = new SceneLoadRequest(2);

        Assert.AreEqual(2, request.SceneBuildIndex);
        Assert.AreEqual(LoadSceneMode.Single, request.LoadMode);
        Assert.AreEqual(0f, request.MinimumVisibleDuration, 0.0001f);
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
    public void Equals_WithIdenticalValues_ReturnsTrue()
    {
        var req1 = new SceneLoadRequest(1, LoadSceneMode.Single, 0.5f);
        var req2 = new SceneLoadRequest(1, LoadSceneMode.Single, 0.5f);

        Assert.IsTrue(req1.Equals(req2));
        Assert.AreEqual(req1.GetHashCode(), req2.GetHashCode());
    }
}
