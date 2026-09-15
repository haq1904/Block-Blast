using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScenarioData))]
public class ScenarioDataEditor : Editor
{
    private enum DrawMode
    {
        ToggleCell = 0,
        StampSlot0 = 1,
        StampSlot1 = 2,
        StampSlot2 = 3
    }

    private static readonly Color OccupiedColor = new Color(1f, 0.55f, 0.15f); // Vibrant orange
    private static readonly Color EmptyColor = new Color(0.26f, 0.26f, 0.30f);    // Dark slate
    private static readonly Color ActiveToolColor = new Color(0.3f, 0.75f, 1f);  // Cyan

    private DrawMode currentDrawMode = DrawMode.ToggleCell;
    private int stampRotation = 0; // 0, 90, 180, 270

    public override void OnInspectorGUI()
    {
        ScenarioData scenario = (ScenarioData)target;
        Undo.RecordObject(scenario, "Scenario Modification");

        // 1. Basic Information
        EditorGUILayout.LabelField("1. Scenario Information", EditorStyles.boldLabel);
        scenario.scenarioName = EditorGUILayout.TextField("Scenario Name", scenario.scenarioName);
        scenario.scenarioType = (ScenarioType)EditorGUILayout.EnumPopup("Scenario Type", scenario.scenarioType);

        EditorGUILayout.Space(6);
        scenario.allowRotation = EditorGUILayout.Toggle(new GUIContent("Allow Rotation (90°/180°/270°)", "Auto-generates rotated variations in gameplay"), scenario.allowRotation);
        scenario.allowMirror = EditorGUILayout.Toggle(new GUIContent("Allow Mirroring (Flip X)", "Auto-generates mirrored variations in gameplay"), scenario.allowMirror);

        EditorGUILayout.Space(12);

        // 2. Phase 1: Setup Batch
        EditorGUILayout.LabelField("2. Phase 1: Setup Batch (Build the Board)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("3 shapes given to the player in phase 1. The player should place these to construct the Target Board below.", MessageType.None);

        if (scenario.setupBatch == null) scenario.setupBatch = new ScenarioBatch();
        DrawBatchFields(scenario.setupBatch, "Setup");

        int setupTiles = scenario.GetSetupTotalTiles();
        EditorGUILayout.LabelField($"Total Tiles in Setup Batch: {setupTiles} tiles", EditorStyles.miniBoldLabel);

        EditorGUILayout.Space(14);

        // 3. Phase 2: Target Board Layout
        EditorGUILayout.LabelField("3. Target Board Layout (Expected 8x8 Grid after Setup)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Draw the 8x8 board state expected after the player places the Setup Batch.", MessageType.None);

        scenario.matchTolerance = EditorGUILayout.IntSlider(new GUIContent("Match Tolerance", "Allowed number of missing/extra cells"), scenario.matchTolerance, 0, 3);

        EditorGUILayout.Space(4);

        // Stamp Mode Toolbar
        DrawStampToolbar(scenario);

        EditorGUILayout.Space(6);

        // 8x8 Grid
        DrawGrid(scenario);

        EditorGUILayout.Space(8);

        // Grid Action Buttons
        DrawGridActions(scenario);

        EditorGUILayout.Space(8);

        // Validation section
        DrawValidationSection(scenario, setupTiles);

        EditorGUILayout.Space(14);

        // 4. Phase 3: Finisher Batch
        EditorGUILayout.LabelField("4. Phase 2: Finisher Batch (Trigger Line Clear)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("3 shapes given to the player after the board matches the Target Board to trigger the clear.", MessageType.None);

        if (scenario.finisherBatch == null) scenario.finisherBatch = new ScenarioBatch();
        DrawBatchFields(scenario.finisherBatch, "Finisher");

        if (GUI.changed)
        {
            scenario.SyncTargetCoordinates();
            EditorUtility.SetDirty(scenario);
        }
    }

    private void DrawBatchFields(ScenarioBatch batch, string labelPrefix)
    {
        GUILayout.BeginVertical("box");
        batch.note = EditorGUILayout.TextField($"{labelPrefix} Note", batch.note);
        batch.slot0 = (ShapeData)EditorGUILayout.ObjectField("Tray Slot 0", batch.slot0, typeof(ShapeData), false);
        batch.slot1 = (ShapeData)EditorGUILayout.ObjectField("Tray Slot 1", batch.slot1, typeof(ShapeData), false);
        batch.slot2 = (ShapeData)EditorGUILayout.ObjectField("Tray Slot 2", batch.slot2, typeof(ShapeData), false);
        GUILayout.EndVertical();
    }

    private void DrawStampToolbar(ScenarioData scenario)
    {
        GUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Drawing & Stamp Tool", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();

        // Toggle cell mode button
        Color defaultBg = GUI.backgroundColor;
        if (currentDrawMode == DrawMode.ToggleCell) GUI.backgroundColor = ActiveToolColor;
        if (GUILayout.Button("Single Cell", GUILayout.Height(24)))
        {
            currentDrawMode = DrawMode.ToggleCell;
        }
        GUI.backgroundColor = defaultBg;

        // Stamp Slot 0
        if (currentDrawMode == DrawMode.StampSlot0) GUI.backgroundColor = ActiveToolColor;
        GUI.enabled = scenario.setupBatch?.slot0 != null;
        if (GUILayout.Button("Stamp Slot 0", GUILayout.Height(24)))
        {
            currentDrawMode = DrawMode.StampSlot0;
        }
        GUI.backgroundColor = defaultBg;

        // Stamp Slot 1
        if (currentDrawMode == DrawMode.StampSlot1) GUI.backgroundColor = ActiveToolColor;
        GUI.enabled = scenario.setupBatch?.slot1 != null;
        if (GUILayout.Button("Stamp Slot 1", GUILayout.Height(24)))
        {
            currentDrawMode = DrawMode.StampSlot1;
        }
        GUI.backgroundColor = defaultBg;

        // Stamp Slot 2
        if (currentDrawMode == DrawMode.StampSlot2) GUI.backgroundColor = ActiveToolColor;
        GUI.enabled = scenario.setupBatch?.slot2 != null;
        if (GUILayout.Button("Stamp Slot 2", GUILayout.Height(24)))
        {
            currentDrawMode = DrawMode.StampSlot2;
        }
        GUI.backgroundColor = defaultBg;
        GUI.enabled = true;

        GUILayout.EndHorizontal();

        if (currentDrawMode != DrawMode.ToggleCell)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Active Stamp: {currentDrawMode} (Rotation: {stampRotation}°)", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("Rotate Stamp 90°", GUILayout.Width(130), GUILayout.Height(20)))
            {
                stampRotation = (stampRotation + 90) % 360;
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
    }

    private void DrawGrid(ScenarioData scenario)
    {
        if (scenario.targetBoard == null || scenario.targetBoard.Length != 64)
        {
            scenario.targetBoard = new bool[64];
        }

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

        // 8 rows top-down (Row 7 down to Row 0)
        for (int row = 7; row >= 0; row--)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label($"R{row}", EditorStyles.miniLabel, GUILayout.Width(26), GUILayout.Height(34));

            for (int col = 0; col < 8; col++)
            {
                int index = row * 8 + col;
                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = scenario.targetBoard[index] ? OccupiedColor : EmptyColor;

                if (GUILayout.Button("", GUILayout.Width(34), GUILayout.Height(34)))
                {
                    HandleCellClick(scenario, col, row, index);
                }

                GUI.backgroundColor = oldColor;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
    }

    private void HandleCellClick(ScenarioData scenario, int col, int row, int index)
    {
        if (currentDrawMode == DrawMode.ToggleCell)
        {
            scenario.targetBoard[index] = !scenario.targetBoard[index];
        }
        else
        {
            ShapeData shapeToStamp = null;
            if (currentDrawMode == DrawMode.StampSlot0) shapeToStamp = scenario.setupBatch?.slot0;
            else if (currentDrawMode == DrawMode.StampSlot1) shapeToStamp = scenario.setupBatch?.slot1;
            else if (currentDrawMode == DrawMode.StampSlot2) shapeToStamp = scenario.setupBatch?.slot2;

            if (shapeToStamp != null && shapeToStamp.baseOffsets != null)
            {
                List<Vector2Int> offsets = shapeToStamp.canRotate
                    ? shapeToStamp.GetRotatedOffsets(stampRotation)
                    : shapeToStamp.baseOffsets;

                // Stamp all offsets relative to clicked cell
                foreach (var off in offsets)
                {
                    int targetCol = col + off.x;
                    int targetRow = row + off.y;

                    if (targetCol >= 0 && targetCol < 8 && targetRow >= 0 && targetRow < 8)
                    {
                        scenario.targetBoard[targetRow * 8 + targetCol] = true;
                    }
                }
            }
        }

        scenario.SyncTargetCoordinates();
        EditorUtility.SetDirty(scenario);
    }

    private void DrawGridActions(ScenarioData scenario)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Clear All", GUILayout.Width(90), GUILayout.Height(24)))
        {
            scenario.targetBoard = new bool[64];
            scenario.SyncTargetCoordinates();
            EditorUtility.SetDirty(scenario);
        }

        GUILayout.Space(6);

        if (GUILayout.Button("Invert Grid", GUILayout.Width(90), GUILayout.Height(24)))
        {
            for (int i = 0; i < 64; i++) scenario.targetBoard[i] = !scenario.targetBoard[i];
            scenario.SyncTargetCoordinates();
            EditorUtility.SetDirty(scenario);
        }

        GUILayout.Space(6);

        // Preview rotation in editor
        if (GUILayout.Button("Test Rotate 90°", GUILayout.Width(120), GUILayout.Height(24)))
        {
            scenario.targetBoard = ScenarioData.TransformGrid(scenario.targetBoard, 90, false);
            scenario.SyncTargetCoordinates();
            EditorUtility.SetDirty(scenario);
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private void DrawValidationSection(ScenarioData scenario, int setupTiles)
    {
        int targetOccupied = scenario.GetTargetOccupiedCount();
        float occupancyPercentage = (targetOccupied / 64f) * 100f;

        EditorGUILayout.LabelField("Board Statistics & Sanity Check", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"• Target Board Occupied Cells: {targetOccupied} / 64 ({occupancyPercentage:F1}%)");
        EditorGUILayout.LabelField($"• Setup Batch Total Tiles: {setupTiles} tiles");

        if (setupTiles > 0)
        {
            if (setupTiles == targetOccupied)
            {
                EditorGUILayout.HelpBox($"✅ Perfectly Balanced! Target Board has exactly {targetOccupied} cells, matching {setupTiles} tiles from the Setup Batch.", MessageType.Info);
            }
            else
            {
                int diff = Mathf.Abs(setupTiles - targetOccupied);
                string moreOrLess = targetOccupied > setupTiles ? "more" : "fewer";
                EditorGUILayout.HelpBox($"⚠️ Tile Count Mismatch: Target Board has {diff} {moreOrLess} cell(s) than the Setup Batch. Please review placements.", MessageType.Warning);
            }
        }
    }
}
