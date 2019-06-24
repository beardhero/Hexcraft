using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HexBlock
{
    public HexTile tile;
    public SerializableVector3 topCenter, topv1, topv2, topv3, topv4, topv5, topv6,
                                botCenter, botv1, botv2, botv3, botv4, botv5, botv6;
    public TileType type;
    public int[] trindex; //triangle indexes
    public float height;

    public bool bedrock;

    public HexBlock(HexTile tile, float topHeight, bool isBedrock)
    {
        bedrock = isBedrock;
        height = topHeight;
        type = tile.type;

        topCenter = (tile.hexagon.center/tile.hexagon.center.magnitude) * height;
        topv1 = (tile.hexagon.v1 / tile.hexagon.v1.magnitude) * height;
        topv2 = (tile.hexagon.v2 / tile.hexagon.v2.magnitude) * height;
        topv3 = (tile.hexagon.v3 / tile.hexagon.v3.magnitude) * height;
        topv4 = (tile.hexagon.v4 / tile.hexagon.v4.magnitude) * height;
        topv5 = (tile.hexagon.v5 / tile.hexagon.v5.magnitude) * height;
        topv6 = (tile.hexagon.v6 / tile.hexagon.v6.magnitude) * height;

        //botCenter = (tile.hexagon.center / tile.hexagon.center.magnitude) * (height-1);
        botv1 = (tile.hexagon.v1 / tile.hexagon.v1.magnitude) * (height - 1);
        botv2 = (tile.hexagon.v2 / tile.hexagon.v2.magnitude) * (height - 1);
        botv3 = (tile.hexagon.v3 / tile.hexagon.v3.magnitude) * (height - 1);
        botv4 = (tile.hexagon.v4 / tile.hexagon.v4.magnitude) * (height - 1);
        botv5 = (tile.hexagon.v5 / tile.hexagon.v5.magnitude) * (height - 1);
        botv6 = (tile.hexagon.v6 / tile.hexagon.v6.magnitude) * (height - 1);
        botCenter = (botv1 + botv2 + botv3 + botv4 + botv5 + botv6) / 6f;
    }
}
