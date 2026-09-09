using UnityEditor;
using UnityEngine;
using System.IO;

public static class ShapeDataGenerator
{
    [MenuItem("Block Blast/Generate Default Shapes")]
    public static void GenerateShapes()
    {
        string dir = "Assets/ScriptableObjects/Shapes";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        ShapeDatabase db = ScriptableObject.CreateInstance<ShapeDatabase>();
        
        // Emergency/Mercy Shape (1x1 is only used for rescue/mercy, never in normal gameplay)
        CreateShape("1_Dot_1x1", false, new[] {12});

        // Basic Shapes
        db.shapes.Add(CreateShape("1_Line_2", true, new[] {7, 12}));
        db.shapes.Add(CreateShape("1_Line_3", true, new[] {7, 12, 17}));
        db.shapes.Add(CreateShape("2_Square_2x2", false, new[] {7, 8, 12, 13}));
        db.shapes.Add(CreateShape("2_Line_4", true, new[] {2, 7, 12, 17}));
        db.shapes.Add(CreateShape("2_Shape_L", true, new[] {7, 12, 17, 18}));
        db.shapes.Add(CreateShape("2_Shape_J", true, new[] {7, 12, 17, 16}));
        db.shapes.Add(CreateShape("2_Shape_T", true, new[] {7, 11, 12, 13}));
        db.shapes.Add(CreateShape("2_Shape_S", true, new[] {7, 8, 11, 12}));
        db.shapes.Add(CreateShape("2_Shape_Z", true, new[] {6, 7, 12, 13}));
        db.shapes.Add(CreateShape("2_Small_V", true, new[] {7, 12, 13}));
        db.shapes.Add(CreateShape("3_Square_3x3", false, new[] {6, 7, 8, 11, 12, 13, 16, 17, 18}));
        db.shapes.Add(CreateShape("3_Line_5", true, new[] {2, 7, 12, 17, 22}));
        db.shapes.Add(CreateShape("3_Big_V", true, new[] {7, 12, 17, 18, 19}));
        db.shapes.Add(CreateShape("3_Rect_2x3", true, new[] {7, 8, 12, 13, 17, 18}));

        AssetDatabase.CreateAsset(db, $"{dir}/MasterShapeDatabase.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("🎉 Đã tạo thành công các khối gạch gốc và MasterShapeDatabase!");
    }

    private static ShapeData CreateShape(string name, bool canRotate, int[] solidIndices)
    {
        ShapeData shape = ScriptableObject.CreateInstance<ShapeData>();
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
