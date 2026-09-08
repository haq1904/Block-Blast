using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShapeData))]
public class ShapeDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ShapeData shape = (ShapeData)target;

        Undo.RecordObject(shape, "Shape Modification");

        // Draw basic parameters
        shape.tier = EditorGUILayout.IntSlider("Tier (Difficulty)", shape.tier, 1, 3);
        shape.canRotate = EditorGUILayout.Toggle("Allow Rotation (Auto-gen)", shape.canRotate);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Draw Shape (Click grid cells)", EditorStyles.boldLabel);

        if (shape.grid == null || shape.grid.Length != 25)
        {
            shape.grid = new bool[25];
        }

        // Create 5x5 drawing grid
        GUILayout.BeginVertical("box");
        for (int y = 0; y < 5; y++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // Center the grid
            for (int x = 0; x < 5; x++)
            {
                int index = y * 5 + x;
                // Color: Red if solid, Default if empty. (Center is slightly darker)
                Color oldColor = GUI.backgroundColor;
                
                if (shape.grid[index])
                {
                    GUI.backgroundColor = Color.red;
                }
                else if (x == 2 && y == 2)
                {
                    GUI.backgroundColor = Color.gray; // Center cell
                }

                // Button
                if (GUILayout.Button("", GUILayout.Width(40), GUILayout.Height(40)))
                {
                    shape.grid[index] = !shape.grid[index]; // Toggle
                    EditorUtility.SetDirty(shape); // Mark dirty so Unity saves
                }
                
                GUI.backgroundColor = oldColor;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        // Hint for user
        EditorGUILayout.HelpBox("Note: The gray cell in the middle is the Pivot! Draw the shape relative to this center.", MessageType.Info);
        
        // Clear button
        if (GUILayout.Button("Clear Grid"))
        {
            shape.grid = new bool[25];
            EditorUtility.SetDirty(shape);
        }

        if (GUI.changed)
        {
            // Update baseOffsets array when grid changes
            var method = typeof(ShapeData).GetMethod("OnValidate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(shape, null);
            }
        }
    }
}
