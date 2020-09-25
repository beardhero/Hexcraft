using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;

public class NetworkManager : MonoBehaviour
{
    private System.Diagnostics.Process process1 = null, process2 = null;
    bool isRunningMongo, isRunningColy;
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

    public void OnHostServer(string endpoint) 
	{
        LaunchMongoAndColyseus();
		isServerHost = true;
		// @TODO: verify textObject.text is a valid url
		client.ConnectToServer(endpoint);
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

			process1 = new System.Diagnostics.Process();
			process1.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			process1.StartInfo.FileName = mongoBinPath;
			process1.StartInfo.Arguments = "--dbpath=\""+dbPath+"\""; // Note the importance of encapsulating the dbPath in quotes in order to preserve spaces in filenames and folders
			process1.StartInfo.UseShellExecute = false;
			process1.EnableRaisingEvents = true;
			process1.Exited += OnExitMongo;
			if (windowless)
			{
				process1.StartInfo.CreateNoWindow = true;
				process1.StartInfo.RedirectStandardOutput = true;
				process1.StartInfo.RedirectStandardError = true;
				process1.OutputDataReceived += MongoOnOutputData;
				process1.ErrorDataReceived += MongoOnOutputData;
			}
			process1.Start();
			process1.BeginOutputReadLine();
			process1.BeginErrorReadLine();
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

			process2 = new System.Diagnostics.Process();
			process2.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			process2.StartInfo.FileName = npmBinPath;
			process2.StartInfo.Arguments = "bin.js ../../../index.ts";
			process2.StartInfo.WorkingDirectory = appEntryPoint;
			process2.StartInfo.UseShellExecute = false;
			process2.EnableRaisingEvents = true;
			process2.Exited += OnExitColy;
			process2.StartInfo.RedirectStandardInput = true;
			if (windowless){
				process2.StartInfo.CreateNoWindow = true;
				process2.StartInfo.RedirectStandardOutput = true;
				process2.StartInfo.RedirectStandardError = true;
				process2.OutputDataReceived += ColyOnOutputData;
				process2.ErrorDataReceived += ColyOnOutputData;
			}
			process2.Start();
			process2.BeginOutputReadLine();
			process2.BeginErrorReadLine();

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

		process1.CloseMainWindow();
		//process1.Kill();
		process1.Dispose();
		isRunningMongo = false;
	}

	void StopColyseus(){
		// @TODO - for future scaling it would be safest to remove all possibility of a client initiating a server shutdown
		if (!isRunningColy)
			return;

		process2.CloseMainWindow();
		//process1.Kill();
		process2.Dispose();

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
		if (process1.ExitCode != 0) {
			Debug.LogError("MongoDB Error! Exit Code: " + process1.ExitCode);
		}
	}

	void OnExitColy(object sender, System.EventArgs e)
	{
		isRunningColy = false;
		if (process2.ExitCode != 0) {
			Debug.LogError("Colyseus Error! Exit Code: " + process2.ExitCode);
		}
	}
}
