using UnityEditor;
using UnityEngine;
using System.IO;

public static class ShapeDataGenerator
{
    [MenuItem("Block Blast/Generate Default Shapes")]
    public static void GenerateShapes()
    {
        string dir = "Assets/Resources/Shapes";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        ShapeDatabase db = ScriptableObject.CreateInstance<ShapeDatabase>();
        
        // Tier 1 (Dễ)
        db.tier1Shapes.Add(CreateShape("1_Dot_1x1", 1, false, new[] {12}));
        db.tier1Shapes.Add(CreateShape("1_Line_2", 1, true, new[] {7, 12}));
        db.tier1Shapes.Add(CreateShape("1_Line_3", 1, true, new[] {7, 12, 17}));

        // Tier 2 (Trung Bình)
        db.tier2Shapes.Add(CreateShape("2_Square_2x2", 2, false, new[] {7, 8, 12, 13}));
        db.tier2Shapes.Add(CreateShape("2_Line_4", 2, true, new[] {2, 7, 12, 17}));
        db.tier2Shapes.Add(CreateShape("2_Shape_L", 2, true, new[] {7, 12, 17, 18}));
        db.tier2Shapes.Add(CreateShape("2_Shape_J", 2, true, new[] {7, 12, 17, 16}));
        db.tier2Shapes.Add(CreateShape("2_Shape_T", 2, true, new[] {7, 11, 12, 13}));
        db.tier2Shapes.Add(CreateShape("2_Shape_S", 2, true, new[] {7, 8, 11, 12}));
        db.tier2Shapes.Add(CreateShape("2_Shape_Z", 2, true, new[] {6, 7, 12, 13}));
        db.tier2Shapes.Add(CreateShape("2_Small_V", 2, true, new[] {7, 12, 13}));

        // Tier 3 (Khó)
        db.tier3Shapes.Add(CreateShape("3_Square_3x3", 3, false, new[] {6, 7, 8, 11, 12, 13, 16, 17, 18}));
        db.tier3Shapes.Add(CreateShape("3_Line_5", 3, true, new[] {2, 7, 12, 17, 22}));
        db.tier3Shapes.Add(CreateShape("3_Big_V", 3, true, new[] {7, 12, 17, 18, 19}));

        AssetDatabase.CreateAsset(db, $"{dir}/MasterShapeDatabase.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("🎉 Đã tạo thành công 14 khối gạch gốc và MasterShapeDatabase!");
    }

    private static ShapeData CreateShape(string name, int tier, bool canRotate, int[] solidIndices)
    {
        ShapeData shape = ScriptableObject.CreateInstance<ShapeData>();
        shape.tier = tier;
        shape.canRotate = canRotate;
        shape.grid = new bool[25];
        foreach(int i in solidIndices)
        {
            shape.grid[i] = true;
        }

        // Gọi hàm OnValidate để lưu baseOffsets
        var method = typeof(ShapeData).GetMethod("OnValidate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(shape, null);

        AssetDatabase.CreateAsset(shape, $"Assets/Resources/Shapes/{name}.asset");
        return shape;
    }
}
