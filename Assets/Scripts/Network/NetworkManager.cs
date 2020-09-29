using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;

public class NetworkManager : MonoBehaviour
{
    private System.Diagnostics.Process mongoProcess = null, colyProcess = null, mongoKillProcess = null;
    bool isRunningMongo = false, isRunningColy = false;
	bool isServerHost;
	ColyseusClient client;
	MainUI ui;

	void Start(){
		client = GetComponent<ColyseusClient>();
		ui = GameObject.Find("MainUI").GetComponent<MainUI>();
	}

	public void SendChat(string text){
		client.SendTypeMessage(new TypeMessage("chat", text));
	}

	public void OnTypeMessage(TypeMessage msg){
		switch (msg.msgType){
			case "chat":
				ui.OnChatReceived(msg.contents);
			break;
		}
	}

    public async Task<bool> OnHostServer(string endpoint) 
	{
		Debug.Log("launching server");
        LaunchMongoAndColyseus();
		isServerHost = true;
		await Task.Delay(1500);
		Debug.Log("Joining the server");
		// @TODO: verify textObject.text is a valid url it needs http:// and a trailing /
		client.ConnectToServer(endpoint);

		return true;
    }

    public void OnJoinServer(string endpoint) 
    {
        // @TODO: verify textObject.text is a valid url
		client.ConnectToServer(endpoint);
    }

	public void OnDisconnect(){
		client.LeaveRoom();
	}

	// =========== Heavy Lifting ===============================
	bool windowless = true;		// False is used for easier debugging.
	bool mongoOutput = true, colyOutput = true;
    void LaunchMongoAndColyseus(){
        if (isRunningMongo)
        {
            Debug.LogError("Already running Mongo");
        }
		else
		{
			// === Mongo ===
			string dbPath = Path.Combine(Application.persistentDataPath, "database", "test");
			Directory.CreateDirectory (dbPath);     // Does nothing if already exists
			string[] mongoPathParts = {Application.streamingAssetsPath, "Server", "MongoDB", "Server", "4.0", "bin", "mongod.exe"};
			string mongoBinPath = Path.Combine(mongoPathParts);      // afaik Combine() can only take 4 strings or else an array

			mongoProcess = new System.Diagnostics.Process();
			mongoProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			mongoProcess.StartInfo.FileName = mongoBinPath;
			mongoProcess.StartInfo.Arguments = "--dbpath=\""+dbPath+"\""; // Note the importance of encapsulating the dbPath in quotes in order to preserve spaces in filenames and folders
			mongoProcess.StartInfo.UseShellExecute = false;
			mongoProcess.EnableRaisingEvents = true;
			mongoProcess.Exited += OnExitMongo;
			if (windowless)
			{
				mongoProcess.StartInfo.CreateNoWindow = true;
				mongoProcess.StartInfo.RedirectStandardOutput = true;
				mongoProcess.StartInfo.RedirectStandardError = true;
				mongoProcess.OutputDataReceived += MongoOnOutputData;
				mongoProcess.ErrorDataReceived += MongoOnOutputData;
			}
			mongoProcess.Start();
			mongoProcess.BeginOutputReadLine();
			mongoProcess.BeginErrorReadLine();
			isRunningMongo = true;
		}

        if (isRunningColy)
        {
            Debug.LogError("Already running Colyseus");
        }
		else
		{
			// === Node process for Colyseus Server ===
			string[] nodePathParts = {Application.streamingAssetsPath, "Server", "Node", "node.exe"};
			string npmBinPath = Path.Combine(nodePathParts);
			string appEntryPoint = Path.Combine(new[] {Application.streamingAssetsPath, "Server", "node_modules", "ts-node", "dist"});

			colyProcess = new System.Diagnostics.Process();
			colyProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			colyProcess.StartInfo.FileName = npmBinPath;
			colyProcess.StartInfo.Arguments = "bin.js ../../../index.ts";
			colyProcess.StartInfo.WorkingDirectory = appEntryPoint;
			colyProcess.StartInfo.UseShellExecute = false;
			colyProcess.EnableRaisingEvents = true;
			colyProcess.Exited += OnExitColy;
			colyProcess.StartInfo.RedirectStandardInput = true;
			if (windowless){
				colyProcess.StartInfo.CreateNoWindow = true;
				colyProcess.StartInfo.RedirectStandardOutput = true;
				colyProcess.StartInfo.RedirectStandardError = true;
				colyProcess.OutputDataReceived += ColyOnOutputData;
				colyProcess.ErrorDataReceived += ColyOnOutputData;
			}
			colyProcess.Start();
			colyProcess.BeginOutputReadLine();
			colyProcess.BeginErrorReadLine();

			isRunningColy = true;
		}
    }

	// Make sure to clean up our mess upon leaving
    void OnDestroy()
	{
		if (isServerHost)
		{
			StopMongo();
			StopColyseus();
		}
	}

    void StopMongo()
	{
		if (!isRunningMongo)
			return;

		mongoKillProcess = new System.Diagnostics.Process();
		//mongoKillProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
		mongoKillProcess.StartInfo.FileName = Path.Combine(Application.streamingAssetsPath, "Server", "MongoDB", "Server", "4.0", "bin", "mongo.exe");		// Mongo, not Mongod as above
		mongoKillProcess.StartInfo.Arguments = "--eval \"db.getSiblingDB('admin').shutdownServer()\"";
		//mongoKillProcess.StartInfo.UseShellExecute = false;
		mongoKillProcess.Start();
		Task.Delay(500).Wait();
		mongoKillProcess.Kill();
		mongoKillProcess.Dispose();
		isRunningMongo = false;
	}

	void StopColyseus(){
		// @TODO - for future scaling it would be safest to remove all possibility of a client initiating a server shutdown
		if (!isRunningColy)
			return;

		colyProcess.CloseMainWindow();
		//colyProcess.Kill();
		colyProcess.Dispose();

		isRunningColy = false;
	}

    void MongoOnOutputData(object sender, System.Diagnostics.DataReceivedEventArgs e)
	{
		if (!mongoOutput || e.Data == null || e.Data == "")
			return;
			
		Debug.Log("Mongo: "+e.Data);
	}

	void ColyOnOutputData(object sender, System.Diagnostics.DataReceivedEventArgs e)
	{
		if (!colyOutput)
			return;

		Debug.Log("Coly: "+e.Data);
	}

    void OnExitMongo(object sender, System.EventArgs e)
	{
		isRunningMongo = false;
		if (mongoProcess.ExitCode != 0) {
			Debug.LogError("MongoDB Error! Exit Code: " + mongoProcess.ExitCode);
		}
	}

	void OnExitColy(object sender, System.EventArgs e)
	{
		isRunningColy = false;
		if (colyProcess.ExitCode != 0) {
			Debug.LogError("Colyseus Error! Exit Code: " + colyProcess.ExitCode);
		}
	}
}
