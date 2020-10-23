using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Unity.Editor;
using Firebase.RemoteConfig;
using Firebase.Functions;

public class NetworkClient : MonoBehaviour
{
    public static FirebaseApp firebase;
    public static FirebaseFunctions functions;
    bool firebaseInitialized;
    // Start is called before the first frame update

    public void Initialize(){StartCoroutine(_Initialize());}
    IEnumerator _Initialize()
    {
        Debug.Log("iniitalizing network client");
        InitializeFirebase();
        while (!firebaseInitialized)
            yield return null;

        FinishInitialization();
        JoinFirstAvailableWorld();
    }

    void InitializeFirebase()
    {
        Firebase.DependencyStatus dependencyStatus = Firebase.FirebaseApp.CheckDependencies();

        if (dependencyStatus != Firebase.DependencyStatus.Available) {
            Firebase.FirebaseApp.FixDependenciesAsync().ContinueWith(task => {
            dependencyStatus = Firebase.FirebaseApp.CheckDependencies();
            if (dependencyStatus == Firebase.DependencyStatus.Available) {
                InitializeFirebaseComponents();
            } else {
                Debug.LogError(
                    "Could not resolve all Firebase dependencies: " + dependencyStatus);
                Application.Quit();
            }
            });
        }
        else {
            InitializeFirebaseComponents();
      }
    }

    void InitializeFirebaseComponents() {
      System.Threading.Tasks.Task.WhenAll(new Task[]{
        InitializeRemoteConfig()
        //InitializeCloudFunctions()
      }).ContinueWith(task => { firebaseInitialized = true; });
    }

    Task InitializeRemoteConfig()
    {    
        Dictionary<string, object> defaults = new Dictionary<string, object>();
        defaults.Add("worldHeight", 128);

        FirebaseRemoteConfig.SetDefaults(defaults);
        // If our last config is older than the timespan specified here, a new config will be fetched
        return FirebaseRemoteConfig.FetchAsync(System.TimeSpan.Zero); 
    }

    void FinishInitialization()
    {
        Debug.Log("firebase components initialized");
        firebase = FirebaseApp.DefaultInstance;
        functions = FirebaseFunctions.DefaultInstance;

        bool newConfiguration = !FirebaseRemoteConfig.ActivateFetched();
        Config.worldHeight = (int)FirebaseRemoteConfig.GetValue("worldHeight").LongValue;

        // @TODO: change from using emulator to using server
        FirebaseFunctions.DefaultInstance.UseFunctionsEmulator("http://localhost:5001");    // /hexworld-293023/us-central1
    }

    async void JoinFirstAvailableWorld()
    {
        HttpsCallableReference join = functions.GetHttpsCallable("getFirstAvailableGameWorld");
        HttpsCallableResult res;

        try{
            res = await join.CallAsync();
        }
        catch (System.Exception error){
            Debug.LogError(error);
            return;
        }

        List<ServerTile> tiles = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ServerTile>>((string)res.Data);
        GameManager.IniitalizeServerWorld(tiles);
    }
}
