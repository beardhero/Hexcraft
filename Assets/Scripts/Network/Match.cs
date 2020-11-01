using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Match
{
    public string id;       // Note we don't ever set the ID, as it matches what firestore assigns
    
    public string type;
    public int maxPlayers;
    public string name;
    public int players;
    public Match(){}
    public Match(string id, string name, int players, int maxPlayers, string type)
    {
        this.name = name;
        this.id = id;
        this.players = players;
        this.maxPlayers = maxPlayers;
        this.type = type;
    }
    public Match(string id, Dictionary<string, object> dic){
        this.id = id;
        name = dic.ContainsKey("name") ? (string)dic["name"] : "?";
        players = dic.ContainsKey("players") ? (int)((long)dic["players"]) : -1;
        maxPlayers = dic.ContainsKey("maxPlayers") ? (int)((long)dic["maxPlayers"]) : -1;
        type = dic.ContainsKey("type") ? (string)dic["type"] : "?";
    }
}
