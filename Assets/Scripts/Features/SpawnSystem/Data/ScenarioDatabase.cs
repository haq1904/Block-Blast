using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ScenarioDatabase", menuName = "Block Blast/Scenario Database")]
public class ScenarioDatabase : ScriptableObject
{
    [Tooltip("List of all authored scenarios")]
    public List<ScenarioData> scenarios = new List<ScenarioData>();

    public ScenarioData GetRandomScenario()
    {
        if (scenarios == null || scenarios.Count == 0) return null;
        int randomIndex = Random.Range(0, scenarios.Count);
        return scenarios[randomIndex];
    }

    public List<ScenarioData> GetScenariosByType(ScenarioType type)
    {
        List<ScenarioData> result = new List<ScenarioData>();
        if (scenarios == null) return result;

        for (int i = 0; i < scenarios.Count; i++)
        {
            if (scenarios[i] != null && scenarios[i].scenarioType == type)
            {
                result.Add(scenarios[i]);
            }
        }
        return result;
    }
}
