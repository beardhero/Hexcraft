using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LibNoise.Unity.Generator;
using System.Linq;
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

  public World(List<HexTile> baseTiles, List<ServerTile> serverTiles)
  {
    origin = Vector3.zero;
    tiles = new List<HexTile>();
    Debug.Log("Hooray! "+ baseTiles[0].hexagon.center.x + " ---- "+serverTiles[2].center);
  }

  public World(WorldSize s, WorldType t, Season se, AxisTilt at)
  {
    size = s;
    type = t;
    season = se;
    tilt = at;
    origin = Vector3.zero;
  }
  
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
      TileType currentType = TileType.Water;
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
        if (currentType > TileType.Light)
          currentType = TileType.Water;  // If we've reached the end of the type list, start again from Water @TODO: create a list of types to use rather than using all

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
      if(ht.type == TileType.Sol){ht.type = TileType.Luna;}
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
      if(ht.type == TileType.Luna){ht.type = TileType.Sol;}
     }
  }   
}
   
