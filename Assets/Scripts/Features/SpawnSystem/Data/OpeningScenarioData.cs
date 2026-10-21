using System;
using System.Collections.Generic;
using UnityEngine;

public enum OpeningGenerationMode
{
    LegacyScenarioChain,
    IntelligentOnly
}

public readonly struct OpeningScenarioSelection
{
    public readonly OpeningScenarioData Data;
    public readonly int Angle;
    public readonly bool MirrorX;

    public OpeningScenarioSelection(OpeningScenarioData data, int angle, bool mirrorX)
    {
        Data = data;
        Angle = angle;
        MirrorX = mirrorX;
    }

    public List<Vector2Int> GetOccupiedCells()
    {
        return Data != null ? Data.GetOccupiedCells(Angle, MirrorX) : new List<Vector2Int>();
    }
}

[CreateAssetMenu(fileName = "New Opening Scenario", menuName = "Block Blast/Opening Scenario Data")]
public sealed class OpeningScenarioData : ScriptableObject
{
    [Tooltip("Descriptive name for this opening scenario layout.")]
    public string scenarioName;

    [Tooltip("Allow random 90/180/270 degree rotation when selecting this scenario.")]
    public bool allowRotation = true;

    [Tooltip("Allow horizontal mirroring (flip X) when selecting this scenario.")]
    public bool allowMirror = true;

    [SerializeField] private bool[] occupiedCells = new bool[64];

    public int OccupiedCellCount
    {
        get
        {
            if (occupiedCells == null) return 0;
            int count = 0;
            for (int i = 0; i < occupiedCells.Length; i++)
            {
                if (occupiedCells[i]) count++;
            }
            return count;
        }
    }

    public float OccupancyRatio => (float)OccupiedCellCount / 64f;

    public bool[] RawCells => occupiedCells;

    public bool GetCell(int col, int row)
    {
        if (col < 0 || col >= 8 || row < 0 || row >= 8 || occupiedCells == null || occupiedCells.Length != 64)
        {
            return false;
        }
        return occupiedCells[row * 8 + col];
    }

    public void SetCell(int col, int row, bool occupied)
    {
        if (col < 0 || col >= 8 || row < 0 || row >= 8) return;
        EnsureArraySize();
        occupiedCells[row * 8 + col] = occupied;
    }

    public void SetCells(bool[] cells)
    {
        if (cells == null || cells.Length != 64) return;
        EnsureArraySize();
        Array.Copy(cells, occupiedCells, 64);
    }

    public void EnsureArraySize()
    {
        if (occupiedCells == null || occupiedCells.Length != 64)
        {
            var old = occupiedCells;
            occupiedCells = new bool[64];
            if (old != null)
            {
                int copyLen = Mathf.Min(old.Length, 64);
                Array.Copy(old, occupiedCells, copyLen);
            }
        }
    }

    public bool IsValidLayout(int width = 8, int height = 8)
    {
        if (width != 8 || height != 8) return false;
        if (occupiedCells == null || occupiedCells.Length != 64) return false;

        int count = OccupiedCellCount;
        // Occupancy must be within [32, 44] (50% - 68.75%)
        if (count < 32 || count > 44) return false;

        // Check no row is completely full (8 cells)
        for (int r = 0; r < 8; r++)
        {
            int rowCount = 0;
            for (int c = 0; c < 8; c++)
            {
                if (occupiedCells[r * 8 + c]) rowCount++;
            }
            if (rowCount == 8) return false;
        }

        // Check no column is completely full (8 cells)
        for (int c = 0; c < 8; c++)
        {
            int colCount = 0;
            for (int r = 0; r < 8; r++)
            {
                if (occupiedCells[r * 8 + c]) colCount++;
            }
            if (colCount == 8) return false;
        }

        return true;
    }

    public List<Vector2Int> GetOccupiedCells(int angle = 0, bool mirrorX = false)
    {
        EnsureArraySize();
        var result = new List<Vector2Int>(OccupiedCellCount);

        bool effectiveMirror = allowMirror && mirrorX;
        int effectiveAngle = allowRotation ? (((angle % 360) + 360) % 360) : 0;

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if (!occupiedCells[row * 8 + col]) continue;

                int x = effectiveMirror ? (7 - col) : col;
                int y = row;

                Vector2Int transformed;
                switch (effectiveAngle)
                {
                    case 90:
                        transformed = new Vector2Int(y, 7 - x);
                        break;
                    case 180:
                        transformed = new Vector2Int(7 - x, 7 - y);
                        break;
                    case 270:
                        transformed = new Vector2Int(7 - y, x);
                        break;
                    default:
                        transformed = new Vector2Int(x, y);
                        break;
                }

                result.Add(transformed);
            }
        }

        return result;
    }
}

public static class OpeningScenarioSelector
{
    public static OpeningGenerationMode SelectMode(
        SpawnConfiguration config,
        float normalizedRoll)
    {
        if (config == null || config.openingScenarioDatabase == null)
        {
            return OpeningGenerationMode.LegacyScenarioChain;
        }

        float chance = Mathf.Clamp01(config.prefilledOpeningChance);
        if (chance <= 0f)
        {
            return OpeningGenerationMode.LegacyScenarioChain;
        }

        if (normalizedRoll < chance)
        {
            var valid = config.openingScenarioDatabase.GetValidScenarios();
            if (valid != null && valid.Count > 0)
            {
                return OpeningGenerationMode.IntelligentOnly;
            }
        }

        return OpeningGenerationMode.LegacyScenarioChain;
    }

    public static bool TrySelectScenario(
        OpeningScenarioDatabase database,
        out OpeningScenarioSelection selection)
    {
        if (database == null)
        {
            selection = default;
            return false;
        }

        var valid = database.GetValidScenarios();
        if (valid == null || valid.Count == 0)
        {
            selection = default;
            return false;
        }

        int index = UnityEngine.Random.Range(0, valid.Count);
        OpeningScenarioData data = valid[index];

        int angle = 0;
        if (data.allowRotation)
        {
            int[] angles = { 0, 90, 180, 270 };
            angle = angles[UnityEngine.Random.Range(0, angles.Length)];
        }

        bool mirrorX = data.allowMirror && (UnityEngine.Random.value > 0.5f);
        selection = new OpeningScenarioSelection(data, angle, mirrorX);
        return true;
    }

    public static bool TrySelectScenario(
        OpeningScenarioDatabase database,
        int scenarioIndex,
        int angle,
        bool mirrorX,
        out OpeningScenarioSelection selection)
    {
        if (database == null)
        {
            selection = default;
            return false;
        }

        var valid = database.GetValidScenarios();
        if (valid == null || valid.Count == 0 || scenarioIndex < 0 || scenarioIndex >= valid.Count)
        {
            selection = default;
            return false;
        }

        OpeningScenarioData data = valid[scenarioIndex];
        int effAngle = data.allowRotation ? angle : 0;
        bool effMirror = data.allowMirror && mirrorX;

        selection = new OpeningScenarioSelection(data, effAngle, effMirror);
        return true;
    }
}
