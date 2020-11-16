using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public struct TileSet
{
  public Texture2D texture;
  public int tileWidth;
  public int tileHeight;
  public TypeMap[] typeUVs;

  bool initialized;

  TypeMap[] _typeUVs;
  

  void Initialize()
  {
    int length = Enum.GetValues(typeof(TileType)).Length;
    _typeUVs = new TypeMap[length];

    foreach (TypeMap t in typeUVs)
    {
      _typeUVs[(int)t.type] = t;
    }

    initialized = true;
  }
    
  public IntCoord GetSideUVForType(TileType t)
  {
    if (!initialized)
      Initialize();

    if (_typeUVs[(int)t] == null)
      return IntCoord.Zero();

    return _typeUVs[(int)t].sideCoord;
  }

  public IntCoord GetUVForType(TileType t)
  {
    if (!initialized)
      Initialize();

    if (_typeUVs[(int)t] == null)
      return IntCoord.Zero();

    IntCoord coord = new IntCoord();
    coord.x = UnityEngine.Random.Range(_typeUVs[(int)t].minCoord.x, _typeUVs[(int)t].maxCoord.x + 1);
    coord.y = UnityEngine.Random.Range(_typeUVs[(int)t].minCoord.y, _typeUVs[(int)t].maxCoord.y + 1);
    //return _typeUVs[(int)t].coord;
    return coord;
  }

  public int[] GetUVsFlattened(){
    if (!initialized)
      Initialize();
      
    int count = Enum.GetNames(typeof(TileType)).Length;
    int[] output = new int[count*2];
    for (int i=0; i<count; i++){
      if (_typeUVs[i]==null) continue;
      output[i] =  _typeUVs[i].minCoord.x;
      output[count+i] = _typeUVs[i].minCoord.y;
    }
    return output;
  }

  [System.Serializable]
  public class TypeMap
  {
    public TileType type;
    public IntCoord minCoord;        // Coordinate of tile in texture map
    public IntCoord maxCoord;
    public IntCoord sideCoord;    // Coordinate of tile for side of hex
  }
}
