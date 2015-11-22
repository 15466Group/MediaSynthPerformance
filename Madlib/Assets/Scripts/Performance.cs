using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using System.Collections.Generic;

public class Performance : NetworkManager {

	private float roundWaitTime;
	private float amountWaited;
	private float initCountdown;
	private float countDown;
	private int round;
	private int numRounds;
	private string[] words;
	private string[] descriptors;
	private StreamReader story; //stream of the text file of the story, read and write to this
	private StreamReader dictionaryFile;//check they are valid words
	private HashSet<string> dict;
	private bool finished;
	private bool lockedForRound; //if other client sends in a valid word for the round first, then you can't
	private bool isHost;
	public string inputStoryPath;
	public string dictPath;
	public string toWritePath;
	public string wordsPath;
	public string descriptorsPath;

	/*              network stuff                */
	//system information -> network -> wifi address
	//public string connectionIP = "128.237.182.237";
	public string connectionIP = "localhost";
	//some ridiculous number
	//public int portNumber = 8271;
	public int portNumber = 7777;
	private string currentMessage = string.Empty;
	private bool connected;
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
	void Start () {
		roundWaitTime = 10f;
		amountWaited = 0f;
		initCountdown = 14f; //each round lasts 7 rounds
		countDown = initCountdown;
		round = 0;
		story = new StreamReader (inputStoryPath);
		dictionaryFile = new StreamReader (dictPath);
		dict = new HashSet<string> ();
		finished = false;
		initialzeStuff ();

		//network stuff
		connected = false;
		button = GameObject.Find("ToggleButton");
		nameStyle = new GUIStyle ();
		nameStyle.fontStyle = FontStyle.Bold;
		nameStyle.normal.textColor = Color.white;
		nameStyle.wordWrap = true;
		msgStyle = new GUIStyle ();
		msgStyle.fontStyle = FontStyle.Normal;
		msgStyle.wordWrap = true;
		msgStyle.normal.textColor = Color.white;
		isHost = false;
	}

	//prefill the words and these are the 
	void initialzeStuff(){
		//populate dict with american-english words
		string word;
		while ((word = dictionaryFile.ReadLine()) != null){
			dict.Add(word.ToLower());
		}
		dictionaryFile.Close();
		Debug.Log ("done with dict");
		Debug.Log (dict.Count);

		StreamReader wordReader = new StreamReader (wordsPath);
		StreamReader descriptorReader = new StreamReader (descriptorsPath);
		words = wordReader.ReadToEnd ().Split("\n"[0]);
		descriptors = descriptorReader.ReadToEnd ().Split ("\n" [0]);
		wordReader.Close ();
		descriptorReader.Close ();
		numRounds = Mathf.Min (words.Length, descriptors.Length);
	}

	//display the words that were chosen, write the words to the story, choose a background music
	//switch scenes to some end scene or something so this doesn't continue?
	void doEndSequence(){
		finished = true;
		int i = 0;
		string line;
		string final = "";
		while ((line = story.ReadLine()) != null){
			final += line;
			if (i < numRounds)
				final += " " + words[i] + " ";
			i++;
		}
		story.Close ();
		File.WriteAllText (toWritePath, final);
		Debug.Log ("finisehd");
		Debug.Break ();
		return;
	}



	/**************************             network stuff                *****************************/
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
		isHost = true;
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
		lockedForRound = true; //someone hit send so no more typing
		var msg = netMsg.ReadMessage <MyMessages.ChatMessage>();
		//button.GetComponent<ToggleScript>().ToggleColor();
		//userHistory.Add (msg.user);
		chatHistory.Add ((MyMessages.ChatMessage)msg);
		chatScrollPosition = (new Vector2 (0, 1000000));
		//for each client, update the word array with this, restart round somehow
		words [round] = msg.message;
		round++;
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

	private void OnGUI(){
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
			displayStuff();
		}
	}

	void Update(){
		doTimerStuff();
	}

	void doTimerStuff(){
		if (users.Count == 2) {
			//check timer, if countDown is <= 0, send message with words[round]
			if (!lockedForRound)
				countDown -= Time.deltaTime;
			else {
				countDown = initCountdown;
				currentMessage = "";
			}
			Debug.Log ("nextRound in: " + countDown);
			if (countDown <= 0f) {
				//no one supplied a word quick enough so host sends msg!
				if (isHost) {
					countDown = initCountdown;
					Debug.Log ("TIMES UP");
					currentMessage = words [round];
					sendMessage ();
				}
			}

			if (lockedForRound) { //new round stuff is happening?
				Debug.Log ("amountWaited: " + amountWaited);
				amountWaited += Time.deltaTime;
				if (amountWaited > roundWaitTime) {
					lockedForRound = false;
					amountWaited = 0f;
				}
			} else {
				Debug.Log ("***** descriptor *****" + descriptors [round]);
			}
		}
	}
	
	void displayStuff(){
		if (round < numRounds) {
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
							if (!lockedForRound){
								sendMessage();
							}
							break;
						}
					}
				}
				GUILayout.EndHorizontal ();
			}
		} 
		else {
			if (!finished)
				doEndSequence();
		}
	}

	bool isValidWord(string word){
		return ((!string.IsNullOrEmpty (word)) && word.Length >= 3 && dict.Contains (word));
	}

	//sends a chat message to server
	void sendMessage() {
		if (currentMessage == null)
			return;
		currentMessage.Trim ();
		currentMessage = currentMessage.ToLower ();
		if (isValidWord (currentMessage)) {
			//play good sound or whatever to show someone got a word
			MyMessages.ChatMessage msg = new MyMessages.ChatMessage ();
			msg.user = userName;
			msg.message = currentMessage;
			NetworkManager.singleton.client.Send ((short)MyMessages.MyMessageTypes.CHAT_MESSAGE, msg);
			currentMessage = "";
		} else {
			//play invalid sound do some animation thing, show an x on both screens in the gui or something
			Debug.Log ("not a valid word");
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
