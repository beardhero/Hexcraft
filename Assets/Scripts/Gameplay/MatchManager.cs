using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;

public class MatchManager : MonoBehaviour
{
    public static NetworkClient networkClient;
    bool matchInitialized;
    Match currentMatch;
    string playerID;        // Local player ID
    bool worldLoaded, firstData;        // Gets set 

    // ----------------
    Transform playerTrans;
    World activeWorld;
    Dictionary<string,UnitList> units;

    public void Initialize(string id, Match m, NetworkClient client){
        playerID = id;
        currentMatch = m;
        networkClient = client;
        playerTrans = GameObject.FindWithTag("Player").transform;
        units = new Dictionary<string, UnitList>();
    }

    public void DoStartMatch(){
        playerTrans.position = activeWorld.GetPositionOfTile(units[playerID].playerUnit.position);
        playerTrans.rotation = Quaternion.LookRotation( Vector3.Cross(playerTrans.position, activeWorld.origin),
          playerTrans.position - activeWorld.origin.ToVector3() );
    }

    public void OnMatchUpdated(DocumentSnapshot snap){
        Debug.Log("updating match");
    }

    public void OnWorldLoaded(World world){
    Debug.Log("World loaded");
        worldLoaded = true;
        activeWorld = world;
        if (firstData){
            // At this point the world is loaded and now we ask the server to start the match
            networkClient.ReadyToStartMatch();
        }
    }

    void OnFirstDataReceived(){
        firstData = true;
        Debug.Log("first data received"); 
        if (worldLoaded){
            // At this point the world is loaded and now we ask the server to start the match
            networkClient.ReadyToStartMatch();
        } 
    }

    public void OnUnitsUpdated(QuerySnapshot snap){
        foreach (DocumentChange change in snap.GetChanges())
        {
            if (change.ChangeType == DocumentChange.Type.Added)
            {
                ServerUnit u = change.Document.ConvertTo<ServerUnit>();

                // Add a list if it's for a player that's not yet in the units
                if (!units.ContainsKey(u.owner)) units[u.owner] = new UnitList();

                // Add unit to units
                if (!units[u.owner].Contains(u)){
                    units[u.owner].Add(u);
                    // @TODO: initialize unit, merge with check if player below
                }
                else{
                    Debug.LogError("Getting a ChangeType.Added for a unit with already existant ID.");
                }

                // Check if it's a player unit placement
                if (u.type == UnitType.Player){
                    if (u.owner == playerID){   // Initial setup/placement of player
                        if (!firstData) OnFirstDataReceived();

                    }
                    else{   // New player joined match
                    }
                }
            }
            else if (change.ChangeType == DocumentChange.Type.Modified)
            {
                
            }
            else if (change.ChangeType == DocumentChange.Type.Removed)
            {
                
            }
        }
    }
}
