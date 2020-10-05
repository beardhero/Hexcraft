using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class BlockInfo : MonoBehaviour
{
    //public int blockCount;
    public int plateIndex;
    public Vector3 plateOrigin;
    //public List<int> blockIndexes;    // Replaced by tiles reference. tiles[index].blocks contains block references

    // This is a list of all block indexes, in *tri vertex order* plat order.
    //   The int[] maps => 0=tile index 1=block index on that tile
    public List<int[]> blockIndices;      
}
