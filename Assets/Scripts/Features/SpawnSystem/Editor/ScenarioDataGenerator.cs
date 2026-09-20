using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ScenarioDataGenerator
{
    private const string TargetDir = "Assets/ScriptableObjects/Scenario";
    private const string ShapesDir = "Assets/ScriptableObjects/Shapes";

    [MenuItem("Block Blast/Generate Default Scenarios")]
    public static void GenerateAllScenarios()
    {
        if (!Directory.Exists(TargetDir))
        {
            Directory.CreateDirectory(TargetDir);
        }

        // Load shapes from Shapes directory
        ShapeData dot1x1 = AssetDatabase.LoadAssetAtPath<ShapeData>($"{ShapesDir}/1_Dot_1x1.asset");
        ShapeData line2 = AssetDatabase.LoadAssetAtPath<ShapeData>($"{ShapesDir}/1_Line_2.asset");
        ShapeData line3 = AssetDatabase.LoadAssetAtPath<ShapeData>($"{ShapesDir}/1_Line_3.asset");
        ShapeData line4 = AssetDatabase.LoadAssetAtPath<ShapeData>($"{ShapesDir}/2_Line_4.asset");
        ShapeData line5 = AssetDatabase.LoadAssetAtPath<ShapeData>($"{ShapesDir}/3_Line_5.asset");
        ShapeData square2x2 = AssetDatabase.LoadAssetAtPath<ShapeData>($"{ShapesDir}/2_Square_2x2.asset");
        ShapeData rect2x3 = AssetDatabase.LoadAssetAtPath<ShapeData>($"{ShapesDir}/3_Rect_2x3.asset");
        ShapeData square3x3 = AssetDatabase.LoadAssetAtPath<ShapeData>($"{ShapesDir}/3_Square_3x3.asset");
        ShapeData shapeL = AssetDatabase.LoadAssetAtPath<ShapeData>($"{ShapesDir}/2_Shape_L.asset");
        ShapeData shapeJ = AssetDatabase.LoadAssetAtPath<ShapeData>($"{ShapesDir}/2_Shape_J.asset");
        ShapeData shapeT = AssetDatabase.LoadAssetAtPath<ShapeData>($"{ShapesDir}/2_Shape_T.asset");
        ShapeData shapeS = AssetDatabase.LoadAssetAtPath<ShapeData>($"{ShapesDir}/2_Shape_S.asset");
        ShapeData shapeZ = AssetDatabase.LoadAssetAtPath<ShapeData>($"{ShapesDir}/2_Shape_Z.asset");
        ShapeData smallV = AssetDatabase.LoadAssetAtPath<ShapeData>($"{ShapesDir}/2_Small_V.asset");
        ShapeData bigV = AssetDatabase.LoadAssetAtPath<ShapeData>($"{ShapesDir}/3_Big_V.asset");

        List<ScenarioData> generatedScenarios = new List<ScenarioData>();

        // ==========================================
        // PART I: 5 CORE SCENARIOS
        // ==========================================

        // 1. Dual Line Blast (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_01_DualLineBlast",
            "Dual Line Blast",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Build 2 bottom rows leaving 2x2 corner open", slot0 = square2x2, slot1 = square2x2, slot2 = square2x2 },
            CreateCoords(
                (0, 0), (1, 0), (2, 0), (3, 0), (4, 0), (5, 0),
                (0, 1), (1, 1), (2, 1), (3, 1), (4, 1), (5, 1)
            ),
            new ScenarioBatch { note = "Finisher: Drop Square 2x2 into the 2x2 corner for 100% ALL CLEAR!", slot0 = square2x2, slot1 = line3, slot2 = smallV }
        ));

        // 2. Crossfire Blast (CrossClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_02_CrossfireBlast",
            "Crossfire Blast",
            ScenarioType.CrossClear,
            new ScenarioBatch { note = "Setup: Prepare Row 4 and Col 4 intersection", slot0 = line4, slot1 = line3, slot2 = line4 },
            CreateCoords(
                (0, 4), (1, 4), (2, 4), (3, 4), (5, 4), (6, 4), (7, 4),
                (4, 0), (4, 1), (4, 2), (4, 3)
            ),
            new ScenarioBatch { note = "Finisher: Drop vertical Line 4 to blast Row 4 and Col 4 for 100% ALL CLEAR!", slot0 = line4, slot1 = square2x2, slot2 = smallV }
        ));

        // 3. Twin Column Crush (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_03_TwinColumnCrush",
            "Twin Column Crush",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Stack three 2x2 squares in columns 0 and 1", slot0 = square2x2, slot1 = square2x2, slot2 = square2x2 },
            CreateCoords(
                (0, 0), (1, 0), (0, 1), (1, 1),
                (0, 2), (1, 2), (0, 3), (1, 3),
                (0, 4), (1, 4), (0, 5), (1, 5)
            ),
            new ScenarioBatch { note = "Finisher: Drop Square 2x2 onto the top for 100% ALL CLEAR!", slot0 = square2x2, slot1 = line3, slot2 = shapeT }
        ));

        // 4. Triple Line Storm (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_04_TripleLineStorm",
            "Triple Line Storm",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: 4x3 block in bottom-left leaving 4-wide gap on right", slot0 = line4, slot1 = square2x2, slot2 = square2x2 },
            CreateCoords(
                (0, 0), (1, 0), (2, 0), (3, 0),
                (0, 1), (1, 1), (2, 1), (3, 1),
                (0, 2), (1, 2), (2, 2), (3, 2)
            ),
            new ScenarioBatch { note = "Finisher: Chained Line 4 blocks for 100% ALL CLEAR triple storm!", slot0 = line4, slot1 = line4, slot2 = line4 }
        ));

        // 5. Corner Twin Row Blast (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_05_CornerTwinRowBlast",
            "Corner Twin Row Blast",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Rect 2x3 and two Square 2x2 in Rows 0-1 leaving 1x2 vertical slot at Col 0", slot0 = rect2x3, slot1 = square2x2, slot2 = square2x2 },
            CreateCoords(
                (1, 0), (2, 0), (3, 0), (4, 0), (5, 0), (6, 0), (7, 0),
                (1, 1), (2, 1), (3, 1), (4, 1), (5, 1), (6, 1), (7, 1)
            ),
            new ScenarioBatch { note = "Finisher: Drop vertical Line 3 into Col 0 for 100% ALL CLEAR!", slot0 = line3, slot1 = square2x2, slot2 = smallV }
        ));

        // ======================================================================
        // PART II: 5 TRIPLE LINE/COLUMN CLEAR SCENARIOS (3x3 & 2x3) - ALL CLEAR
        // ======================================================================

        // 6. Mega 3x3 Triple Row (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_06_Mega3x3TripleRow",
            "Mega 3x3 Triple Row",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Two Rect 2x3 and Line 3 in bottom 3 rows leaving 3x3 crater", slot0 = rect2x3, slot1 = rect2x3, slot2 = line3 },
            CreateCoords(
                (0, 0), (1, 0), (2, 0), (3, 0), (4, 0),
                (0, 1), (1, 1), (2, 1), (3, 1), (4, 1),
                (0, 2), (1, 2), (2, 2), (3, 2), (4, 2)
            ),
            new ScenarioBatch { note = "Finisher: Drop Square 3x3 into the 3x3 crater for 100% ALL CLEAR!", slot0 = square3x3, slot1 = line4, slot2 = square2x2 }
        ));

        // 7. Rect 2x3 Triple Row (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_07_Rect2x3TripleRow",
            "Rect 2x3 Triple Row",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Three vertical Rect 2x3 in bottom 3 rows leaving 2x3 slot", slot0 = rect2x3, slot1 = rect2x3, slot2 = rect2x3 },
            CreateCoords(
                (0, 0), (1, 0), (2, 0), (3, 0), (4, 0), (5, 0),
                (0, 1), (1, 1), (2, 1), (3, 1), (4, 1), (5, 1),
                (0, 2), (1, 2), (2, 2), (3, 2), (4, 2), (5, 2)
            ),
            new ScenarioBatch { note = "Finisher: Drop vertical Rect 2x3 into Cols 6-7 for 100% ALL CLEAR!", slot0 = rect2x3, slot1 = line3, slot2 = square2x2 }
        ));

        // 8. Mega 3x3 Triple Column (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_08_Mega3x3TripleColumn",
            "Mega 3x3 Triple Column",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Three vertical Line 5 in columns 0, 1, 2 leaving 3x3 slot at top", slot0 = line5, slot1 = line5, slot2 = line5 },
            CreateCoords(
                (0, 0), (1, 0), (2, 0),
                (0, 1), (1, 1), (2, 1),
                (0, 2), (1, 2), (2, 2),
                (0, 3), (1, 3), (2, 3),
                (0, 4), (1, 4), (2, 4)
            ),
            new ScenarioBatch { note = "Finisher: Drop Square 3x3 on top of columns for 100% ALL CLEAR!", slot0 = square3x3, slot1 = line4, slot2 = smallV }
        ));

        // 9. Center 3x3 Crater Blast (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_09_Center3x3CraterBlast",
            "Center 3x3 Crater Blast",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Two vertical Rect 2x3 and Line 3 in middle rows leaving 3x3 center crater", slot0 = rect2x3, slot1 = rect2x3, slot2 = line3 },
            CreateCoords(
                (0, 3), (1, 3), (2, 3), (6, 3), (7, 3),
                (0, 4), (1, 4), (2, 4), (6, 4), (7, 4),
                (0, 5), (1, 5), (2, 5), (6, 5), (7, 5)
            ),
            new ScenarioBatch { note = "Finisher: Drop Square 3x3 into center crater for 100% ALL CLEAR!", slot0 = square3x3, slot1 = square2x2, slot2 = line3 }
        ));

        // 10. Rect 2x3 Triple Column (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_10_Rect2x3TripleColumn",
            "Rect 2x3 Triple Column",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Three horizontal 3x2 Rect blocks in columns 0..2 leaving top slot", slot0 = rect2x3, slot1 = rect2x3, slot2 = rect2x3 },
            CreateCoords(
                (0, 0), (1, 0), (2, 0),
                (0, 1), (1, 1), (2, 1),
                (0, 2), (1, 2), (2, 2),
                (0, 3), (1, 3), (2, 3),
                (0, 4), (1, 4), (2, 4),
                (0, 5), (1, 5), (2, 5)
            ),
            new ScenarioBatch { note = "Finisher: Drop horizontal 3x2 Rect block onto top for 100% ALL CLEAR!", slot0 = rect2x3, slot1 = line4, slot2 = smallV }
        ));

        // ======================================================================
        // PART III: 5 DOUBLE LINE/COLUMN CLEAR SCENARIOS (2x3) - ALL CLEAR
        // ======================================================================

        // 11. Rect 3x2 Dual Row Blast (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_11_Rect3x2DualRowBlast",
            "Rect 3x2 Dual Row Blast",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Square 2x2 and two Line 3 in Rows 0-1 leaving 3x2 gap", slot0 = square2x2, slot1 = line3, slot2 = line3 },
            CreateCoords(
                (0, 0), (1, 0), (2, 0), (3, 0), (4, 0),
                (0, 1), (1, 1), (2, 1), (3, 1), (4, 1)
            ),
            new ScenarioBatch { note = "Finisher: Drop horizontal 3x2 Rect into Rows 0-1 for 100% ALL CLEAR!", slot0 = rect2x3, slot1 = line3, slot2 = square2x2 }
        ));

        // 12. Rect 2x3 Dual Column Crush (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_12_Rect2x3DualColumnCrush",
            "Rect 2x3 Dual Column Crush",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Square 2x2 and two Line 3 in Cols 0-1 leaving 2x3 gap", slot0 = square2x2, slot1 = line3, slot2 = line3 },
            CreateCoords(
                (0, 0), (1, 0),
                (0, 1), (1, 1),
                (0, 2), (1, 2),
                (0, 3), (1, 3),
                (0, 4), (1, 4)
            ),
            new ScenarioBatch { note = "Finisher: Drop vertical 2x3 Rect onto top of columns for 100% ALL CLEAR!", slot0 = rect2x3, slot1 = line4, slot2 = smallV }
        ));

        // 13. Center Dual Row Rect Blast (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_13_CenterDualRowRectBlast",
            "Center Dual Row Rect Blast",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Square 2x2 and two Line 3 in Rows 3-4 leaving 3x2 gap", slot0 = square2x2, slot1 = line3, slot2 = line3 },
            CreateCoords(
                (0, 3), (1, 3), (2, 3), (3, 3), (4, 3),
                (0, 4), (1, 4), (2, 4), (3, 4), (4, 4)
            ),
            new ScenarioBatch { note = "Finisher: Drop horizontal 3x2 Rect into center Rows 3-4 for 100% ALL CLEAR!", slot0 = rect2x3, slot1 = line3, slot2 = square2x2 }
        ));

        // 14. Center Dual Column Crush (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_14_CenterDualColumnCrush",
            "Center Dual Column Crush",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Square 2x2 and two Line 3 in Cols 3-4 leaving 2x3 gap", slot0 = square2x2, slot1 = line3, slot2 = line3 },
            CreateCoords(
                (3, 0), (4, 0),
                (3, 1), (4, 1),
                (3, 2), (4, 2),
                (3, 3), (4, 3),
                (3, 4), (4, 4)
            ),
            new ScenarioBatch { note = "Finisher: Drop vertical 2x3 Rect onto middle columns for 100% ALL CLEAR!", slot0 = rect2x3, slot1 = square2x2, slot2 = line4 }
        ));

        // 15. Top Dual Row Rect Blast (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_15_TopDualRowRectBlast",
            "Top Dual Row Rect Blast",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Square 2x2 and two Line 3 in Rows 6-7 leaving 3x2 gap", slot0 = square2x2, slot1 = line3, slot2 = line3 },
            CreateCoords(
                (0, 6), (1, 6), (2, 6), (3, 6), (4, 6),
                (0, 7), (1, 7), (2, 7), (3, 7), (4, 7)
            ),
            new ScenarioBatch { note = "Finisher: Drop horizontal 3x2 Rect into top ceiling Rows 6-7 for 100% ALL CLEAR!", slot0 = rect2x3, slot1 = line4, slot2 = line3 }
        ));

        // ======================================================================
        // PART IV: 5 DUAL LINE/COLUMN CLEAR SCENARIOS (TETRIS L, J, T, S, Z) - ALL CLEAR
        // ======================================================================

        // 16. Shape L Dual Row Blast (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_16_ShapeL_DualRowBlast",
            "Shape L Dual Row Blast",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Two Square 2x2 and Shape J in Rows 0-1 leaving Shape L slot", slot0 = square2x2, slot1 = square2x2, slot2 = shapeJ },
            CreateCoords(
                (0, 0), (1, 0), (2, 0), (3, 0), (4, 0),
                (0, 1), (1, 1), (2, 1), (3, 1), (4, 1), (5, 1), (6, 1)
            ),
            new ScenarioBatch { note = "Finisher: Drop Shape L into corner for 100% ALL CLEAR!", slot0 = shapeL, slot1 = line3, slot2 = square2x2 }
        ));

        // 17. Shape J Dual Row Blast (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_17_ShapeJ_DualRowBlast",
            "Shape J Dual Row Blast",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Two Square 2x2 and Shape L in Rows 0-1 leaving Shape J slot", slot0 = square2x2, slot1 = square2x2, slot2 = shapeL },
            CreateCoords(
                (3, 0), (4, 0), (5, 0), (6, 0), (7, 0),
                (1, 1), (2, 1), (3, 1), (4, 1), (5, 1), (6, 1), (7, 1)
            ),
            new ScenarioBatch { note = "Finisher: Drop Shape J into left corner for 100% ALL CLEAR!", slot0 = shapeJ, slot1 = square2x2, slot2 = line3 }
        ));

        // 18. Shape T Dual Row Blast (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_18_ShapeT_DualRowBlast",
            "Shape T Dual Row Blast",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Line 4, Line 5 and Small V leaving Shape T slot", slot0 = line4, slot1 = line5, slot2 = smallV },
            CreateCoords(
                (0, 0), (1, 0), (2, 0), (3, 0), (7, 0),
                (0, 1), (1, 1), (2, 1), (3, 1), (4, 1), (6, 1), (7, 1)
            ),
            new ScenarioBatch { note = "Finisher: Drop Shape T into slot for 100% ALL CLEAR!", slot0 = shapeT, slot1 = line3, slot2 = square2x2 }
        ));

        // 19. Shape S Dual Row Blast (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_19_ShapeS_DualRowBlast",
            "Shape S Dual Row Blast",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Line 4, Line 5 and Small V leaving Shape S slot", slot0 = line4, slot1 = line5, slot2 = smallV },
            CreateCoords(
                (0, 0), (1, 0), (2, 0), (3, 0), (6, 0), (7, 0),
                (0, 1), (1, 1), (2, 1), (3, 1), (4, 1), (7, 1)
            ),
            new ScenarioBatch { note = "Finisher: Drop Shape S into slot for 100% ALL CLEAR!", slot0 = shapeS, slot1 = line4, slot2 = line3 }
        ));

        // 20. Shape Z Dual Row Blast (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_20_ShapeZ_DualRowBlast",
            "Shape Z Dual Row Blast",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Line 5, Line 4 and Small V leaving Shape Z slot", slot0 = line5, slot1 = line4, slot2 = smallV },
            CreateCoords(
                (0, 0), (1, 0), (2, 0), (3, 0), (4, 0), (7, 0),
                (0, 1), (1, 1), (2, 1), (3, 1), (6, 1), (7, 1)
            ),
            new ScenarioBatch { note = "Finisher: Drop Shape Z into slot for 100% ALL CLEAR!", slot0 = shapeZ, slot1 = line4, slot2 = line3 }
        ));

        // ======================================================================
        // PART V: 5 SCENARIOS USING BIG V & SMALL V (21 -> 25) - ALL CLEAR
        // ======================================================================

        // 21. Small V Dual Row Right (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_21_SmallV_DualRowRight",
            "Small V Dual Row Right",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Two Rect 2x3 and Dot 1x1 in Rows 0-1 leaving Small V gap at bottom-right corner", slot0 = rect2x3, slot1 = rect2x3, slot2 = dot1x1 },
            CreateCoords(
                (0, 0), (1, 0), (2, 0), (3, 0), (4, 0), (5, 0),
                (0, 1), (1, 1), (2, 1), (3, 1), (4, 1), (5, 1), (6, 1)
            ),
            new ScenarioBatch { note = "Finisher: Drop Small V into bottom-right corner for 100% ALL CLEAR!", slot0 = smallV, slot1 = line3, slot2 = square2x2 }
        ));

        // 22. Small V Dual Row Left (MultiLineClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_22_SmallV_DualRowLeft",
            "Small V Dual Row Left",
            ScenarioType.MultiLineClear,
            new ScenarioBatch { note = "Setup: Two Rect 2x3 and Dot 1x1 in Rows 0-1 leaving Small V gap at bottom-left corner", slot0 = rect2x3, slot1 = rect2x3, slot2 = dot1x1 },
            CreateCoords(
                (2, 0), (3, 0), (4, 0), (5, 0), (6, 0), (7, 0),
                (1, 1), (2, 1), (3, 1), (4, 1), (5, 1), (6, 1), (7, 1)
            ),
            new ScenarioBatch { note = "Finisher: Drop Small V into bottom-left corner for 100% ALL CLEAR!", slot0 = smallV, slot1 = square2x2, slot2 = line3 }
        ));

        // 23. Big V Corner Cross Clear (CrossClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_23_BigV_CornerCrossClear",
            "Big V Corner Cross Clear",
            ScenarioType.CrossClear,
            new ScenarioBatch { note = "Setup: Line 4, Dot 1x1 on Row 0 and Line 5 on Col 0 leaving 3x3 corner for Big V", slot0 = line4, slot1 = dot1x1, slot2 = line5 },
            CreateCoords(
                (3, 0), (4, 0), (5, 0), (6, 0), (7, 0),
                (0, 3), (0, 4), (0, 5), (0, 6), (0, 7)
            ),
            new ScenarioBatch { note = "Finisher: Drop Big V into bottom-left corner to clear Row 0 and Col 0 for 100% ALL CLEAR!", slot0 = bigV, slot1 = square2x2, slot2 = line3 }
        ));

        // 24. Big V Top Corner Cross (CrossClear) - 100% All Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_24_BigV_TopCornerCross",
            "Big V Top Corner Cross",
            ScenarioType.CrossClear,
            new ScenarioBatch { note = "Setup: Line 4, Dot 1x1 on Row 7 and Line 5 on Col 7 leaving 3x3 top corner for Big V", slot0 = line4, slot1 = dot1x1, slot2 = line5 },
            CreateCoords(
                (0, 7), (1, 7), (2, 7), (3, 7), (4, 7),
                (7, 0), (7, 1), (7, 2), (7, 3), (7, 4)
            ),
            new ScenarioBatch { note = "Finisher: Drop Big V into top-right corner to clear Row 7 and Col 7 for 100% ALL CLEAR!", slot0 = bigV, slot1 = line4, slot2 = square2x2 }
        ));

        // ======================================================================
        // PART VI: 9 MULTI-LINE SUPER CROSS CLEAR SCENARIOS (25 -> 33) - 100% ALL CLEAR
        // ======================================================================

        // 25. Cross 2x2 Small V (CrossClear) - 4 Lines Simultaneous Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_25_Cross_2x2_SmallV",
            "Cross 2x2 Small V",
            ScenarioType.CrossClear,
            new ScenarioBatch { note = "Setup: Two horizontal Rect 2x3 and one vertical Rect 2x3 in bottom-left", slot0 = rect2x3, slot1 = rect2x3, slot2 = rect2x3 },
            CreateCoords(
                (2, 0), (3, 0), (4, 0), (5, 0), (6, 0), (7, 0),
                (2, 1), (3, 1), (4, 1), (5, 1), (6, 1), (7, 1),
                (0, 2), (1, 2), (0, 3), (1, 3), (0, 4), (1, 4)
            ),
            new ScenarioBatch { note = "Finisher: Place Rect 2x3 in upper column, then drop Square 2x2 into bottom-left corner for 4-line blast & 100% ALL CLEAR!", slot0 = rect2x3, slot1 = square2x2, slot2 = smallV }
        ));

        // 26. Cross 2x2 Center (CrossClear) - 4 Lines Simultaneous Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_26_Cross_2x2_Center",
            "Cross 2x2 Center",
            ScenarioType.CrossClear,
            new ScenarioBatch { note = "Setup: Build left, right, and bottom arms of 2x2 center cross using three Rect 2x3", slot0 = rect2x3, slot1 = rect2x3, slot2 = rect2x3 },
            CreateCoords(
                (0, 3), (1, 3), (2, 3), (0, 4), (1, 4), (2, 4),
                (5, 3), (6, 3), (7, 3), (5, 4), (6, 4), (7, 4),
                (3, 0), (4, 0), (3, 1), (4, 1), (3, 2), (4, 2)
            ),
            new ScenarioBatch { note = "Finisher: Place Rect 2x3 in top arm, then drop Square 2x2 into center core to blast 4 lines simultaneously for 100% ALL CLEAR!", slot0 = rect2x3, slot1 = square2x2, slot2 = line3 }
        ));

        // 27. Cross 2x2 Bottom Left (CrossClear) - 4 Lines Simultaneous Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_27_Cross_2x2_BottomLeft",
            "Cross 2x2 Bottom Left",
            ScenarioType.CrossClear,
            new ScenarioBatch { note = "Setup: Line 5 in Rows 1 and 2, Line 5 in Col 1", slot0 = line5, slot1 = line5, slot2 = line5 },
            CreateCoords(
                (3, 1), (4, 1), (5, 1), (6, 1), (7, 1),
                (3, 2), (4, 2), (5, 2), (6, 2), (7, 2),
                (1, 3), (1, 4), (1, 5), (1, 6), (1, 7)
            ),
            new ScenarioBatch { note = "Finisher: Complete Col 2 with Line 5, then drop Square 2x2 into intersection for 4-line blast & 100% ALL CLEAR!", slot0 = line5, slot1 = square2x2, slot2 = line4 }
        ));

        // 28. Cross 2x2 Top Right (CrossClear) - 4 Lines Simultaneous Clear
        generatedScenarios.Add(CreateScenario(
            "Scenario_28_Cross_2x2_TopRight",
            "Cross 2x2 Top Right",
            ScenarioType.CrossClear,
            new ScenarioBatch { note = "Setup: Line 5 in Rows 5 and 6, Line 5 in Col 5", slot0 = line5, slot1 = line5, slot2 = line5 },
            CreateCoords(
                (0, 5), (1, 5), (2, 5), (3, 5), (4, 5),
                (0, 6), (1, 6), (2, 6), (3, 6), (4, 6),
                (5, 0), (5, 1), (5, 2), (5, 3), (5, 4)
            ),
            new ScenarioBatch { note = "Finisher: Complete Col 6 with Line 5, then drop Square 2x2 into intersection for 4-line blast & 100% ALL CLEAR!", slot0 = line5, slot1 = square2x2, slot2 = line4 }
        ));

        // 29. Cross 2x3 Center (CrossClear) - 5 Lines Simultaneous Clear (2 Rows x 3 Cols)
        generatedScenarios.Add(CreateScenario(
            "Scenario_29_Cross_2x3_Center",
            "Cross 2x3 Center",
            ScenarioType.CrossClear,
            new ScenarioBatch { note = "Setup: Square 3x3 in bottom column, Rect 2x3 in right wing, Square 2x2 in left wing", slot0 = square3x3, slot1 = rect2x3, slot2 = square2x2 },
            CreateCoords(
                (0, 3), (1, 3), (0, 4), (1, 4),
                (5, 3), (6, 3), (7, 3), (5, 4), (6, 4), (7, 4),
                (2, 0), (3, 0), (4, 0),
                (2, 1), (3, 1), (4, 1),
                (2, 2), (3, 2), (4, 2)
            ),
            new ScenarioBatch { note = "Finisher: Place Square 3x3 in top arm, then drop Rect 2x3 into center to blast 2 rows and 3 columns (5 lines) for 100% ALL CLEAR!", slot0 = square3x3, slot1 = rect2x3, slot2 = line3 }
        ));

        // 30. Cross 2x3 Bottom (CrossClear) - 5 Lines Simultaneous Clear (2 Rows x 3 Cols)
        generatedScenarios.Add(CreateScenario(
            "Scenario_30_Cross_2x3_Bottom",
            "Cross 2x3 Bottom",
            ScenarioType.CrossClear,
            new ScenarioBatch { note = "Setup: Square 3x3 in vertical body, Rect 2x3 in right wing, Square 2x2 in left wing", slot0 = square3x3, slot1 = rect2x3, slot2 = square2x2 },
            CreateCoords(
                (0, 0), (1, 0), (0, 1), (1, 1),
                (5, 0), (6, 0), (7, 0), (5, 1), (6, 1), (7, 1),
                (2, 2), (3, 2), (4, 2),
                (2, 3), (3, 3), (4, 3),
                (2, 4), (3, 4), (4, 4)
            ),
            new ScenarioBatch { note = "Finisher: Place Square 3x3 at top, then drop Rect 2x3 into bottom intersection to blast 5 lines simultaneously for 100% ALL CLEAR!", slot0 = square3x3, slot1 = rect2x3, slot2 = line3 }
        ));

        // 31. Cross 3x2 Center (CrossClear) - 5 Lines Simultaneous Clear (3 Rows x 2 Cols)
        generatedScenarios.Add(CreateScenario(
            "Scenario_31_Cross_3x2_Center",
            "Cross 3x2 Center",
            ScenarioType.CrossClear,
            new ScenarioBatch { note = "Setup: Square 3x3 in left wing, vertical Rect 2x3 in top arm, Square 2x2 in bottom arm", slot0 = square3x3, slot1 = rect2x3, slot2 = square2x2 },
            CreateCoords(
                (0, 2), (1, 2), (2, 2),
                (0, 3), (1, 3), (2, 3),
                (0, 4), (1, 4), (2, 4),
                (3, 5), (4, 5),
                (3, 6), (4, 6),
                (3, 7), (4, 7),
                (3, 0), (4, 0),
                (3, 1), (4, 1)
            ),
            new ScenarioBatch { note = "Finisher: Place Square 3x3 in right wing, then drop vertical Rect 2x3 into center to blast 3 rows and 2 columns (5 lines) for 100% ALL CLEAR!", slot0 = square3x3, slot1 = rect2x3, slot2 = line4 }
        ));

        // 32. Cross 3x2 Left (CrossClear) - 5 Lines Simultaneous Clear (3 Rows x 2 Cols)
        generatedScenarios.Add(CreateScenario(
            "Scenario_32_Cross_3x2_Left",
            "Cross 3x2 Left",
            ScenarioType.CrossClear,
            new ScenarioBatch { note = "Setup: Square 3x3 in middle wing, vertical Rect 2x3 in top arm, Square 2x2 in bottom arm", slot0 = square3x3, slot1 = rect2x3, slot2 = square2x2 },
            CreateCoords(
                (2, 2), (3, 2), (4, 2),
                (2, 3), (3, 3), (4, 3),
                (2, 4), (3, 4), (4, 4),
                (0, 5), (1, 5),
                (0, 6), (1, 6),
                (0, 7), (1, 7),
                (0, 0), (1, 0),
                (0, 1), (1, 1)
            ),
            new ScenarioBatch { note = "Finisher: Place Square 3x3 in far right wing, then drop vertical Rect 2x3 into left intersection to blast 5 lines simultaneously for 100% ALL CLEAR!", slot0 = square3x3, slot1 = rect2x3, slot2 = line4 }
        ));

        // 33. Mega Cross 3x3 Center (CrossClear) - 6 Lines Mega Blast (3 Rows x 3 Cols)
        generatedScenarios.Add(CreateScenario(
            "Scenario_33_MegaCross_3x3_Center",
            "Mega Cross 3x3 Center",
            ScenarioType.CrossClear,
            new ScenarioBatch { note = "Setup: Square 3x3 in right wing, Square 3x3 in top arm, vertical Rect 2x3 in left wing", slot0 = square3x3, slot1 = square3x3, slot2 = rect2x3 },
            CreateCoords(
                (5, 2), (6, 2), (7, 2),
                (5, 3), (6, 3), (7, 3),
                (5, 4), (6, 4), (7, 4),
                (2, 5), (3, 5), (4, 5),
                (2, 6), (3, 6), (4, 6),
                (2, 7), (3, 7), (4, 7),
                (0, 2), (1, 2),
                (0, 3), (1, 3),
                (0, 4), (1, 4)
            ),
            new ScenarioBatch { note = "Finisher: Place Rect 2x3 at bottom, then drop Square 3x3 into the center to unleash the 6-LINE MEGA CROSS BLAST for 100% ALL CLEAR!", slot0 = rect2x3, slot1 = square3x3, slot2 = line5 }
        ));

        // Create or update ScenarioDatabase asset
        ScenarioDatabase db = ScriptableObject.CreateInstance<ScenarioDatabase>();
        db.scenarios = generatedScenarios;
        AssetDatabase.CreateAsset(db, $"{TargetDir}/ScenarioDatabase.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Successfully created {generatedScenarios.Count} 100% ALL CLEAR scenarios and ScenarioDatabase at {TargetDir}!");
    }

    private static ScenarioData CreateScenario(
        string fileName,
        string scenarioName,
        ScenarioType type,
        ScenarioBatch setupBatch,
        IEnumerable<Vector2Int> targetCoords,
        ScenarioBatch finisherBatch)
    {
        ScenarioData scenario = ScriptableObject.CreateInstance<ScenarioData>();
        scenario.scenarioName = scenarioName;
        scenario.scenarioType = type;
        scenario.allowRotation = true;
        scenario.allowMirror = true;
        scenario.matchTolerance = 1;
        scenario.setupBatch = setupBatch;
        scenario.finisherBatch = finisherBatch;
        scenario.targetBoard = new bool[64];

        if (targetCoords != null)
        {
            foreach (var coord in targetCoords)
            {
                if (coord.x >= 0 && coord.x < 8 && coord.y >= 0 && coord.y < 8)
                {
                    int index = coord.y * 8 + coord.x;
                    scenario.targetBoard[index] = true;
                }
            }
        }

        scenario.SyncTargetCoordinates();

        string assetPath = $"{TargetDir}/{fileName}.asset";
        AssetDatabase.CreateAsset(scenario, assetPath);
        return scenario;
    }

    private static List<Vector2Int> CreateCoords(params (int col, int row)[] points)
    {
        List<Vector2Int> list = new List<Vector2Int>();
        if (points != null)
        {
            foreach (var (col, row) in points)
            {
                list.Add(new Vector2Int(col, row));
            }
        }
        return list;
    }
}

