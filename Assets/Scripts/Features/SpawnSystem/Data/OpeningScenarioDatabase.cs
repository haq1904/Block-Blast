using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OpeningScenarioDatabase", menuName = "Block Blast/Opening Scenario Database")]
public sealed class OpeningScenarioDatabase : ScriptableObject
{
    [Tooltip("List of authored opening scenarios.")]
    public List<OpeningScenarioData> scenarios = new List<OpeningScenarioData>();

    public List<OpeningScenarioData> GetValidScenarios()
    {
        var valid = new List<OpeningScenarioData>();
        if (scenarios == null) return valid;

        for (int i = 0; i < scenarios.Count; i++)
        {
            if (scenarios[i] != null && scenarios[i].IsValidLayout())
            {
                valid.Add(scenarios[i]);
            }
        }
        return valid;
    }

    public bool TryGetRandomScenario(out OpeningScenarioData scenario)
    {
        var valid = GetValidScenarios();
        if (valid.Count == 0)
        {
            scenario = null;
            return false;
        }

        int index = Random.Range(0, valid.Count);
        scenario = valid[index];
        return true;
    }
}
