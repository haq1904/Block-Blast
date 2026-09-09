using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shape Database", menuName = "Block Blast/Shape Database")]
public class ShapeDatabase : ScriptableObject
{
    [Header("All available shapes in the game")]
    public List<ShapeData> shapes = new List<ShapeData>();
}
