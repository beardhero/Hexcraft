using UnityEngine;
using UnityEngine.UI;

using System;
using System.Collections.Generic;

using System.Threading;
using System.Threading.Tasks;

using Colyseus;
using Colyseus.Schema;

using GameDevWare.Serialization;

[Serializable]
class Metadata
{
	public string str;
	public int number;
}

[Serializable]
class CustomRoomAvailable : RoomAvailable
{
	public Metadata metadata;
}

class CustomData
{
	public int integer;
	public string str;
}

public class TypeMessage
{
	public string msgType, contents;
	public TypeMessage(){}
	public TypeMessage(string t, string cont){
		msgType = t;
		contents = cont;
	}
}

enum MessageType {
	ONE = 0
};
class MessageByEnum
{
	public string str;
}

public class ColyseusClient : MonoBehaviour {
	public string roomName = "demo";	// These room names correspond to those defined in Server/rooms

	protected Client client;
	protected Room<State> room;

	protected Room<IndexedDictionary<string, object>> roomFossilDelta;
	protected Room<object> roomNoneSerializer;

	protected IndexedDictionary<Entity, GameObject> entities = new IndexedDictionary<Entity, GameObject>();


	public void SendTypeMessage(TypeMessage msg)
	{
		if (room != null)
		{
			// something was here about schema
			room.Send("TypeMessage", msg);		// Chat is the type
		}
		else
		{
			Debug.Log("Room is not connected!");
		}
	}

	public void RegisterRoomHandlers()
	{
		Debug.Log("Starting multi session: " + room.SessionId);

		room.State.entities.OnAdd += OnEntityAdd;
		room.State.entities.OnRemove += OnEntityRemove;
		room.State.TriggerAll();

		PlayerPrefs.SetString("roomId", room.Id);
		PlayerPrefs.SetString("sessionId", room.SessionId);
		PlayerPrefs.Save();

		room.OnLeave += (code) => Debug.Log("ROOM: ON LEAVE");
		room.OnError += (code, message) => Debug.LogError("ERROR, code =>" + code + ", message => " + message);
		room.OnStateChange += OnStateChangeHandler;

		room.OnMessage<TypeMessage>("TypeMessage", (message) =>
		{
			GameManager.networkManager.OnTypeMessage(message);
		});

		room.OnMessage((Message message) =>
		{
			Debug.Log("Received Schema message:");
			Debug.Log(message.num + ", " + message.str);
		});

		room.OnMessage<MessageByEnum>((byte) MessageType.ONE, (message) =>
		{
			Debug.Log(">> Received message by enum/number => " + message.str);
		});

		_ = room.Send((byte)MessageType.ONE, new MessageByEnum { str = "Sending message by enum/number" });

		Debug.Log("--- ROOM INITIALIZED ---");
	}

	public async void ConnectToServer (string endpoint)
	{
		Debug.Log("Connecting to " + endpoint);
		/*
		 * Connect into Colyeus Server
		 */
		client = ColyseusManager.Instance.CreateClient(endpoint);

		await client.Auth.Login();

		//var friends = await client.Auth.GetFriends();

		// Update username
		client.Auth.Username = "Jake";
		await client.Auth.Save();
		JoinOrCreateRoom();
	}

	public async void CreateRoom()
	{
		room = await client.Create<State>(roomName, new Dictionary<string, object>() { });
		roomNoneSerializer = await client.Create("no_state", new Dictionary<string, object>() { });
		roomFossilDelta = await client.Create<IndexedDictionary<string, object>>("fossildelta", new Dictionary<string, object>() { });
		RegisterRoomHandlers();
	}

	public async void JoinOrCreateRoom()
	{
		room = await client.JoinOrCreate<State>(roomName, new Dictionary<string, object>() { });
		RegisterRoomHandlers();
		Debug.Log("finished join or create");
	}

	public async void JoinRoom ()
	{
		room = await client.Join<State>(roomName, new Dictionary<string, object>() { });
		RegisterRoomHandlers();
	}

	async void ReconnectRoom ()
	{
		string roomId = PlayerPrefs.GetString("roomId");
		string sessionId = PlayerPrefs.GetString("sessionId");
		if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(roomId))
		{
			Debug.Log("Cannot Reconnect without having a roomId and sessionId");
			return;
		}

		room = await client.Reconnect<State>(roomId, sessionId);

		Debug.Log("Reconnected into room successfully.");
		RegisterRoomHandlers();
	}


	public async void LeaveRoom()
	{
		await room.Leave(false);

		// Destroy player entities
		foreach (KeyValuePair<Entity, GameObject> entry in entities)
		{
			Destroy(entry.Value);
		}

		entities.Clear();
	}

	async void GetAvailableRooms()
	{
		var roomsAvailable = await client.GetAvailableRooms<CustomRoomAvailable>(roomName);

		Debug.Log("Available rooms (" + roomsAvailable.Length + ")");
		for (var i = 0; i < roomsAvailable.Length; i++)
		{
			Debug.Log("roomId: " + roomsAvailable[i].roomId);
			Debug.Log("maxClients: " + roomsAvailable[i].maxClients);
			Debug.Log("clients: " + roomsAvailable[i].clients);
			Debug.Log("metadata.str: " + roomsAvailable[i].metadata.str);
			Debug.Log("metadata.number: " + roomsAvailable[i].metadata.number);
		}
	}

	void OnStateChangeHandler (State state, bool isFirstState)
	{
		// Setup room first state
		Debug.Log("State has been updated!");
	}

	void OnEntityAdd(Entity entity, string key)
	{
		GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

		Debug.Log("Player add! x => " + entity.x + ", y => " + entity.y);

		cube.transform.position = new Vector3(entity.x, entity.y, 0);

		// Add "player" to map of players
		entities.Add(entity, cube);

		// On entity update...
		entity.OnChange += (List<Colyseus.Schema.DataChange> changes) =>
		{
			cube.transform.Translate(new Vector3(entity.x, entity.y, 0));
		};
	}

	public void OnEntityRemove(Entity entity, string key)
	{
		GameObject cube;
		entities.TryGetValue(entity, out cube);
		Destroy(cube);

		entities.Remove(entity);
	}

}
