using UnityEngine;

public class SpawnModel
{
    public Vector3[] TrayPositions { get; private set; }
    public BlockModel[] CurrentBatch { get; private set; }
    public bool[] IsSlotEmpty { get; private set; }

    public SpawnModel()
    {
        TrayPositions = new Vector3[]
        {
            new Vector3(-2.5f, -1f, -2f),
            new Vector3(2.5f, -1f, -2f),
            new Vector3(0f, -1f, -6.5f)
        };
        CurrentBatch = new BlockModel[3];
        IsSlotEmpty = new bool[3] { true, true, true };
    }

    public void SetBatch(BlockModel[] batch)
    {
        for (int i = 0; i < 3; i++)
        {
            CurrentBatch[i] = batch[i];
            IsSlotEmpty[i] = false; // Khi có batch mới, slot không còn trống
        }
    }

    public void MarkSlotEmpty(int index)
    {
        if (index >= 0 && index < 3)
        {
            IsSlotEmpty[index] = true;
            CurrentBatch[index] = null;
        }
    }

    public bool IsTrayEmpty()
    {
        return IsSlotEmpty[0] && IsSlotEmpty[1] && IsSlotEmpty[2];
    }

    // Scenario Sequence State (< 200,000 points)
    public int ActiveScenarioId { get; set; } = -1;
    public int ScenarioStepIndex { get; set; } = 0;

    public void ResetScenario()
    {
        ActiveScenarioId = -1;
        ScenarioStepIndex = 0;
    }
}
