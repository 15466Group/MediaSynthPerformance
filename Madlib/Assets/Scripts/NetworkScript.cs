using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using System.Collections.Generic;

public class NetworkScript : NetworkManager
{
	//system information -> network -> wifi address
	//public string connectionIP = "128.237.182.237";
	public string connectionIP = "localhost";
	//some ridiculous number
	//public int portNumber = 8271;
	public int portNumber = 7777;
	private string currentMessage = "";
	private bool connected = false;

	public string userName;

    bool toggle = false;
    public GameObject button;

	public List<MyMessages.ChatMessage> chatHistory = new List<MyMessages.ChatMessage> ();
	//public List<string> userHistory = new List<string> ();
	public List<string> users = new List<string>();
    public static short MSGType = 555;
	private GUIStyle nameStyle;
	private GUIStyle msgStyle;

	private Vector2 chatScrollPosition = Vector2.zero;
	private Vector2 userScrollPosition = Vector2.zero;

	
    // Use this for initialization
    void Start()
    {
        button = GameObject.Find("ToggleButton");
		userName = "default";
		nameStyle = new GUIStyle ();
		nameStyle.fontStyle = FontStyle.Bold;
		nameStyle.normal.textColor = Color.white;
		nameStyle.wordWrap = true;
		msgStyle = new GUIStyle ();
		msgStyle.fontStyle = FontStyle.Normal;
		msgStyle.wordWrap = true;
		msgStyle.normal.textColor = Color.white;

    }

	public override void OnClientConnect(NetworkConnection conn) {
		connected = true;
		base.OnClientConnect (conn);
		Debug.Log ("client is connected");
		sendUserConnect ();
	}

	public override void OnServerConnect(NetworkConnection conn) {
		base.OnServerConnect (conn);
		foreach (string u in users) {
			MyMessages.UserMessage um = new MyMessages.UserMessage ();
			um.user = u;
			um.connected = true;
			NetworkServer.SendToClient (conn.connectionId, (short) MyMessages.MyMessageTypes.USER_INFO, um);
		}
		Debug.Log ("new client's users updated");
	}

	public override void OnServerDisconnect (NetworkConnection conn) {
		base.OnServerDisconnect (conn);
		sendUserDisconnect ();
	}

	//automatically called when starting a client
    public override void OnStartClient(NetworkClient mClient)
    {
		Debug.Log ("onstartclient called");
		base.OnStartClient(mClient);
		mClient.RegisterHandler((short)MyMessages.MyMessageTypes.CHAT_MESSAGE, OnClientChatMessage);
		mClient.RegisterHandler((short)MyMessages.MyMessageTypes.USER_INFO, OnClientUserInfo);
    }
	

	//automatically called when starting a server
    // hook into NetManagers server setup process
    public override void OnStartServer()
    {
		Debug.Log ("onstartserver called");
        base.OnStartServer(); //base is empty
		NetworkServer.RegisterHandler ((short)MyMessages.MyMessageTypes.CHAT_MESSAGE, OnServerChatMessage);
		NetworkServer.RegisterHandler ((short)MyMessages.MyMessageTypes.USER_INFO, OnServerUserInfo);
		//connected = true;
    }
	

	//when a chat message reaches the server
    private void OnServerChatMessage(NetworkMessage netMsg)
    {
        var msg = netMsg.ReadMessage<MyMessages.ChatMessage>();
		MyMessages.ChatMessage chat = new MyMessages.ChatMessage ();
		chat.user = msg.user;
		chat.message = msg.message;
		NetworkServer.SendToAll((short) MyMessages.MyMessageTypes.CHAT_MESSAGE, chat);
        //button.GetComponent<ToggleScript>().ToggleColor();
    }
	
	//when a chat message reaches the client
    private void OnClientChatMessage(NetworkMessage netMsg)
    {
		var msg = netMsg.ReadMessage <MyMessages.ChatMessage>();
        //button.GetComponent<ToggleScript>().ToggleColor();
		//userHistory.Add (msg.user);
		chatHistory.Add ((MyMessages.ChatMessage)msg);
		chatScrollPosition = (new Vector2 (0, 1000000));
    }

	private void OnServerUserInfo(NetworkMessage netMsg)
	{
		var msg = netMsg.ReadMessage<MyMessages.UserMessage> ();
		MyMessages.UserMessage um = new MyMessages.UserMessage ();
		if (msg.connected && !users.Contains (msg.user)) {
			this.users.Add (msg.user);
			Debug.Log ("user added to host");
		} else if (!msg.connected && users.Contains (msg.user)) {
			this.users.Remove (msg.user);
			Debug.Log ("user removed from host");
		} else {
			Debug.Log ("nothing happened at host");
		}
		um.user = msg.user;
		um.connected = msg.connected;
//		Debug.Log (um.users);
		NetworkServer.SendToAll((short) MyMessages.MyMessageTypes.USER_INFO, um);
	}

	private void OnClientUserInfo(NetworkMessage netMsg)
	{

		var msg = netMsg.ReadMessage<MyMessages.UserMessage> ();
		Debug.Log (msg.connected);
		Debug.Log (msg.user);
		if (msg.connected && !users.Contains (msg.user)) {
			this.users.Add (msg.user);
			Debug.Log ("user added to client");
		} else if (!msg.connected && users.Contains (msg.user)) {
			this.users.Remove (msg.user);
			Debug.Log ("user removed from client");
		} else {
			Debug.Log ("nothing happened at client");
		}
		//Debug.Log (this.users);
	}



	private void OnGUI()
	{
		if (!connected) {

			GUILayout.BeginVertical (GUILayout.Width (300));
			{
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label ("Connection IP", GUILayout.Width (100));
					connectionIP = GUILayout.TextField (connectionIP);
				}
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label ("Port Number ", GUILayout.Width (100));
					int.TryParse (GUILayout.TextField (portNumber.ToString()), out portNumber);
				}
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label ("Username ", GUILayout.Width (100));
					userName = GUILayout.TextField (userName);
				}
				GUILayout.EndHorizontal ();


			}
			GUILayout.EndVertical ();


			//if connect button clicked
			if (GUILayout.Button ("Connect")) {
				this.networkAddress = connectionIP;
				this.networkPort = portNumber;
				this.StartClient();
			}
			//if host button clicked
			//a host is a server and a client at the same time
			if (GUILayout.Button ("Host")) {
				this.networkAddress = connectionIP;
				this.networkPort = portNumber;
				this.StartHost();
			}
		} else {
			//GUILayout.Label ("Connections: " + this.users.Count);
		}

		if (connected) {



			//chat display
			GUILayout.BeginVertical (GUILayout.Width (400));
			{
				GUILayout.BeginHorizontal (GUILayout.Height (250));
				{
					chatScrollPosition = GUILayout.BeginScrollView (chatScrollPosition, GUILayout.Width (200));
					foreach (MyMessages.ChatMessage c in chatHistory) {
						GUILayout.BeginHorizontal ();
						{
							GUILayout.Label (c.user + ":", nameStyle, GUILayout.Width (50));
							GUILayout.Label (c.message, msgStyle);
						}
						GUILayout.EndHorizontal ();
						
					}
					GUILayout.EndScrollView();
					userScrollPosition = GUILayout.BeginScrollView (userScrollPosition);
					GUILayout.Label ("Currently Connected: " + users.Count);
					foreach (string u in users) {
						GUILayout.Label (u);
					}
					GUILayout.EndScrollView();

				}
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal (GUILayout.Width (250));
				{
					currentMessage = GUILayout.TextField (currentMessage, GUILayout.Width(200));
					if(Event.current.isKey) {
						switch (Event.current.keyCode) {
						case KeyCode.Return:
							sendMessage();
							break;
						}
					}
					//currentMessage = GUILayout.TextField (currentMessage);
					if (GUILayout.Button ("send")) {
						sendMessage();
					}
				}
				GUILayout.EndHorizontal ();
			}

		}
	}

	//sends a chat message to server
	void sendMessage() {
		if (!string.IsNullOrEmpty (currentMessage)) {
			currentMessage.Trim ();
			MyMessages.ChatMessage msg = new MyMessages.ChatMessage ();
			msg.user = userName;
			msg.message = currentMessage;
			NetworkManager.singleton.client.Send ((short)MyMessages.MyMessageTypes.CHAT_MESSAGE, msg);
			currentMessage = "";
		}
	}

	//sends connect notification to server
	void sendUserConnect() {
		MyMessages.UserMessage umsg = new MyMessages.UserMessage ();
		umsg.user = userName;
		umsg.connected = true;
		NetworkManager.singleton.client.Send ((short)MyMessages.MyMessageTypes.USER_INFO, umsg);
	}

	//sends disconnect notification to server
	void sendUserDisconnect() {
		MyMessages.UserMessage umsg = new MyMessages.UserMessage ();
		umsg.user = userName;
		umsg.connected = false;
		NetworkManager.singleton.client.Send ((short)MyMessages.MyMessageTypes.USER_INFO, umsg);
	}

}
