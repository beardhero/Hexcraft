using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Biome
{
    public List<int> tileIndexes;
    public TileType type;
    public int index;

    public Biome(){
        tileIndexes = new List<int>();
        index = -1; // null value
    }
}
