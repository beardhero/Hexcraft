using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;

// A set of HexWorld user data (not to be confused with firebaseAuth.user)
public class User
{
    public string displayName, email;

    public User(){}
    public User(DocumentSnapshot snap){
        if (!snap.TryGetValue<string>("DisplayName", out this.displayName))  // Writes result to this.displayName
            Debug.LogError("Corrupted user data");
        if (!snap.TryGetValue<string>("Email", out this.email))  // Writes result to this.displayName
            Debug.LogError("Corrupted user data");
    }
}
