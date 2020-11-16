using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LibNoise.Unity.Generator;
using System.Linq;
using Unity.Collections; 

public enum WorldSize {None, Small, Medium, Large};
public enum WorldType {None, Verdant, Frigid, Oceanic, Barren, Volcanic, Radiant, Gaseous};
public enum Season {None, Spring, Summer, Fall, Winter};
public enum AxisTilt { None, Slight, Moderate, Severe };      // Affects intensity of difficulty scaling during seasons


[System.Serializable]
public class World
{
  public int[] state;
  public const string cachePath = "/Resources/currentWorld.bytes";
  public string name;
  public WorldSize size;
  public WorldType type;
  public Season season;
  public AxisTilt tilt;
  public TileType element;

  public float avgHeight;
  public float oceanLevel;
  public float maxHeight;
  public float glyphProb = 0.006f; //distribution of glyphs
  public float populationProb = 0;//.42f;
  public int maxObjects = 2400;
  public static int zeroState = 3;
  public static int oneState = 4;
  [HideInInspector] public List<Biome> biomes;
  [HideInInspector] public List<HexTile> tiles;
  [HideInInspector] public SerializableVector3 origin;
  [HideInInspector] public int circumferenceInTiles;
  [HideInInspector] public int distanceBetweenTiles;
  [HideInInspector] public float circumference, radius;
  [HideInInspector] public int numberOfPlates; //Set by polysphere on cache
  [HideInInspector] public float seaLevel = 0;
  [HideInInspector] public List<TriTile> triTiles;
  [HideInInspector] public List<HexTile> pentagons;
  [HideInInspector] public List<Rune> runes;
  //[HideInInspector] public List<Plate> plates;
  [HideInInspector] public Dictionary<int, int> tileToPlate; //key hextile.index, value plate index
  
  [HideInInspector] public int[] heightmap;
  [System.NonSerializedAttribute] PolySphere activeSphere;  // used for generating/caching

  [System.NonSerializedAttribute] public NativePlateData[] platesData;
  private bool neighborInit;
  private List<List<HexTile>> _neighbors;
  public List<List<HexTile>> neighbors{
    get{
      if (!neighborInit)
      {
        if (tiles.Count < 1)
          Debug.LogError("Making neighbor list from null tiles");

        neighborInit = true;
        _neighbors = new List<List<HexTile>>();

        foreach (HexTile t in tiles)
        {
          List<HexTile> neighbs = new List<HexTile>();

          for (int i=0; i<t.hexagon.neighbors.Length; i++)
          {
            try
            {
              neighbs.Add(tiles[t.hexagon.neighbors[i]]);
            }
            catch (System.Exception e)
            {
              Debug.LogError(e + "tile "+t.index+"'s "+Direction.ToString(i)+" neighbor is bad: "+t.hexagon.neighbors[i]);
            }
          }
          _neighbors.Add(neighbs);
        }
      }

      return _neighbors;
    }
    set{}
  }

  public World()
  {
    origin = Vector3.zero;
  }

  // Used for batching data for threading
  public struct NativePlateData{
    public NativeArray<float> heights;
    public NativeArray<Vector3> centers, v1s, v2s, v3s, v4s, v5s, v6s,
      sideNorm1, sideNorm2, sideNorm3, sideNorm4, sideNorm5, sideNorm6;
    public NativeArray<int> types;
    public void Dispose(){
      heights.Dispose();
      centers.Dispose();
      v1s.Dispose();v2s.Dispose();v3s.Dispose();v4s.Dispose();v5s.Dispose();v6s.Dispose();
      types.Dispose();
      sideNorm1.Dispose();sideNorm2.Dispose();sideNorm3.Dispose();sideNorm4.Dispose();sideNorm5.Dispose();sideNorm6.Dispose();
    }
  }

  public World(PolySphere baseworld, ServerWorld serverWorld)
  {
    origin = Vector3.zero;    // Will we ever use this?
    numberOfPlates = baseworld.numberOfPlates;
    tiles = new List<HexTile>();

    if (baseworld.hexTiles.Count != serverWorld.tiles.Length)
      Debug.LogError("Count mismatch between baseworld and server world: "+baseworld.hexTiles.Count+" vs. "+serverWorld.tiles.Length);

    int maxPlateNumber = 0;
    // Hard copy tile data
    for(int i=0; i<serverWorld.tiles.Length; i++)
    {
      HexTile ht = new HexTile();
      ht.index = i;
      ht.height = serverWorld.tiles[i].h;

      Hexagon h = new Hexagon();    // We don't use the previous hexagon builder constructor because we don't need to do normal calculation
      h.index = i;
      h.center = baseworld.hexTiles[i].hexagon.center;
      h.normal = baseworld.hexTiles[i].hexagon.normal;
      h.v1 = baseworld.hexTiles[i].hexagon.v1;
      h.v2 = baseworld.hexTiles[i].hexagon.v2;
      h.v3 = baseworld.hexTiles[i].hexagon.v3;
      h.v4 = baseworld.hexTiles[i].hexagon.v4;
      h.v5 = baseworld.hexTiles[i].hexagon.v5;
      h.v6 = baseworld.hexTiles[i].hexagon.v6;
      h.sideNormal1 = baseworld.hexTiles[i].hexagon.sideNormal1;
      h.sideNormal2 = baseworld.hexTiles[i].hexagon.sideNormal2;
      h.sideNormal3 = baseworld.hexTiles[i].hexagon.sideNormal3;
      h.sideNormal4 = baseworld.hexTiles[i].hexagon.sideNormal4;
      h.sideNormal5 = baseworld.hexTiles[i].hexagon.sideNormal5;
      h.sideNormal6 = baseworld.hexTiles[i].hexagon.sideNormal6;
      // @TODO: is hexagon.uv's used at all? seems like it once was but not any more
      //h.neighbors = baseworld.hexTiles[i].neighbors.ToArray<int>();   // hexagon neighbors also deprecated?
      h.isPentagon = baseworld.hexTiles[i].hexagon.isPentagon;  
      // Not setting hexagon scale because I'm not sure how it's used (is it supposed to be a Scale() method?)
      ht.hexagon = h;

      ht.type = serverWorld.tiles[i].t;
      ht.neighbors = baseworld.hexTiles[i].neighbors;   // Should go either way
      ht.passable = serverWorld.tiles[i].p;
      ht.plate = baseworld.hexTiles[i].plate;
      // counting plates
      if (ht.plate > maxPlateNumber) maxPlateNumber = ht.plate;
      tiles.Add(ht);
    }

    origin = new SerializableVector3(0,0,0);    // idk
    oceanLevel = serverWorld.oceanLevel;
    maxHeight = serverWorld.maxHeight;

    // ----Build Plate Data----
    maxPlateNumber++;   // Cause it's 0-indexed
    // First we need temp lists
    List<float>[] tmp_heights = new List<float>[maxPlateNumber];
    List<Vector3>[] tmp_centers = new List<Vector3>[maxPlateNumber];
    List<Vector3>[] tmp_v1s = new List<Vector3>[maxPlateNumber];
    List<Vector3>[] tmp_v2s = new List<Vector3>[maxPlateNumber];
    List<Vector3>[] tmp_v3s = new List<Vector3>[maxPlateNumber];
    List<Vector3>[] tmp_v4s = new List<Vector3>[maxPlateNumber];
    List<Vector3>[] tmp_v5s = new List<Vector3>[maxPlateNumber];
    List<Vector3>[] tmp_v6s = new List<Vector3>[maxPlateNumber];
    List<int>[] tmp_types = new List<int>[maxPlateNumber];
    List<Vector3>[] sn1 = new List<Vector3>[maxPlateNumber];
    List<Vector3>[] sn2 = new List<Vector3>[maxPlateNumber];
    List<Vector3>[] sn3 = new List<Vector3>[maxPlateNumber];
    List<Vector3>[] sn4 = new List<Vector3>[maxPlateNumber];
    List<Vector3>[] sn5 = new List<Vector3>[maxPlateNumber];
    List<Vector3>[] sn6 = new List<Vector3>[maxPlateNumber];

    for (int i=0; i<maxPlateNumber; i++){
      tmp_heights[i] = new List<float>();
      tmp_centers[i] = new List<Vector3>();
      tmp_v1s[i] = new List<Vector3>();
      tmp_v2s[i] = new List<Vector3>();
      tmp_v3s[i] = new List<Vector3>();
      tmp_v4s[i] = new List<Vector3>();
      tmp_v5s[i] = new List<Vector3>();
      tmp_v6s[i] = new List<Vector3>();
      tmp_types[i] = new List<int>();
      sn1[i] = new List<Vector3>();
      sn2[i] = new List<Vector3>();
      sn3[i] = new List<Vector3>();
      sn4[i] = new List<Vector3>();
      sn5[i] = new List<Vector3>();
      sn6[i] = new List<Vector3>();
    }

    foreach (HexTile tile in tiles){
      tmp_heights[tile.plate].Add(tile.height);
      tmp_centers[tile.plate].Add(tile.hexagon.center);
      tmp_v1s[tile.plate].Add(tile.hexagon.v1);
      tmp_v2s[tile.plate].Add(tile.hexagon.v2);
      tmp_v3s[tile.plate].Add(tile.hexagon.v3);
      tmp_v4s[tile.plate].Add(tile.hexagon.v4);
      tmp_v5s[tile.plate].Add(tile.hexagon.v5);
      tmp_v6s[tile.plate].Add(tile.hexagon.v6);
      tmp_types[tile.plate].Add((int)tile.type);
      sn1[tile.plate].Add(tile.hexagon.sideNormal1);
      sn2[tile.plate].Add(tile.hexagon.sideNormal2);
      sn3[tile.plate].Add(tile.hexagon.sideNormal3);
      sn4[tile.plate].Add(tile.hexagon.sideNormal4);
      sn5[tile.plate].Add(tile.hexagon.sideNormal5);
      sn6[tile.plate].Add(tile.hexagon.sideNormal6);
    }

    // Now convert the temp lists into NativeContainers
    platesData = new NativePlateData[maxPlateNumber];
    for (int i=0; i<maxPlateNumber; i++){
      platesData[i] = new NativePlateData();
      platesData[i].heights = new NativeArray<float>(tmp_heights[i].ToArray(), Allocator.TempJob);
      platesData[i].centers = new NativeArray<Vector3>(tmp_centers[i].ToArray(), Allocator.TempJob);
      platesData[i].v1s = new NativeArray<Vector3>(tmp_v1s[i].ToArray(), Allocator.TempJob);
      platesData[i].v2s = new NativeArray<Vector3>(tmp_v2s[i].ToArray(), Allocator.TempJob);
      platesData[i].v3s = new NativeArray<Vector3>(tmp_v3s[i].ToArray(), Allocator.TempJob);
      platesData[i].v4s = new NativeArray<Vector3>(tmp_v4s[i].ToArray(), Allocator.TempJob);
      platesData[i].v5s = new NativeArray<Vector3>(tmp_v5s[i].ToArray(), Allocator.TempJob);
      platesData[i].v6s = new NativeArray<Vector3>(tmp_v6s[i].ToArray(), Allocator.TempJob);
      platesData[i].types = new NativeArray<int>(tmp_types[i].ToArray(), Allocator.TempJob);
      platesData[i].sideNorm1 = new NativeArray<Vector3>(sn1[i].ToArray(), Allocator.TempJob);
      platesData[i].sideNorm2 = new NativeArray<Vector3>(sn2[i].ToArray(), Allocator.TempJob);
      platesData[i].sideNorm3 = new NativeArray<Vector3>(sn3[i].ToArray(), Allocator.TempJob);
      platesData[i].sideNorm4 = new NativeArray<Vector3>(sn4[i].ToArray(), Allocator.TempJob);
      platesData[i].sideNorm5 = new NativeArray<Vector3>(sn5[i].ToArray(), Allocator.TempJob);
      platesData[i].sideNorm6 = new NativeArray<Vector3>(sn6[i].ToArray(), Allocator.TempJob);
    }
  }
  
  public Vector3 GetPositionOfTile(int index){
    return tiles[index].hexagon.center;
  }

  // deprecated
  public World(WorldSize s, WorldType t, Season se, AxisTilt at)
  {
    size = s;
    type = t;
    season = se;
    tilt = at;
    origin = Vector3.zero;
  }

    // Deprecated  
    public void Populate(string seed)
    {
      // Base heighmap
      float[] heights = GenerateHeightmap(PerlinType.DefaultSurface());
      heightmap = new int[heights.Length];
      // Convert from decimal-based to byte-based height
      float amplitude = BlockManager.maxHeight / 4;
      for (int h=0; h<heights.Length; h++)
      {
        double v1 = heights[h] * amplitude;
        heightmap[h] = (int)(v1 + (BlockManager.maxHeight/2f));      // Note that we pad the values to prevent negative heights
      }

      avgHeight = 0;
      foreach (int h in heightmap)
          avgHeight += h;
      avgHeight /= heightmap.Length;
      oceanLevel = avgHeight-1; //+1;  // Note that +1 makes an island world and -1 makes a dry world

      // Impassable tiles map
      for (int i=0; i<heightmap.Length; i++)
      {
        if (heightmap[i] < oceanLevel)
        {
          heightmap[i]--;   // Just to make it a little more visible
          //heightmap[i] = (int)((heightmap[i] + oceanLevel*2)/3.0f) -2;
          tiles[i].passable = false;
          tiles[i].type = TileType.Void;    // @Todo: make base world type
          //heightmap[i] = (int)(heightmap[i] * impassMap[i] * 4);
        }
      }

      // // Perlin Biomes
      // float[] bioMap = GenerateHeightmap(PerlinType.DefaultBiome());
      // // Set Tile Types
      // for (int t=0; t<heightmap.Length; t++)
      // {
      //   if (!tiles[t].passable) {tiles[t].type = TileType.Water; }    // Need to switch to Divine type once set up
      //   else if (bioMap[t] > .55f) {tiles[t].type = TileType.Light; } // Light
      //   else if (bioMap[t] > .48f) {tiles[t].type = TileType.Air; } // Air
      //   else if (bioMap[t] > .45f) {tiles[t].type = TileType.Fire; }  // Fire
      //   else if (bioMap[t] > .39f) {tiles[t].type = TileType.Earth; }  // Earth
      //   else if (bioMap[t] > .27f) {tiles[t].type = TileType.Water; }  // Water
      //   else  {tiles[t].type = TileType.Dark; } // Dark
      // }

      // --- Flood Fill biomes ---
      // Set all to Gray to start
      for (int s=0; s<tiles.Count; s++)
      {
        if (tiles[s].passable)// && Random.value < .5f)
          tiles[s].type = TileType.Gray;  // Gray=toCheck
      }

      // Then set a starting biome on a tile and "fill" by checking each of it's neighbors.
      //  If it's gray then add it's neighbors to a list of tiles to check again next round
      TileType currentType = TileType.Dark;
      int maxBiomeSize = Random.Range(75,88);
      int minBiomeSize = 75;
      biomes = new List<Biome>();
      Biome currentBiome = new Biome();
      currentBiome.type = currentType;
      currentBiome.index = 0;

      for (int tileIndex=0; tileIndex<tiles.Count; tileIndex++)
      {
        if (tiles[tileIndex].type != TileType.Gray) continue;

        tiles[tileIndex].type = currentType;
        currentBiome.tileIndexes.Add(tiles[tileIndex].index);
        tiles[tileIndex].biomeIndex = currentBiome.index;
        int count = 1;    // Cause of the assignment above ^

        RecursiveFill(tileIndex, currentType, ref count, maxBiomeSize, currentBiome, false);
        if (count < minBiomeSize){
          RecursiveFill(tileIndex, currentType, ref count, minBiomeSize, currentBiome, true);
        }

        // When recursiveFill finishes, we've reached the end of this contiguous mass
        //  Start next biome
        biomes.Add(currentBiome);
        currentBiome = new Biome();
        currentBiome.index = biomes.Count;
        currentType++;
        if (currentType > TileType.Fire)
          currentType = TileType.Dark;  // If we've reached the end of the type list, start again from Water @TODO: create a list of types to use rather than using all

        currentBiome.type = currentType;    // It's important to split this up so we don't increment current biome's type

        maxBiomeSize = Random.Range(12,88); // Set this for the next run
      }
    }

    void RecursiveFill(int index, TileType type, ref int count, int max, Biome currentBiome, bool overrideNeighbors)
    {
      // Check the six neighbors of index
      for (int n=0; n<tiles[index].neighbors.Count; n++)
      {
        // If it's gray it's definitely passable. If we're overriding, double check
        if (tiles[tiles[index].neighbors[n]].type == TileType.Gray || (tiles[tiles[index].neighbors[n]].passable && overrideNeighbors))
        {
          if (tiles[tiles[index].neighbors[n]].type != TileType.Gray && tiles[tiles[index].neighbors[n]].type != type) // We're overriding a neighbor (that's not our type) and need to take it out of an existing biome list
          {
            biomes[tiles[tiles[index].neighbors[n]].biomeIndex].tileIndexes.Remove(tiles[index].neighbors[n]);
          }

          count++;
          tiles[tiles[index].neighbors[n]].type = type;
          currentBiome.tileIndexes.Add(tiles[index].neighbors[n]);
          tiles[tiles[index].neighbors[n]].biomeIndex = currentBiome.index;

          if (count > max)
            return;
          else
            RecursiveFill(tiles[index].neighbors[n], type, ref count, max, currentBiome, overrideNeighbors);
        }
      }
    }

    // In general, this returns values in the range of [0,1] (needs testing)
    public float[] GenerateHeightmap(Perlin perlin)
    {
        float[] hmap = new float[tiles.Count];

        UnityEngine.Random.InitState(PerlinType.globalSeed.GetHashCode());

        for (int i = 0; i < WorldManager.activeWorld.tiles.Count; i++)
        {
            HexTile ht = WorldManager.activeWorld.tiles[i];
            //Get next height
            // Note static float hexScale = 99;
            // Becase perlin seems to return values in the [-5,5] range, we add 5 and divide by 5
            //  (edit: after some experimentation (4+perlin)/9 seems to return the best result, meaning close to [0,1] range)
            hmap[i] = (4+(float)perlin.GetValue(ht.hexagon.center.x * BlockManager.hexScale, ht.hexagon.center.y * BlockManager.hexScale, ht.hexagon.center.z * BlockManager.hexScale))/9;
        }

        // Testing
        // float min = 999, max = -999;
        // for (int x=0; x<hmap.Length; x++){
        //   if (hmap[x] > max)
        //     max = hmap[x];
        //   if (hmap[x] < min)
        //     min = hmap[x];
        // }
        // Debug.Log("Perlin generated a range of ["+min+","+max+"]");

        return hmap;
    }

  // old populate
  /*
  public void Populate(byte[] seed)
  {
    Object[] airBiome = Resources.LoadAll("Air/");
    Object[] earthBiome = Resources.LoadAll("Earth/");
    Object[] waterBiome = Resources.LoadAll("Water/");
    Object[] fireBiome = Resources.LoadAll("Fire/");
    Object[] darkBiome = Resources.LoadAll("Dark/");
    Object[] lightBiome = Resources.LoadAll("Light/");
    Object[] misc = Resources.LoadAll("Misc/");

    Perlin perlin = new Perlin();
    double f = 0;
    double p = 0;
    double l = 0;
    int o = 0;
    //int pSeed = 0;
    float sc = 0; //99.0f
    float amplitude = 42; //42
    //int h = 0;
    //int iterations = 0;
    runes = new List<Rune>();
    //set generation to 3, the 0 state for life casting
    foreach(HexTile ht in tiles)
    {
      ht.generation = zeroState;
    }
    sc = 99; //Random.Range(99f,111f);
    f = 0.0000024; //(double)Random.Range(.0000014618f,.000001918f); //(double)Random.Range(0.000000618f,0.000000918f);//.01618;// * Random.Range(0.5f,1.5f);// * i; //.0000024
    l = 2.4; //(double)Random.Range(2.24f,4.42f);// * Random.Range(0.5f,1.5f);//2.4;
    p = .2; //(double)Random.Range(.16f,.191f);// * Random.Range(0.5f,1.5f); //.24
    o = 6; //Random.Range(3,7);// + i;
    amplitude = 42; //Random.Range(24,42);
    glyphProb /= 64;
    for(int i = 0; i < seed.Length; i++)
    {
      UnityEngine.Random.InitState(seed[i]);
      perlin.Seed = seed[i];

      float stepHeight = Random.Range(0, 0);// 0.1f,0.5f);
      PerlinPopulate(perlin,f,l,p,o,amplitude,sc,stepHeight);
    }
    
      //Heightmap perlin seed
      //UnityEngine.Random.InitState(randseed);
      //perlin.Seed = perlinseed;
      //pSeed += seed[i];
      
      //iterations = 32;//Random.Range(2,7);

    //Populate with params found in seed
    //Normalize perlin values
    //pSeed /= seed.Length;
    //sc /= seed.Length;
    //f /= seed.Length;
    //l /= seed.Length;
    //p /= seed.Length;
    //iterations = (iterations % 12) + 1; //seed.Length;
    //o = (o % 32) + 3;

    //glyphProb /= iterations;
    //populationProb /= iterations;
    //Debug.Log("octave: " + o + " sc: " + sc + " freq: " + f + " lac: " + l + " pers: " + p + " iterations: " + iterations);
  
    

    //PerlinPopulate(perlin,pSeed,f*6,l,p,o,amplitude,sc);
    //PerlinPopulate(perlin,pSeed,f,l,p,o,amplitude*10,sc);
    
    
    
    //biomes and ocean
  
    int water = 0; 
    int fire = 0;
    int vapor = 0;
    int crystal = 0;

    seaLevel = AverageTileHeight() - 1;// + 0.1f;
    Debug.Log("sea level: " + seaLevel);
    //water world
    foreach(HexTile ht in tiles)
    {
      if(ht.type == TileType.Water){water++;}
      if(ht.type == TileType.Fire){fire++;}
      if(ht.type == TileType.Vapor){vapor++;}
      if(ht.type == TileType.Crystal){crystal++;}
    }

    if(water >= fire && water >= vapor && water >= crystal)
    {
      element = TileType.Water;
      foreach(HexTile ht in tiles)
      {
        if((ht.type == TileType.Water))//&& ht.hexagon.scale < seaLevel)
        {
          if(ht.hexagon.scale > seaLevel)
          {
            ht.type = TileType.Earth;
          }
        }
        
      }
      LightToDark();
    }
    //fire world
    if(fire > water && fire > vapor && fire > crystal)
    {
      element = TileType.Fire;
      foreach(HexTile ht in tiles)
      {
        
        if((ht.type == TileType.Water || ht.type == TileType.Fire))// && ht.hexagon.scale < seaLevel)
        {
          //ht.hexagon.scale = seaLevel;
        }
        
      }
      DarkToLight();
    }
    //vapor
    if(vapor >= water && vapor >= fire && vapor >= crystal)
    {
      element = TileType.Vapor;
      //foreach(HexTile ht in tiles)
      //{
        
        // if(ht.type == TileType.Vapor || ht.type == TileType.Crystal)
        // {
        //   ht.hexagon.scale = seaLevel;
        // }
        
      //}
      //DarkToLight();
    }
    //crystal
    if(crystal > fire && crystal > vapor && crystal > water)
    {
      element = TileType.Crystal;
      //foreach(HexTile ht in tiles)
      //{

        // if(ht.type == TileType.Vapor || ht.type == TileType.Crystal)
        // {
        //   ht.hexagon.scale = seaLevel;
        // }

      //}
      //LightToDark();
    }
     
    foreach(HexTile ht in tiles)
    {
      if(element == TileType.Water || element == TileType.Fire)
      {    
        if(ht.hexagon.scale <= seaLevel)
        {
          ht.type = element;
          ht.oceanTile = true;
          //ht.passable = false;
          ht.hexagon.scale = seaLevel;
        }
      }
    }
    
    int numObjects = 0;
    //biome objects
    foreach(HexTile ht in tiles)
    {
      //if surrounded by ocean tiles, become oceantile
      int x = 0;
      foreach(int n in ht.neighbors)
      {
        if(tiles[n].oceanTile)
        {
          x++;
        }
      }
      if(x == ht.neighbors.Count)
      {
        ht.generation = zeroState;
        ht.hexagon.scale = seaLevel;
      }
      
      //choose which object to place
      if(ht.placeObject && numObjects <= maxObjects && !ht.oceanTile)
      {
        ht.passable = false;
        numObjects++;
        switch(ht.type)
        {
          case TileType.Gray: ht.objectToPlace = Random.Range(0,misc.Length); break;
          case TileType.Water: ht.objectToPlace = Random.Range(0,waterBiome.Length); break;
          case TileType.Fire: ht.objectToPlace = Random.Range(0,fireBiome.Length); break;
          case TileType.Earth: ht.objectToPlace = Random.Range(0,earthBiome.Length); break;
          case TileType.Air: ht.objectToPlace = Random.Range(0,airBiome.Length); break;
          case TileType.Dark: ht.objectToPlace = Random.Range(0,darkBiome.Length); break;
          case TileType.Light: ht.objectToPlace = Random.Range(0,lightBiome.Length); break;

          case TileType.Ice: ht.objectToPlace = Random.Range(0,waterBiome.Length); break;
          case TileType.Metal: ht.objectToPlace = Random.Range(0,fireBiome.Length); break;
          case TileType.Arbor: ht.objectToPlace = Random.Range(0,earthBiome.Length); break;
          case TileType.Vapor: ht.objectToPlace = Random.Range(0,airBiome.Length); break;
          case TileType.Astral: ht.objectToPlace = Random.Range(0,darkBiome.Length); break;
          case TileType.Crystal: ht.objectToPlace = Random.Range(0,lightBiome.Length); break;
          default: break;
        }
      }
    }
    //random generation
    foreach (HexTile ht in tiles)
    {
        if (ht.generation == zeroState)
        {
             ht.generation = Random.Range(zeroState, oneState+1);
        }
    }
  }
  */
  public void PerlinPopulate(Perlin perlin, double frequency, double lacunarity, double persistence, int octave, float amplitude, float scale, float stepHeight)
  {
    //Random.InitState(seed);
    //perlin.Seed = seed;
    perlin.Frequency = frequency;
    perlin.Lacunarity = lacunarity;
    perlin.Persistence = persistence;
    perlin.OctaveCount = octave;
    int typeShift = Random.Range(0,21);
    foreach(HexTile ht in tiles)
    {
      //Get next height
      double perlinVal = perlin.GetValue(ht.hexagon.center.x * scale, ht.hexagon.center.y * scale, ht.hexagon.center.z * scale);
      double v1 = perlinVal*amplitude;//*i; 
      int h = (int)v1;
      //Debug.Log(v1);
      ht.hexagon.scale += h/(1+(stepHeight/2f));//1.5f;
      if(ht.generation == zeroState) //keep glyphs
      {
        int v = Mathf.Abs((int)ht.type + h + typeShift);
        int t = (v % 12) + 1; //using 12 types
        ht.type = (TileType)t;
      }
      
      float gP = Random.Range(0f,1.0f);
      if(glyphProb > gP)
      {
        byte[] newID = new byte[32];
        for(int b = 0; b < 32; b++)
        {
          newID[b] = (byte)Random.Range(0,256);
        }
        //Create new rune on world (locked in)
        Rune newRune = new Rune(newID);
        ht.generation = newRune.tile.uvy;
        ht.placeObject = false;
        newRune.hexTile = ht.index;
        runes.Add(newRune);
      } 
      
      float r = Random.Range(0,1.0f);
      if(r < populationProb && ht.generation == zeroState)// populationProb)// && ht.generation == zeroState)
      {
        //populate, unless too many neighbors are populated
        int neighborPopulation = 0;
        foreach(int htn in ht.neighbors)
        {
          if(tiles[htn].placeObject)
          {
            neighborPopulation++;
          }
        }
        if(neighborPopulation < 3)
        {
          ht.placeObject = true;
          //ht.passable = false;
        }
      }
    }
  }
  public void ReadState()
  {
    //state of tiletypes
    state = new int[tiles.Count];
    for (int i = 0; i < tiles.Count; i++)
    {
      state[i] = (int)tiles[i].type;
    }
  }

  public void SetState(int[] st)
  {
    state = st;
    foreach(HexTile ht in tiles)
    {
      ht.ChangeType((TileType)state[ht.index]);
    }
  }

  public void Clear()
  {
    foreach(HexTile ht in tiles)
    {
      if(ht.type != TileType.Gray){ht.ChangeType(TileType.Gray);}
      ht.antPasses = 0;
      ht.generation = 0;
    }
  }
  
/*
  public void Imbue(int[] glyph, HexTile origin)
  {

  }
*/
/*  an imprecise but working solution
public List<int> GetTilesInRadius(float radius, int origin)
{
  List<int> output = new List<int>();
  foreach(HexTile ht in tiles)
  {
    if((ht.hexagon.center - tiles[origin].hexagon.center).magnitude <= radius)
    {
      output.Add(ht.index);
    }
  }
  return output;
}*/

  public List<int> GetTilesInRadius(int radius, int origin)
  {
    List<int> returnedTiles = new List<int>();
    List<int> tilesToAdd = new List<int>();
    returnedTiles.Add(origin);
    for(int r = 0; r < radius; r++)
    {
      foreach(int t in returnedTiles)
      {
        foreach(int n in tiles[t].neighbors)
        {
          tilesToAdd.Add(n);
        }
      }
      foreach(int a in tilesToAdd)
      {
        returnedTiles.Add(a);
      }
      returnedTiles = returnedTiles.Distinct().ToList();
    }
    return returnedTiles;
  }
  
  public int TileDistanceFromTo(int from, int to)
  { 
    //Just brute force it with GetTilesInRadius, expensive but simple solution
    //max radius 10
    for(int r = 0; r <= 10; r++)
    {
      List<int> t = GetTilesInRadius(r,from);
      foreach(int i in t)
      {
        if(to == i)
        {
          return r;
        }
      }
    }
    Debug.Log("Couldn't find distance between " + from + " and " + to);
    return 10;
  }
  
  public float AverageTileHeight()
  {
    float h = 0;
    foreach(HexTile ht in tiles)
    {
      h += ht.hexagon.scale;
    }
    h /= tiles.Count;
    return h;
  }

  public void Generate(float scale, int subdivisions)
  {
    if (tiles == null || tiles.Count == 0)
    {
      neighborInit = false;
      activeSphere = new PolySphere(Vector3.zero, scale, subdivisions);
      //make the tileToPlate dict
      numberOfPlates = activeSphere.numberOfPlates;
      //tileToPlate = new Dictionary<int, int>();
      // triTiles = activeSphere.triTiles;
      tiles = activeSphere.hexTiles;
    }
    else
      Debug.Log("tiles not null during cache prep");
  }

  public void LightToDark()
  {
    foreach(HexTile ht in tiles)
    {
      if(ht.type == TileType.Light){ht.type = TileType.Dark;}
      if(ht.type == TileType.Metal){ht.type = TileType.Ice;}
      if(ht.type == TileType.Fire){ht.type = TileType.Water;}
      if(ht.type == TileType.Astral){ht.type = TileType.Arbor;}
      if(ht.type == TileType.Air){ht.type = TileType.Earth;}
      if(ht.type == TileType.Crystal){ht.type = TileType.Vapor;}
      if(ht.type == TileType.Solar){ht.type = TileType.Lunar;}
    }
  }
  public void DarkToLight()
  {
    foreach(HexTile ht in tiles)
     {
      if(ht.type == TileType.Dark){ht.type = TileType.Light;}
      if(ht.type == TileType.Ice){ht.type = TileType.Metal;}
      if(ht.type == TileType.Water){ht.type = TileType.Fire;}
      if(ht.type == TileType.Arbor){ht.type = TileType.Astral;}
      if(ht.type == TileType.Earth){ht.type = TileType.Air;}
      if(ht.type == TileType.Vapor){ht.type = TileType.Crystal;}
      if(ht.type == TileType.Lunar){ht.type = TileType.Solar;}
     }
  }   
}
   
