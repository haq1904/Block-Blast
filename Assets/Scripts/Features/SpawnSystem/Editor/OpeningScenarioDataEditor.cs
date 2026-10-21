using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OpeningScenarioData))]
public sealed class OpeningScenarioDataEditor : Editor
{
    private enum PaintTool
    {
        Toggle = 0,
        PaintOccupied = 1,
        PaintEmpty = 2
    }

    private static readonly Color OccupiedColor = new Color(1f, 0.55f, 0.15f);
    private static readonly Color EmptyColor = new Color(0.24f, 0.24f, 0.28f);
    private static readonly Color ActiveToolColor = new Color(0.3f, 0.75f, 1f);

    private SerializedProperty scenarioNameProp;
    private SerializedProperty allowRotationProp;
    private SerializedProperty allowMirrorProp;
    private SerializedProperty occupiedCellsProp;

    private PaintTool currentTool = PaintTool.Toggle;

    private void OnEnable()
    {
        scenarioNameProp = serializedObject.FindProperty("scenarioName");
        allowRotationProp = serializedObject.FindProperty("allowRotation");
        allowMirrorProp = serializedObject.FindProperty("allowMirror");
        occupiedCellsProp = serializedObject.FindProperty("occupiedCells");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        OpeningScenarioData scenario = (OpeningScenarioData)target;
        scenario.EnsureArraySize();

        // 1. Basic Information
        EditorGUILayout.LabelField("1. Scenario Information", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(scenarioNameProp);
        EditorGUILayout.PropertyField(allowRotationProp);
        EditorGUILayout.PropertyField(allowMirrorProp);

        EditorGUILayout.Space(10);

        // 2. Paint Tool
        EditorGUILayout.LabelField("2. Paint Tool", EditorStyles.boldLabel);
        DrawPaintToolbar();

        EditorGUILayout.Space(8);

        // 3. 8x8 Grid Layout
        EditorGUILayout.LabelField("3. Board Layout (8x8 Grid)", EditorStyles.boldLabel);
        DrawGrid(scenario);

        EditorGUILayout.Space(8);

        // 4. Grid Actions
        DrawActionButtons(scenario);

        EditorGUILayout.Space(10);

        // 5. Statistics & Validation
        DrawStatsAndValidation(scenario);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPaintToolbar()
    {
        GUILayout.BeginHorizontal();
        DrawToolButton("Toggle Click", PaintTool.Toggle);
        DrawToolButton("Paint Occupied", PaintTool.PaintOccupied);
        DrawToolButton("Paint Empty", PaintTool.PaintEmpty);
        GUILayout.EndHorizontal();
    }

    private void DrawToolButton(string label, PaintTool tool)
    {
        Color defaultBg = GUI.backgroundColor;
        if (currentTool == tool)
        {
            GUI.backgroundColor = ActiveToolColor;
        }

        if (GUILayout.Button(label, GUILayout.Height(24)))
        {
            currentTool = tool;
        }

        GUI.backgroundColor = defaultBg;
    }

    private void DrawGrid(OpeningScenarioData scenario)
    {
        GUILayout.BeginVertical("box");

        // Column headers 0..7
        GUILayout.BeginHorizontal();
        GUILayout.Space(26);
        GUILayout.FlexibleSpace();
        for (int col = 0; col < 8; col++)
        {
            GUILayout.Label(col.ToString(), EditorStyles.miniBoldLabel, GUILayout.Width(34), GUILayout.Height(16));
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        // Rows from 7 down to 0
        for (int row = 7; row >= 0; row--)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label($"R{row}", EditorStyles.miniLabel, GUILayout.Width(26), GUILayout.Height(34));

            for (int col = 0; col < 8; col++)
            {
                int index = row * 8 + col;
                SerializedProperty cellProp = occupiedCellsProp.GetArrayElementAtIndex(index);

                Color defaultBg = GUI.backgroundColor;
                GUI.backgroundColor = cellProp.boolValue ? OccupiedColor : EmptyColor;

                if (GUILayout.Button("", GUILayout.Width(34), GUILayout.Height(34)))
                {
                    ApplyPaint(cellProp);
                }

                GUI.backgroundColor = defaultBg;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
    }

    private void ApplyPaint(SerializedProperty cellProp)
    {
        switch (currentTool)
        {
            case PaintTool.Toggle:
                cellProp.boolValue = !cellProp.boolValue;
                break;
            case PaintTool.PaintOccupied:
                cellProp.boolValue = true;
                break;
            case PaintTool.PaintEmpty:
                cellProp.boolValue = false;
                break;
        }
    }

    private void DrawActionButtons(OpeningScenarioData scenario)
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All", GUILayout.Height(22)))
        {
            for (int i = 0; i < 64; i++) occupiedCellsProp.GetArrayElementAtIndex(i).boolValue = false;
        }
        if (GUILayout.Button("Fill All", GUILayout.Height(22)))
        {
            for (int i = 0; i < 64; i++) occupiedCellsProp.GetArrayElementAtIndex(i).boolValue = true;
        }
        if (GUILayout.Button("Invert", GUILayout.Height(22)))
        {
            for (int i = 0; i < 64; i++)
            {
                var p = occupiedCellsProp.GetArrayElementAtIndex(i);
                p.boolValue = !p.boolValue;
            }
        }
        if (GUILayout.Button("Rotate 90°", GUILayout.Height(22)))
        {
            RotateGrid90();
        }
        if (GUILayout.Button("Mirror X", GUILayout.Height(22)))
        {
            MirrorGridX();
        }
        GUILayout.EndHorizontal();
    }

    private void RotateGrid90()
    {
        bool[] temp = new bool[64];
        for (int i = 0; i < 64; i++)
        {
            temp[i] = occupiedCellsProp.GetArrayElementAtIndex(i).boolValue;
        }

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                // (col, row) -> (row, 7 - col)
                int oldIdx = row * 8 + col;
                int newIdx = (7 - col) * 8 + row;
                occupiedCellsProp.GetArrayElementAtIndex(newIdx).boolValue = temp[oldIdx];
            }
        }
    }

    private void MirrorGridX()
    {
        bool[] temp = new bool[64];
        for (int i = 0; i < 64; i++)
        {
            temp[i] = occupiedCellsProp.GetArrayElementAtIndex(i).boolValue;
        }

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                int oldIdx = row * 8 + col;
                int newIdx = row * 8 + (7 - col);
                occupiedCellsProp.GetArrayElementAtIndex(newIdx).boolValue = temp[oldIdx];
            }
        }
    }

    private void DrawStatsAndValidation(OpeningScenarioData scenario)
    {
        int occupied = 0;
        for (int i = 0; i < 64; i++)
        {
            if (occupiedCellsProp.GetArrayElementAtIndex(i).boolValue) occupied++;
        }
        int empty = 64 - occupied;
        float percent = (float)occupied / 64f * 100f;

        EditorGUILayout.LabelField("4. Statistics & Validation", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Occupied: {occupied}/64 ({percent:F1}%)  |  Empty: {empty}/64 ({100f - percent:F1}%)", EditorStyles.miniBoldLabel);

        // Check validation rules
        bool hasError = false;
        string errorMessage = "";

        if (occupied < 32 || occupied > 44)
        {
            hasError = true;
            errorMessage += $"• Occupied cell count ({occupied}) is outside the required [32, 44] range (50% - 68.75%).\n";
        }

        for (int r = 0; r < 8; r++)
        {
            int rowCount = 0;
            for (int c = 0; c < 8; c++)
            {
                if (occupiedCellsProp.GetArrayElementAtIndex(r * 8 + c).boolValue) rowCount++;
            }
            if (rowCount == 8)
            {
                hasError = true;
                errorMessage += $"• Row {r} is completely full (initial full rows are not allowed).\n";
            }
        }

        for (int c = 0; c < 8; c++)
        {
            int colCount = 0;
            for (int r = 0; r < 8; r++)
            {
                if (occupiedCellsProp.GetArrayElementAtIndex(r * 8 + c).boolValue) colCount++;
            }
            if (colCount == 8)
            {
                hasError = true;
                errorMessage += $"• Column {c} is completely full (initial full columns are not allowed).\n";
            }
        }

        if (hasError)
        {
            EditorGUILayout.HelpBox(errorMessage.TrimEnd(), MessageType.Error);
        }
        else
        {
            EditorGUILayout.HelpBox("Layout is valid! Occupancy is within [50%, 68.75%] and no lines are pre-completed.", MessageType.Info);
        }
    }
}
