using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BlockController))]
public class BlockView : MonoBehaviour
{
    [SerializeField] private GameObject cellPrefab;


    private BlockController blockController;
    private IPoolService poolService;
    private List<GameObject> activeCells = new List<GameObject>();

    private void Awake()
    {
        blockController = GetComponent<BlockController>();

        // Đăng ký nghe ngay từ Awake để đảm bảo không bị lỡ nhịp khi Controller gọi OnEnable()

        blockController.OnShapeAssigned += DrawShape;
    }

    private void OnDestroy()
    {
        if (blockController != null)
        {
            blockController.OnShapeAssigned -= DrawShape;
        }
    }

    // XÓA hàm OnDisable() gọi ClearShape() đi vì:
    // Khi cục gạch mẹ bị trả về Pool (SetActive(false)), Unity cấm không cho phép
    // các cục gạch con thay đổi Parent (về Pool) trong lúc thằng mẹ đang bị tắt.
    // Cứ kệ tụi nó nằm im trong bụng mẹ. Lần sau lấy ra xài (DrawShape), nó sẽ tự dọn dẹp!

    private void DrawShape(List<(int x, int y)> offsets)
    {
        if (poolService == null) poolService = ServiceLocator.Get<IPoolService>();


        ClearShape(); // Dọn dẹp trước cho chắc ăn

        if (cellPrefab == null)
        {
            Debug.LogError("BlockView: Chưa gắn cellPrefab vào Inspector của " + gameObject.name);
            return;
        }

        Vector2 center = blockController != null ? blockController.CenterOffset : Vector2.zero;

        foreach (var offset in offsets)
        {
            // Bốc 1 cục gạch con từ kho
            GameObject cell = poolService.SpawnObject(cellPrefab, Vector3.zero, Quaternion.identity);

            // Gắn vào cục gạch mẹ (Block). Dùng tham số 'false' cực kỳ quan trọng!
            // 'false' cấm Unity tự động bóp méo localScale (1.3333) để bù trừ cho cái Scale 0.75 của thằng Cha.
            cell.transform.SetParent(this.transform, false);
            
            // Đảm bảo gạch con luôn mang kích thước gốc 1:1 (Thằng cha 0.75 thì world scale tự thành 0.75)
            cell.transform.localScale = Vector3.one;

            // Quy đổi tọa độ: X của mảng 2D -> X của Unity 3D, Y của mảng 2D -> Z của Unity 3D
            // Trừ đi center để căn giữa tâm khối gạch vào tâm GameObject
            cell.transform.localPosition = new Vector3(offset.x - center.x, 0, offset.y - center.y);

            // Lưu lại để xíu nữa trả về kho
            activeCells.Add(cell);
        }
    }

    private void ClearShape()
    {
        if (poolService == null) return;


        foreach (var cell in activeCells)
        {
            if (cell != null)
            {
                poolService.ReturnObjectToPool(cell);
            }
        }
        activeCells.Clear();
    }
}
