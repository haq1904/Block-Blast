using System.Collections.Generic;
using UnityEngine;

namespace BlockBlast.Features.WorldCustomization
{
    public readonly struct WorldLineClearContext
    {
        public int TotalLinesCleared { get; }
        public IReadOnlyList<int> Rows { get; }
        public IReadOnlyList<int> Cols { get; }
        public IReadOnlyList<Vector3> ComboVFXPositions { get; }

        public WorldLineClearContext(
            int totalLinesCleared,
            IReadOnlyList<int> rows = null,
            IReadOnlyList<int> cols = null,
            IReadOnlyList<Vector3> comboVFXPositions = null)
        {
            TotalLinesCleared = totalLinesCleared;
            Rows = rows;
            Cols = cols;
            ComboVFXPositions = comboVFXPositions;
        }
    }

    public readonly struct WorldTableClearContext
    {
        public int TilesCleared { get; }
        public int TotalLinesCleared { get; }

        public WorldTableClearContext(int tilesCleared, int totalLinesCleared)
        {
            TilesCleared = tilesCleared;
            TotalLinesCleared = totalLinesCleared;
        }
    }

    public readonly struct WorldComboContext
    {
        public int CurrentCombo { get; }

        public WorldComboContext(int currentCombo)
        {
            CurrentCombo = currentCombo;
        }
    }

    public readonly struct WorldStressContext
    {
        public bool IsInStress { get; }
        public float OccupancyRatio { get; }

        public WorldStressContext(bool isInStress, float occupancyRatio = 0f)
        {
            IsInStress = isInStress;
            OccupancyRatio = occupancyRatio;
        }
    }
}
