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
            new Vector3(0f, -1f, -4.7f)
        };
        CurrentBatch = new BlockModel[3];
        IsSlotEmpty = new bool[3] { true, true, true };
    }

    public void SetBatch(BlockModel[] batch)
    {
        for (int i = 0; i < 3; i++)
        {
            CurrentBatch[i] = (batch != null && i < batch.Length) ? batch[i] : null;
            IsSlotEmpty[i] = (CurrentBatch[i] == null);
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

    // Scenario Sequence State (< 250,000 points)
    public int ActiveScenarioId { get; set; } = -1;
    public int ScenarioStepIndex { get; set; } = 0;
    public int ActiveScenarioAngle { get; set; } = 0;
    public bool ActiveScenarioMirror { get; set; } = false;

    public void ResetScenario()
    {
        ActiveScenarioId = -1;
        ScenarioStepIndex = 0;
        ActiveScenarioAngle = 0;
        ActiveScenarioMirror = false;
    }
}
