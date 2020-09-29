using UnityEngine;
using Mirror;

public class Jfc : NetworkBehaviour
{
    public GameObject treePrefab;

    // Register prefab and connect to the server  
    public void ClientConnect()
    {
        ClientScene.RegisterPrefab(treePrefab);
        NetworkClient.RegisterHandler<ConnectMessage>(OnClientConnect);
        NetworkClient.Connect("localhost");
    }

    void OnClientConnect(NetworkConnection conn, ConnectMessage msg)
    {
        Debug.Log("Connected to server: " + conn);
    }
    public void ServerListen()
    {
        NetworkServer.RegisterHandler<ConnectMessage>(OnServerConnect);
        NetworkServer.RegisterHandler<ReadyMessage>(OnClientReady);

        // start listening, and allow up to 4 connections
        NetworkServer.Listen(4);
    }

    // When client is ready spawn a few trees  
    void OnClientReady(NetworkConnection conn, ReadyMessage msg)
    {
        Debug.Log("Client is ready to start: " + conn);
        NetworkServer.SetClientReady(conn);
        SpawnTrees();
    }

    void SpawnTrees()
    {
        Debug.Log("spawntrees");
        int x = 0;
        for (int i = 0; i < 5; ++i)
        {
            GameObject treeGo = Instantiate(treePrefab, new Vector3(x++, 0, 0), Quaternion.identity);
            NetworkServer.Spawn(treeGo);
        }
    }

    void OnServerConnect(NetworkConnection conn, ConnectMessage msg)
    {
        Debug.Log("New client connected: " + conn);
    }
}
