using UnityEngine;

public class ShapeDBService : MonoBehaviour
{
    [SerializeField] private ShapeDatabase shapeDatabase;

    private void Awake()
    {
        if (shapeDatabase != null)
        {
            ServiceLocator.Register<ShapeDatabase>(shapeDatabase);
        }
        else
        {
            Debug.LogError("[ShapeDBService] Chưa gán MasterShapeDatabase vào Inspector! Hãy kéo thả file MasterShapeDatabase vào component này.");
        }
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<ShapeDatabase>();
    }
}
