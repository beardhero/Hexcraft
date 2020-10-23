using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System.IO;
using Newtonsoft.Json;

public enum RelativityState {None, CacheBaseworld, Lobby, Caching, MainMenu, WorldMap, ZoneMap, WorldDuel};

public class GameManager : MonoBehaviour
{
  // === Const & Inspector Cache ===
  public RelativityState beginningState = RelativityState.WorldMap;
  public string gameSeed = "doesthisneedtobemorethaneightchars";

  // === Static Cache ===
  public static GameManager instance;
  static RelativityState state;
  public static Transform myTrans;
  public static RelativityState State {get{return state;} set{}}
  public static Camera cam;
  public static MainUI mainUI;

  //For World
  public static World currentWorld;
  public static GameObject worldManagerObj;
  public static WorldManager worldManager;
  static CreateWorldCache worldCacher;

  public static List<GameObject> currentZoneObjects;
  public static ZoneViewCamera zoneCameraControls;

  //Zone
  public static ZoneManager zoneManager;
  public static ZoneRenderer zoneRenderer;
  public static Zone currentZone;

  // For combat
  public static GameObject combatManagerObj;
  public static CombatManager combatManager;
  public static RoundManager roundManager;
  public GameObject blockPrefab;

    private void Start() {
      instance = this;
            Init();
    }

    // *** Main Initializer ***
    void Init()
    {
        myTrans = transform;

        // @TODO: Make these a singleton pattern
        //currentZone = new Zone(1); // Required so Hex doesn't null ref currentZone
        Hex.Initialize();

        // Ideally, the only place state is manually set.
        state = beginningState;
        bool loading;
        switch (state)
        {
          // Note: all but first two states have been deprecated
          case RelativityState.Lobby:
            // @TODO: render some kind of character lobby or homeworld to show before going online.
          break;

          case RelativityState.CacheBaseworld:
            PerlinType.globalSeed = gameSeed;

            CacheOnly();

            Debug.Log("baseworld.json saved to Resources and tilemap.json saved to Cache");
            Application.Quit();
          break;

          case RelativityState.WorldDuel:
            loading = true;
            InitializeWorld(loading);

            InitializeCombat();
          break;

          case RelativityState.WorldMap:
            loading = true;
            InitializeWorld(loading);
          break;

          case RelativityState.ZoneMap:
            InitializeZone();
          break;

          case RelativityState.Caching:
           // Debug.Log("got to game manager caching");
            loading = false;
            PerlinType.globalSeed = gameSeed;
            InitializeWorld(loading);
            // When loading is false, world.initialize will .Generate and .Populate
            CreateWorldCache.BuildCache(WorldManager.activeWorld);
          break;

          default:
            Debug.LogError("Please set a state in GameManager.beginningState before playing.");
          break;
      }
  }

  void CacheOnly()
  {
    PolySphere sphere = new PolySphere(Vector3.zero, WorldManager.worldScale, WorldManager.worldSubdivisions);

    // Serialize Vertex Data to JSON, for clients to use in building geometry
    JsonSerializer serializer = new JsonSerializer();
    //serializer.Formatting = Formatting.Indented;  // indentation increases file size by 200%
    
    using (StreamWriter sw = new StreamWriter(Application.dataPath+"\\Resources\\baseworld.json"))   // Note DO NOT use any encoding options
    using (JsonWriter writer = new JsonTextWriter(sw))
    {
        serializer.Serialize(writer, sphere);
    }

    // Serialize index and neighbor data, as well as hexagon center, for server to use in generating worlds
    List<ServerTile> tiles = new List<ServerTile>();
    foreach (HexTile ht in sphere.hexTiles){
      tiles.Add(new ServerTile(ht));  // copy constructor
    }

    using (StreamWriter sw = new StreamWriter(Application.dataPath+"\\Cache\\tilemap.json"))
    using (JsonWriter writer = new JsonTextWriter(sw))
    {
        serializer.Serialize(writer, tiles);
    }

    

  }

  public static void IniitalizeServerWorld(List<ServerTile> tiles){
    worldManagerObj = GameObject.FindWithTag("World Manager");
    worldManager = worldManagerObj.GetComponent<WorldManager>();
    worldManager.InitializeServerWorld(instance.blockPrefab, tiles);
  }
  void InitializeWorld(bool loading)
  {
    worldManagerObj = GameObject.FindWithTag("World Manager");
    worldManager = worldManagerObj.GetComponent<WorldManager>();
    worldManager.Initialize(blockPrefab, loading);

    // Note that blockManager is initialized in WorldManager.Initialize
  }

  void InitializeCombat()
  {
    combatManagerObj = GameObject.FindWithTag("CombatManager");
    combatManager = combatManagerObj.GetComponent<CombatManager>();
    combatManager.Initialize(currentWorld);
  }
	
  void InitializeZone()
  {
    zoneManager = GameObject.FindWithTag("Zone Manager").GetComponent<ZoneManager>();
    zoneRenderer = zoneManager.GetComponent<ZoneRenderer>();

    // --- Input

    // --- Network

    // --- Zone
    if (currentZoneObjects != null && currentZoneObjects.Count > 0)
    {
      foreach (GameObject g in currentZoneObjects)
        Destroy (g);
    }
    int safety = 100;
    bool buildingZone = true;
    int minimumSize = 50;

    Triangle tri = new Triangle(new Vector3(0, 0, 0), new Vector3(18, 0, 24), new Vector3(0, 0, 36));

    while (buildingZone)
    {
      currentZone = new Zone(tri);

      if (currentZone.landArea > minimumSize)
      {
        Debug.Log("Zone generated with a land mass of "+currentZone.landArea+" hex.");
        buildingZone = false;
      }
      else if (currentZone.landArea>0)
      {
        Debug.Log("Land mass is too low. New level being generated....");
      }
      else
      {
        Debug.Log("Underwater level detected. New level being generated....");
      }

      safety--;
      if (safety < 0)
        break;
    }

    currentZoneObjects = zoneRenderer.RenderZone(currentZone, zoneManager.regularTileSet);
    //zoneManager.Initialize(currentZone);
    //CapturePNG();
  }
}