using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;

// This class is similar to CacheTile but contains the extra data sent back from the server
//  and does not contain data that can be instead extracted from Baseworld
[System.Serializable][FirestoreData]
public class ServerTile
{
    [FirestoreProperty] public int i { get; set; }      // index
    [FirestoreProperty] public float h { get; set; }        // height
    [FirestoreProperty] public bool p { get; set; }  // passable
    [FirestoreProperty] public TileType t { get; set; }  // type
}
