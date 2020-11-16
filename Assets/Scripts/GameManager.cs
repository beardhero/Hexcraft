using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System.Threading.Tasks;

public enum RelativityState {None, CacheBaseworld, Lobby, Caching, MainMenu, WorldMap, ZoneMap, WorldDuel};

public class GameManager : MonoBehaviour
{
  // === Const & Inspector Cache ===
  public RelativityState beginningState = RelativityState.WorldMap;
  public string gameSeed = "doesthisneedtobemorethaneightchars";

  // === Static Cache ===
  public static GameManager instance;
  public static MatchManager matchManager;
  static RelativityState state;
  public static Transform myTrans;
  public static RelativityState State {get{return state;} set{}}
  public static Camera cam;
  public static MainUI mainUI;
  public static NetworkClient networkClient;
  public static CameraController cameraController;
  public static FirebasePlayerController playerController;

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
        Hex.Initialize();   // @TODO: examine current necessity

        mainUI = GameObject.FindWithTag("MainUI").GetComponent<MainUI>();
        networkClient = GetComponent<NetworkClient>();
        cameraController = GameObject.FindWithTag("CameraController").GetComponent<CameraController>();
        playerController = GameObject.FindWithTag("Player").GetComponent<FirebasePlayerController>();
        matchManager = GetComponent<MatchManager>();

        // Ideally, the only place state is manually set.
        state = beginningState;
        bool loading;
        switch (state)
        {
          // Note: all but first two states have been deprecated
          case RelativityState.Lobby:
            // @TODO: render some kind of character lobby or homeworld to show before joining match.
            networkClient.Initialize(() => {
              mainUI.Initialize();
              mainUI.OnClickedRefresh();  // Call after networkClient is finished initializing
            });
          break;

          case RelativityState.CacheBaseworld:
            PerlinType.globalSeed = gameSeed;

            CacheOnly();
            #if UNITY_EDITOR
              UnityEditor.EditorApplication.isPlaying = false;
            #else
              Application.Quit();
            #endif
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
    JSONSerializer.WriteTextFile(sphere, "\\Resources\\baseworld.json");

    // Serialize index and neighbor data, as well as hexagon center, for server to use in generating worlds
    List<CacheTile> tiles = new List<CacheTile>();
    foreach (HexTile ht in sphere.hexTiles){
      tiles.Add(new CacheTile(ht));  // copy constructor
    }

    JSONSerializer.WriteTextFile(tiles, "\\Cache\\server_baseworld.json");
    Debug.Log("PolySphere written to Resources\\baseworld.json and List<CacheTile> written to Cache\\server_baseworld.json");
  }

  // This is called when match listener gets a match from firestore for the first time
  public static async void OnMatchJoin(Match match){
    ServerWorld serverWorld = await NetworkClient.GetWorldFromServerByID(match.worldID);
    float starttime = Time.time;
    InitalizeServerWorld(serverWorld, (w=>{
      mainUI.OnLeaveLobby();
      matchManager.OnWorldLoaded(w);
      float endtime = Time.time; 
      Debug.Log("Generated world in "+(endtime-starttime)+" seconds.");
    }));
  }

  public static void InitalizeServerWorld(ServerWorld world, Action<World> callback=null){
    worldManagerObj = GameObject.FindWithTag("World Manager");
    worldManager = worldManagerObj.GetComponent<WorldManager>();
    worldManager.StartCoroutine(worldManager.InitializeServerWorld(instance.blockPrefab, world, callback));
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