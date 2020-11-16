using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Unity.Editor;
using Firebase.RemoteConfig;
using Firebase.Functions;
using Firebase.Firestore;
using Firebase.Auth;
using Newtonsoft.Json;

public class NetworkClient : MonoBehaviour
{
    public bool runOfflineWithEmulation;
    public static FirebaseUser user;
    public static User userData;
    public static FirebaseApp firebaseApp;
    public static FirebaseFunctions functions;
    public static FirebaseFirestore db;
    public static FirebaseAuth auth;
    public static MainUI mainUI;
    public static MatchManager matchManager;
    bool firebaseInitialized;
    List<ListenerRegistration> matchListeners;
    
    // Initialize() is at bottom of file

    // ===== Round ======
    public void ReadyToStartMatch(){  // Finally everything is ready
        matchManager.DoStartMatch();
        // set readyCheck flag
        return;
    }

    // ===== Lobby =====
    public async Task<List<Match>> GetMatchList()
    {
        List<Match> matches = new List<Match>();

        Query q = db.Collection("matches").OrderByDescending("players").Limit(10);
        QuerySnapshot snapSht = await q.GetSnapshotAsync();

        foreach (DocumentSnapshot doc in snapSht.Documents)
        {
            //Dictionary<string, object> dic = doc.ToDictionary();
            matches.Add(doc.ConvertTo<Match>());
        }

        return matches;
    }

    public void OnJoinMatch (in Match match){ JoinMatch(match); }
    async void JoinMatch(Match match)
    {
        Debug.Log("joining match: "+match.name+" with id "+match.id);
        // First ask the server initialize us (give us inventory, place us on the world, etc.)
        HttpsCallableResult res;
        res = await functions.GetHttpsCallable("joinMatch").CallAsync(new string[]{match.id, match.worldID});
        // @TODO: check res to make sure all is well

        matchManager.Initialize(user.UserId, match, this);

        matchListeners = new List<ListenerRegistration>();
        // Listen to changes in this match
        matchListeners.Add(db.Collection("matches").Document(match.id).Listen( snap => matchManager.OnMatchUpdated(snap) ));
        
        // Get initial Units collection
        CollectionReference unitsCol = db.Collection("matches").Document(match.id).Collection("units");
        // QuerySnapshot unitsSnap = await unitsCol.GetSnapshotAsync();
        // matchManager.OnUnitsUpdated(unitsSnap); 

        // Listen to changes in the Units collection in this match
        matchListeners.Add(unitsCol.Listen(snap=>matchManager.OnUnitsUpdated(snap)));

        GameManager.OnMatchJoin(match);
    }

    public void OnLeaveMatch()
    {
        foreach (ListenerRegistration l in matchListeners)
            l.Stop();
    }

    public async void StartNewMatch(string matchName)
    {
        Debug.Log("asking server for a new match called "+matchName);
        HttpsCallableReference join = functions.GetHttpsCallable("startNewMatch");
        HttpsCallableResult res;

        try{
            res = await join.CallAsync(matchName);  // This returns the Firestore ID of the match
        }
        catch (System.Exception error){
            Debug.LogError(error);
            return;
        }

        JoinMatch(JsonConvert.DeserializeObject<Match>((string)res.Data));
    }

    public static async void TestGenerateWorld(){
        Debug.Log("Generating test world (no matchmaking).");
        HttpsCallableReference generate = functions.GetHttpsCallable("testGenerateWorld");
        HttpsCallableResult res = await generate.CallAsync("testWorld");
        ServerWorld world = JsonConvert.DeserializeObject<ServerWorld>((string)res.Data);
        // JSONSerializer.WriteTextFile(world, "\\Cache\\server_generatedWorld.json");  // If we want to see the output
        GameManager.InitalizeServerWorld(world);
    }

    public static async Task<ServerWorld> GetWorldFromServerByID(string id){
        DocumentSnapshot snap = await db.Collection("worlds").Document(id).GetSnapshotAsync();

        if (!snap.Exists){
            Debug.LogError("Could not fetch world matching Match.worldID: "+id);
            return null;
        }

        return snap.ConvertTo<ServerWorld>();
    }

    // ===== User Account ====
    public static async Task<bool> Login(string email, string password)
    {
        Credential cred = EmailAuthProvider.GetCredential(email, password);

        return await auth.SignInWithCredentialAsync(cred)
          .ContinueWith(new Func<Task<FirebaseUser>,bool>((task) => {
            if (task.IsCanceled) {
                Debug.LogError("SignInWithcredentials was canceled.");
                return false;
            }
            if (task.IsFaulted) {
                Debug.LogError("Bad Login error: " + task.Exception);
                return false;
            }

            //Debug.LogFormat("User signed in successfully: {0} ({1})", task.Result.DisplayName, task.Result.UserId);
            return true;
        }));
    }
    public static async Task<string> BeginRegistration(string email, string password, string displayName)
    {
        // Input validation is done in MainUI
        Query query = db.Collection("users").WhereEqualTo("DisplayName", displayName).Limit(1);
        QuerySnapshot response = await query.GetSnapshotAsync();
        if (response.Count > 0)
            return "name taken";

        return await auth.CreateUserWithEmailAndPasswordAsync(email, password)
          .ContinueWith((async (task) => {
            if (task.IsFaulted) {
                int errorCode = (task.Exception.Flatten().InnerException as Firebase.FirebaseException).ErrorCode;
                switch (errorCode){
                    case 8: return "email in use";
                    default:
                        Debug.LogError(task.Exception);
                        return "error";
                }
            }
            else{
                UserProfile profile = new UserProfile();
                profile.DisplayName = displayName;
                await task.Result.UpdateUserProfileAsync(profile);
                // Firebase user has been created.
                await task.Result.SendEmailVerificationAsync();
                await db.Collection("users").Document(email).SetAsync(new {       // Creates a new doc with email as the id
                    DisplayName = displayName, Email = email, Password = password
                });

                return "success";
            }
        })).Result;
    }

    public static async Task<User> GetUserWithDisplayName(string name, string password){
        CollectionReference usersRef = db.Collection("users");
        // This might not be secure if someone can intercept and decrypt packets with the whole user data...
        //  ...might need to execute this in a cloud function
        Query query = usersRef.WhereEqualTo("DisplayName", name).WhereEqualTo("Password", password).Limit(1);
        QuerySnapshot response = await query.GetSnapshotAsync();
        if (response.Count < 1)
            return null;        // Invalid login

        return new User(response[0]);
    }
    
    // ======= Initialization =======
    public void Initialize(Action callback = null){StartCoroutine(_Initialize(callback));}
    IEnumerator _Initialize(Action callback = null)
    {
        mainUI = GameObject.FindWithTag("MainUI").GetComponent<MainUI>();
        matchManager = GameObject.FindWithTag("GameController").GetComponent<MatchManager>();

        InitializeFirebase();
        while (!firebaseInitialized)
            yield return null;

        FinishInitialization(callback);
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
      }).ContinueWith(task => {
        // Firebase
        firebaseApp = FirebaseApp.DefaultInstance;
        // Functions
        functions = FirebaseFunctions.DefaultInstance;
        // Firestore
        db = FirebaseFirestore.DefaultInstance;
        db.Settings.PersistenceEnabled = true;      // Offline cache
        auth = FirebaseAuth.GetAuth(firebaseApp);
        firebaseInitialized = true;
      });
    }

    Task InitializeRemoteConfig()
    {    
        Dictionary<string, object> defaults = new Dictionary<string, object>();
        defaults.Add("worldHeight", 128);

        FirebaseRemoteConfig.SetDefaults(defaults);
        // If our last config is older than the timespan specified here, a new config will be fetched
        return FirebaseRemoteConfig.FetchAsync(System.TimeSpan.Zero); 
    }

    void FinishInitialization(Action callback = null)
    {
        bool newConfiguration = !FirebaseRemoteConfig.ActivateFetched();
        Config.worldHeight = (int)FirebaseRemoteConfig.GetValue("worldHeight").LongValue;

        if (runOfflineWithEmulation){
            FirebaseFunctions.DefaultInstance.UseFunctionsEmulator("http://localhost:5001");
            // We can't do this as there's no unity firestore emulator support yet
            //db.useEmulator();
            //await functions.GetHttpsCallable("loadTestData").CallAsync();
        }

        // Register hooks
        auth.StateChanged += AuthStateChanged;

        Debug.Log("Network client initialized");
        callback();
    }

    void AuthStateChanged(object sender, System.EventArgs eventArgs) {
        if (auth.CurrentUser != user) {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
            if (!signedIn && user != null) {
                Debug.Log("Signed out " + user.UserId);
                // @TODO: disable game functions
            }
            user = auth.CurrentUser;
            if (signedIn) {
                Debug.Log("Signed in " + user.DisplayName);
                mainUI.MainLogin();     // Refresh the main login
                // @TODO: enable game functions
                // @TODO: set this.userData from firestore query
            }
        }
    }
    public static bool IsLoggedIn{
        get{return auth.CurrentUser != null && auth.CurrentUser == user;}
        set{}
    }
}
