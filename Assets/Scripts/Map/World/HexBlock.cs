using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HexBlock
{
    public int index;       // Index in tile.blocks
    public int tileIndex;       // Index of the underlying hexTile that this block is stacked upon
    // public HexTile tile;       // Reference to the underlying hexTile
    public int indexInPlate;
    public SerializableVector3 topCenter, topv1, topv2, topv3, topv4, topv5, topv6,
                                botCenter, botv1, botv2, botv3, botv4, botv5, botv6;
    public TileType type;
    //public int[] trindex; //triangle indexes
    public float height;  
    public int blockHeight;
    //public int plate;     // No longer needed with a reference to HexTile.plate
    //public bool plateOrigin;        // I think this refers to whether the plate hosting the tile is the parent of other plates. No longer needed
    public bool unbreakable;
    public bool quarterBlock;

    public void CreateBlock()
    {
        Hexagon hex = WorldManager.activeWorld.tiles[tileIndex].hexagon;
        float h = hex.center.magnitude;
        float f = 1 + BlockManager.blockScaleFactor;

        if (quarterBlock)
        { height = (h * BlockManager.blockQuarterFactor + h) * Mathf.Pow(f, blockHeight); }
        else
        { height = (h * BlockManager.blockScaleFactor + h) * Mathf.Pow(f, blockHeight); }

        float botHeight = h;
        if (blockHeight != 0)
        {
            botHeight = (h * BlockManager.blockScaleFactor + h) * Mathf.Pow(f, blockHeight - 1);
        }
        //float botHeight = h + (h * BlockManager.blockScaleFactor * blockHeight);

        topCenter = (hex.center / hex.center.magnitude) * height;
        topv1 = (hex.v1 / hex.v1.magnitude) * height;
        topv2 = (hex.v2 / hex.v2.magnitude) * height;
        topv3 = (hex.v3 / hex.v3.magnitude) * height;
        topv4 = (hex.v4 / hex.v4.magnitude) * height;
        topv5 = (hex.v5 / hex.v5.magnitude) * height;
        topv6 = (hex.v6 / hex.v6.magnitude) * height;
        //topCenter = (topv1 + topv2 + topv3 + topv4 + topv5 + topv6) / 6f;

        botCenter = (hex.center / hex.center.magnitude) * (botHeight);
        botv1 = (hex.v1 / hex.v1.magnitude) * (botHeight);
        botv2 = (hex.v2 / hex.v2.magnitude) * (botHeight);
        botv3 = (hex.v3 / hex.v3.magnitude) * (botHeight);
        botv4 = (hex.v4 / hex.v4.magnitude) * (botHeight);
        botv5 = (hex.v5 / hex.v5.magnitude) * (botHeight);
        botv6 = (hex.v6 / hex.v6.magnitude) * (botHeight);
        //botCenter = (botv1 + botv2 + botv3 + botv4 + botv5 + botv6) / 6f;
    }

    public void IncreaseByQuarterFromTop()
    {

    }
    public void IncreaseByQuarterFromBot()
    {

    }
    public void DecreaseByQuarterFromTop()
    {
        Mesh mesh = BlockManager.plateMeshes[WorldManager.activeWorld.tiles[tileIndex].plate];
        Vector3[] verts = mesh.vertices;

    }
    public void DecreaseByQuarterFromBot()
    {

    }

    public void ChangeType(TileType toType)
    {
        type = toType;
        Mesh mesh = BlockManager.plateMeshes[WorldManager.activeWorld.tiles[tileIndex].plate];
        IntCoord newCoord = WorldManager.staticTileSet.GetUVForType(toType);
        //newCoord.y = generation;
        Vector2 newOffset = new Vector2((newCoord.x * WorldRenderer.uvTileWidth), (newCoord.y * WorldRenderer.uvTileHeight));
        Vector2[] uvs = mesh.uv;
        //int ind = BlockManager.plateInfos[plate].blockIndexes.IndexOf(BlockManager.blocks.IndexOf(hb)) * 14;
        int ind = indexInPlate * 14;
        uvs[ind] = WorldRenderer.uv0 + newOffset;
        uvs[ind + 1] = WorldRenderer.uv1 + newOffset;
        uvs[ind + 2] = WorldRenderer.uv2 + newOffset;
        uvs[ind + 3] = WorldRenderer.uv3 + newOffset;
        uvs[ind + 4] = WorldRenderer.uv4 + newOffset;
        uvs[ind + 5] = WorldRenderer.uv5 + newOffset;
        uvs[ind + 6] = WorldRenderer.uv6 + newOffset;
        uvs[ind + 7] = WorldRenderer.uv0 + newOffset;
        uvs[ind + 8] = WorldRenderer.uv1 + newOffset;
        uvs[ind + 9] = WorldRenderer.uv2 + newOffset;
        uvs[ind + 10] = WorldRenderer.uv3 + newOffset;
        uvs[ind + 11] = WorldRenderer.uv4 + newOffset;
        uvs[ind + 12] = WorldRenderer.uv5 + newOffset;
        uvs[ind + 13] = WorldRenderer.uv6 + newOffset;
        mesh.uv = uvs;
        //catch (Exception e) { Debug.Log(" bad tile: " + index + " uv0: " + hexagon.uv0i + " error: " + e); }
    }
}
