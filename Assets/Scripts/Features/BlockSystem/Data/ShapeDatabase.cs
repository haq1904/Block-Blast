using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shape Database", menuName = "Block Blast/Shape Database")]
public class ShapeDatabase : ScriptableObject
{
    [Header("Shape inventory categorized by difficulty tier")]
    public List<ShapeData> tier1Shapes = new List<ShapeData>();
    public List<ShapeData> tier2Shapes = new List<ShapeData>();
    public List<ShapeData> tier3Shapes = new List<ShapeData>();
}
