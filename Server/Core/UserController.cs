using System.Collections.Generic;
using System;
using Coflnet;
using MessagePack;
using Coflnet.Server;



public class UserController
{
	string HandleFunc(WebSocketSharp.Net.WebSockets.WebSocketContext arg)
	{
		arg.Headers.Set("Authentication", "ahahahahha :)");
		return null;
	}

	public static UserController instance;

	static UserController()
	{
		instance = new UserController();

		//WebSocketSharp.Net.HttpConnection connection = new WebSocketSharp.Net.HttpConnection()
		//WebSocketSharp.Net.HttpListenerContext top = new WebSocketSharp.Net.HttpListenerContext()
		//WebSocketSharp.Net.WebSockets.HttpListenerWebSocketContext context = new WebSocketSharp.Net.WebSockets.HttpListenerWebSocketContext(,);
		//WebSocket webSocket = new WebSocket("wss://echo.websocket.org");

		//SaveUsers();
	}



	public CoflnetUser GetUser(SourceReference userId)
	{
		if (ReferenceManager.Instance.Exists(userId))
			return ReferenceManager.Instance.GetResource<CoflnetUser>(userId);
		throw new Exception("User not found");
	}


	public void AddUser(CoflnetUser user)
	{
		ReferenceManager.Instance.CreateReference(user);
	}
}



[System.Serializable]
[MessagePackObject]
public class SH
{
	[Key("name")]
	public List<int> name = new List<int>();
}




public class CustomUser : CoflnetUser
{
	public string beweis = "omg das geht";
}



/*
/// <summary>
/// Because user objects are usually only present on one or two servers
/// this reference is used instead
/// </summary>
public class UserReference
{
	protected string userId;
	/// <summary>
	/// The server identifier used as a shortcut for comparing to the curent server
	/// </summary>
	protected long serverId;
	[NonSerialized]
	protected CoflnetServer server;

	protected CoflnetServer secondaryServer;
	[NonSerialized]
	protected CoflnetUser user;

	public string UserId
	{
		get
		{
			return userId;
		}
	}

	public CoflnetServer Server
	{
		get
		{
			return server;
		}
	}

	public CoflnetUser User
	{
		get
		{
			return user;
		}
		set
		{
			user = value;
		}
	}


	public string ServerId
	{
		get
		{
			return serverId;
		}
	}

	public UserReference(string userId, CoflnetServer server, CoflnetUser user = null, CoflnetServer secondaryServer = null)
	{
		this.userId = userId;
		this.server = server;
		this.user = user;
		this.serverId = server.publicId;
		this.secondaryServer = secondaryServer;
	}
}
*/