using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class is similar to CacheTile but contains the extra data sent back from the server
//  and does not contain data that can be instead extracted from Baseworld
[System.Serializable]
public class ServerTile
{
    public int i; // index
    public float h; // height
    public bool p;  // passable
    public TileType t;

    public ServerTile(){}
}
