using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HexBlock
{
    public int tileIndex;
    public SerializableVector3 topCenter, topv1, topv2, topv3, topv4, topv5, topv6,
                                botCenter, botv1, botv2, botv3, botv4, botv5, botv6;
    public TileType type;
    //public int[] trindex; //triangle indexes
    public float height;
    public int plate;

    //public bool bedrock;

    public HexBlock(HexTile tile, TileType tileType, float topHeight, float botHeight)//, bool isBedrock)
    {
        tileIndex = tile.index;
        //bedrock = isBedrock;
        height = topHeight;
        type = tileType;
        plate = tile.plate;

        topCenter = (tile.hexagon.center/tile.hexagon.center.magnitude) * height;
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
}
