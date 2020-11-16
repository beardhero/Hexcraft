using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;

public enum UnitType{   // This must exactly match unit.ts on the server
  None, Player
}

[FirestoreData]
public class ServerUnit
{
    [FirestoreDocumentId] public string id { get; set; }
    [FirestoreProperty("location")] public int position { get; set; }
    [FirestoreProperty] public string owner { get; set; }
    [FirestoreProperty] public UnitType type { get; set; }
}

// This custom class is for any unit comparison
public class UnitList : List<ServerUnit>{

  public ServerUnit playerUnit;

  public new void Add(ServerUnit u){
    base.Add(u);
    if (u.type == UnitType.Player){
      if (playerUnit == null)
        playerUnit = u;
      else
        Debug.LogError("Got another player unit added to this unit list");
    }
  }
  public new bool Contains(ServerUnit toCheck){
    foreach (ServerUnit u in this){
      Debug.Log("Comparing "+u.id+"with "+toCheck.id);
      if (u.id == toCheck.id)
        return true;
    }

    return false;
  }
}