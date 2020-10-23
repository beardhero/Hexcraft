using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is basically a trimmed-down version of HexTile
// Ideally HexTile would inherit from this and add extra members, but I'm too lazy to do that rn
//   (Also that probably wouldn't work without setting up conditional serialization of extra members of Hexagon, which would be a waste of computation)
// This version is used for communication with server where file size matters
// Serializing PolySphere with Hextile produces a 17mb file
// Serializing a list of ServerTiles is only 3mb
[System.Serializable]
public class ServerTile{
    public int index;
    public float height;
    public SerializableVector3 center;
    public bool isPentagon, passable;
    public TileType type;
    public List<int> neighbors;

    public ServerTile(){}
    public ServerTile (HexTile ht){
        this.index = ht.index;
        this.height = ht.height;
        this.center = ht.hexagon.center.ToVector3();
        this.isPentagon = ht.hexagon.isPentagon;
        this.passable = ht.passable;
        this.type = ht.type;
        this.neighbors = ht.neighbors;
    }
}