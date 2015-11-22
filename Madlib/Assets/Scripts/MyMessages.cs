using UnityEngine;
using UnityEngine.Networking;

using System.Collections.Generic;

public class MyMessages {
	public enum MyMessageTypes {
		CHAT_MESSAGE = 1000,
		USER_INFO = 2000,
		USER_LIST = 3000
	}

	public class ChatMessage : MessageBase {
		public string user;
		public string message;
	}

	public class UserMessage : MessageBase {
		public string user;
		public bool connected;
	}

//	public class UserListMessage : MessageBase {
//		public List<string> users;
//	}
}