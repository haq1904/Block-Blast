using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridDebugger : MonoBehaviour
{
    private IGridService gridService;

    void Start()
    {
        // Wait 1 frame to ensure GridController has registered the service
        Invoke(nameof(Init), 0.1f);
    }

    void Init()
    {
        gridService = ServiceLocator.Get<IGridService>();
        Debug.Log("[GridDebugger] Connected to GridController! Press 1, 2, 3, 4, 5, 6, 7, 8 to test.");
    }

    void Update()
    {
        if (gridService == null) return;
        if (Keyboard.current == null) return; // Guard against headless environments without a keyboard

        // Key 1: Enable ghost preview on first 3 cells of row 0
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            List<Vector2Int> testPos = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
            gridService.RequestPreview(testPos);
        }

        // Key 2: Intentionally send out-of-bounds coordinates to test preview dismissal
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            List<Vector2Int> badPos = new List<Vector2Int> { new Vector2Int(9, 9) };
            gridService.RequestPreview(badPos);
        }

        // Key 3: Place real blocks on first 3 cells of row 0
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            List<Vector2Int> testPos = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
            gridService.PlaceBlocks(testPos);
            // Dismiss ghost preview after placing
            gridService.RequestPreview(new List<Vector2Int>());
        }

        // Key 4: Place remaining 5 blocks on row 0 to trigger line clear
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            List<Vector2Int> remainingBlocks = new List<Vector2Int>();
            for (int i = 3; i < 8; i++) remainingBlocks.Add(new Vector2Int(i, 0));
            gridService.PlaceBlocks(remainingBlocks);
        }

        // --- ADDITIONAL SCENARIO TESTS ---

        // Key 5: Setup vertical column. Place 7 blocks on Col 7 (leave top cell [7,7] empty)
        if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            List<Vector2Int> colBlocks = new List<Vector2Int>();
            for (int i = 0; i < 7; i++) colBlocks.Add(new Vector2Int(7, i));
            gridService.PlaceBlocks(colBlocks);
        }

        // Key 6: Trigger vertical column clear. Place final block on [7,7]
        if (Keyboard.current.digit6Key.wasPressedThisFrame)
        {
            gridService.PlaceBlocks(new List<Vector2Int> { new Vector2Int(7, 7) });
        }

        // Key 7: Setup 2-row combo. Fill Row 2 and Row 3, leaving Col 4 empty
        if (Keyboard.current.digit7Key.wasPressedThisFrame)
        {
            List<Vector2Int> comboBlocks = new List<Vector2Int>();
            for (int col = 0; col < 8; col++)
            {
                if (col == 4) continue; // Leave middle gap
                comboBlocks.Add(new Vector2Int(col, 2));
                comboBlocks.Add(new Vector2Int(col, 3));
            }
            gridService.PlaceBlocks(comboBlocks);
        }

        // Key 8: Trigger 2-row combo clear. Drop 1x2 vertical block into Col 4 gap
        if (Keyboard.current.digit8Key.wasPressedThisFrame)
        {
            gridService.PlaceBlocks(new List<Vector2Int> { new Vector2Int(4, 2), new Vector2Int(4, 3) });
        }
    }
}
