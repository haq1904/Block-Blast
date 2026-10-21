using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class BlockSpawnEntranceTest
{
    private GameObject blockObject;
    private BlockController controller;

    [SetUp]
    public void SetUp()
    {
        blockObject = new GameObject("BlockSpawnEntranceTest");
        controller = blockObject.AddComponent<BlockController>();
    }

    [TearDown]
    public void TearDown()
    {
        if (blockObject != null)
        {
            UnityEngine.Object.DestroyImmediate(blockObject);
        }
    }

    [Test]
    public void Setup_WithSpawnEntrance_LocksInputUntilViewCompletion()
    {
        Action completion = null;
        int receivedSlot = -1;
        Vector3 receivedTrayPosition = Vector3.zero;

        controller.OnSpawnEntranceRequested += (slotIndex, trayPosition, onComplete) =>
        {
            receivedSlot = slotIndex;
            receivedTrayPosition = trayPosition;
            completion = onComplete;
        };

        Vector3 trayPosition = new Vector3(2f, -1f, -4f);
        controller.Setup(CreateBlockModel(), trayPosition, 2, playSpawnEntrance: true);

        Assert.IsFalse(controller.IsInteractionEnabled);
        Assert.AreEqual(2, receivedSlot);
        Assert.AreEqual(trayPosition, receivedTrayPosition);
        Assert.IsNotNull(completion);

        completion.Invoke();

        Assert.IsTrue(controller.IsInteractionEnabled);
    }

    [Test]
    public void Setup_WithSpawnEntranceAndNoViewSubscriber_FailsSafeToEnabledInput()
    {
        controller.Setup(CreateBlockModel(), Vector3.zero, 0, playSpawnEntrance: true);

        Assert.IsTrue(controller.IsInteractionEnabled);
    }

    [Test]
    public void Setup_WithoutSpawnEntrance_EnablesInputWithoutRequest()
    {
        bool receivedRequest = false;
        controller.OnSpawnEntranceRequested += (_, _, _) => receivedRequest = true;

        controller.Setup(CreateBlockModel(), Vector3.zero, 1);

        Assert.IsTrue(controller.IsInteractionEnabled);
        Assert.IsFalse(receivedRequest);
    }

    [Test]
    public void SpawnEntranceOrder_UsesCenterThenLeftThenRight()
    {
        MethodInfo orderMethod = typeof(BlockView).GetMethod(
            "GetSpawnEntranceOrder",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.IsNotNull(orderMethod);
        Assert.AreEqual(0, orderMethod.Invoke(null, new object[] { 2 }));
        Assert.AreEqual(1, orderMethod.Invoke(null, new object[] { 0 }));
        Assert.AreEqual(2, orderMethod.Invoke(null, new object[] { 1 }));
    }

    private static BlockModel CreateBlockModel()
    {
        return new BlockModel(new List<(int x, int y)>
        {
            (0, 0)
        });
    }
}
