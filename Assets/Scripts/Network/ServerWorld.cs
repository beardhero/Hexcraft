using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;

// An interface used to deserialize the world representation received form the HexServer
[System.Serializable][FirestoreData]
public class ServerWorld
{
    [FirestoreDocumentId] public string id { get; set; }
    [FirestoreProperty] public string name { get; set; }
    [FirestoreProperty] public float oceanLevel { get; set; }
    [FirestoreProperty] public float maxHeight { get; set; }
    [FirestoreProperty] public ServerTile[] tiles { get; set; }
}
