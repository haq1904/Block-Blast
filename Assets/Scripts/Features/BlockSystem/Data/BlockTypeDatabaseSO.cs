using UnityEngine;

[CreateAssetMenu(fileName = "BlockTypeDatabase", menuName = "Block Blast/Block Type Database")]
public class BlockTypeDatabaseSO : ScriptableObject
{
    [Tooltip("Catalog of all available block types across the entire game.")]
    public BlockTypeSO[] blockTypes;

    public BlockTypeSO GetBlockType(string typeId)
    {
        if (blockTypes == null) return null;
        for (int i = 0; i < blockTypes.Length; i++)
        {
            if (blockTypes[i] != null && blockTypes[i].typeId == typeId)
            {
                return blockTypes[i];
            }
        }
        return blockTypes.Length > 0 ? blockTypes[0] : null;
    }
}
