using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using System;

[System.Serializable][FirestoreData]
public class Match
{
    [FirestoreDocumentId] public string id { get; set; }
    [FirestoreProperty("worldID")] public string worldID { get; set; }
    [FirestoreProperty] public string name { get; set; }
    [FirestoreProperty] public Dictionary<string, object>[] players { get; set; }
    [FirestoreProperty] public int roundLength {get;set;}
    [FirestoreProperty] public Timestamp lastRound {get;set;}
    [FirestoreProperty] public Dictionary<string, object> units {get;set;}
}
