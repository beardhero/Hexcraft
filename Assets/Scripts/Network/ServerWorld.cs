using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// An interface used to deserialize the world representation received form the HexServer
[System.Serializable]
public class ServerWorld
{
    public string name;
    public float oceanLevel;
    public float maxHeight;
    public List<ServerTile> tiles;
}
