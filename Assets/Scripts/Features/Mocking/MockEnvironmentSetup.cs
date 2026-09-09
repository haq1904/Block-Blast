#if UNITY_EDITOR
using UnityEngine;
using BlockBlast.Mocking;

/// <summary>
/// Tool used to inject mock objects into Play Mode.
/// DefaultExecutionOrder(-100) ensures it awakes before other systems.
/// </summary>
[DefaultExecutionOrder(-100)]
public class MockEnvironmentSetup : MonoBehaviour
{
    [Header("Testing Toggles")]
    [Tooltip("If true, completely disables real GridController and injects FakeFullGrid to force Game Over.")]
    public bool simulateFullGrid = false;

    private void Awake()
    {
        if (simulateFullGrid)
        {
            InjectFakeFullGrid();
        }
    }

    private void InjectFakeFullGrid()
    {
        // 1. Find and disable the real GridController before it runs Awake/Start
        GridController realGrid = FindObjectOfType<GridController>();
        if (realGrid != null)
        {
            realGrid.gameObject.SetActive(false);
            Debug.Log("[MockSetup] Disabled real GridController.");
        }

        // 2. Unregister previous IGridService if already registered
        ServiceLocator.Unregister<IGridService>();

        // 3. Inject mock grid into ServiceLocator
        ServiceLocator.Register<IGridService>(new FakeFullGrid());
        Debug.Log("[MockSetup] Injected FakeFullGrid into ServiceLocator successfully!");
    }
}
#endif
