using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HexBlock
{
    public int tileIndex;
    public int indexInPlate;
    public SerializableVector3 topCenter, topv1, topv2, topv3, topv4, topv5, topv6,
                                botCenter, botv1, botv2, botv3, botv4, botv5, botv6;
    public TileType type;
    //public int[] trindex; //triangle indexes
    public float height;
    public int blockHeight;
    public int plate;
    public bool plateOrigin;
    public bool unbreakable;
    public bool quarter;

    public HexBlock(int hexTileIndex, TileType tileType, int _blockHeight, bool canBreak, bool quarterBlock)
    {
        HexTile tile = WorldManager.activeWorld.tiles[hexTileIndex];
        tileIndex = hexTileIndex;
        blockHeight = _blockHeight;
        quarter = quarterBlock;
        //height = topHeight;
        float h = tile.hexagon.center.magnitude;
        float f = 1 + BlockManager.blockScaleFactor;
        /* if (quarterBlock)
         {
             f = 1 + BlockManager.blockQuarterFactor;
         }*/
        if (quarterBlock)
        { height = (h * BlockManager.blockQuarterFactor + h) * Mathf.Pow(f, blockHeight); }
        else
        { height = (h * BlockManager.blockScaleFactor + h) * Mathf.Pow(f, blockHeight); }
        //height = h + (h * BlockManager.blockScaleFactor * (blockHeight + 1));
        float botHeight = h;
        if (blockHeight != 0)
        {
            botHeight = (h * BlockManager.blockScaleFactor + h) * Mathf.Pow(f, blockHeight - 1);
        }
        //float botHeight = h + (h * BlockManager.blockScaleFactor * blockHeight);
        type = tileType;
        plate = tile.plate;
        plateOrigin = tile.plateOrigin;
        unbreakable = canBreak;
        blockHeight = _blockHeight;

        topCenter = (tile.hexagon.center / tile.hexagon.center.magnitude) * height;
        topv1 = (tile.hexagon.v1 / tile.hexagon.v1.magnitude) * height;
        topv2 = (tile.hexagon.v2 / tile.hexagon.v2.magnitude) * height;
        topv3 = (tile.hexagon.v3 / tile.hexagon.v3.magnitude) * height;
        topv4 = (tile.hexagon.v4 / tile.hexagon.v4.magnitude) * height;
        topv5 = (tile.hexagon.v5 / tile.hexagon.v5.magnitude) * height;
        topv6 = (tile.hexagon.v6 / tile.hexagon.v6.magnitude) * height;
        //topCenter = (topv1 + topv2 + topv3 + topv4 + topv5 + topv6) / 6f;

        botCenter = (tile.hexagon.center / tile.hexagon.center.magnitude) * (botHeight);
        botv1 = (tile.hexagon.v1 / tile.hexagon.v1.magnitude) * (botHeight);
        botv2 = (tile.hexagon.v2 / tile.hexagon.v2.magnitude) * (botHeight);
        botv3 = (tile.hexagon.v3 / tile.hexagon.v3.magnitude) * (botHeight);
        botv4 = (tile.hexagon.v4 / tile.hexagon.v4.magnitude) * (botHeight);
        botv5 = (tile.hexagon.v5 / tile.hexagon.v5.magnitude) * (botHeight);
        botv6 = (tile.hexagon.v6 / tile.hexagon.v6.magnitude) * (botHeight);
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
        Mesh mesh = BlockManager.plateMeshes[plate];
        Vector3[] verts = mesh.vertices;

    }
    public void DecreaseByQuarterFromBot()
    {

    }

    public void ChangeType(TileType toType)
    {
        type = toType;
        Mesh mesh = BlockManager.plateMeshes[plate];
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
