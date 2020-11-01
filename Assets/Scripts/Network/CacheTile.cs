using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is basically a trimmed-down version of HexTile. 
// This version is used for communication with server where file size matters
// Because everything is converted to strings, the shorter the var names the better
// Serializing PolySphere with Hextile produces a 17mb file
// Serializing a list of ServerTiles is only 3mb
// After the data returns from the server it is stored as a ServerTile, which is slightly larger
[System.Serializable]
public class CacheTile{
    public int i; // index
    public float x, y, z;
    public List<int> n; // neightbors
    public bool p;  //pentagon

    public CacheTile(){}
    public CacheTile (HexTile ht){
        this.i = ht.index;
        this.x = ht.hexagon.center.x;
        this.y = ht.hexagon.center.y;
        this.z = ht.hexagon.center.z;
        this.p = ht.hexagon.isPentagon;
        this.n = ht.neighbors;
    }
}