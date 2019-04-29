using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Security.Cryptography.X509Certificates;
using Unity.Jobs;
using Unity.Collections;
using System.Net.Http;
using RestSharp;
using MessagePack;
using System.IO;
using System.Text;
using Coflnet.Server;
using System.Runtime.Serialization;
using Coflnet;
using MessagePack.Resolvers;

/// <summary>
/// Coflnet socket.
/// Handles transfering of data
/// </summary>
public class CoflnetSocket
{
	/*
     * Settings
     */
	public const bool sslEnabled = true;

	protected CoflnetWebsocketServer server;

	public static CoflnetSocket Instance;

	public static WebSocketServer socketServer;
	//static readonly RestClient client = new RestClient();


	public void SetBirthDay(MessageData data)
	{
		UserController.instance.GetUser(data.sId).Birthday = data.GetAs<DateTime>();

	}

	/*
	public static CommandController Commands
	{
		get
		{
			return socketServer.CommandController;
		}
	} */

	static CoflnetSocket()
	{
		socketServer = new WebSocketServer(8080, sslEnabled);
		socketServer.Log.Level = LogLevel.Trace;
		//      wssv.Log.Output = Debug.Log;


		//socketServer = new CoflnetWebsocketServer();
		//socketServer.CommandController.RegisterCommand("setAge", SetAge);


		socketServer.AddWebSocketService<CoflnetWebsocketServer>("/socket", (s) =>
		{
			s.Protocol = "dev";
			CoflnetSocket.Instance.server = s;
		});

		if (socketServer.IsSecure)
			socketServer.SslConfiguration.ServerCertificate =
					new X509Certificate2("/home/ekwav/dev/ssl/cert.pfx", "adh3o8UBIZUZHBTTUZIUgvghHU");
		socketServer.Start();

		Instance = new CoflnetSocket();
	}


	/// <summary>
	/// Tries to send a command to the <see cref="MessageData.rId"/> will try direct connection, Managing Server and Location router.
	/// </summary>
	/// <returns><c>true</c>, if command was sent, <c>false</c> otherwise.</returns>
	/// <param name="data">Data.</param>
	public static bool TrySendCommand(MessageData data, long serverId = 0)
	{
		if (serverId == 0)
		{
			if (Instance.server.TrySend(data))
			{
				return true;
			}
			// resource isn't connected, try to route the data forward
			serverId = data.rId.ServerId;
		}

		if (Instance.server.TrySendTo(new SourceReference(serverId, 0), data))
		{
			return true;
		}
		// & 0x7FFFFFFFFFFF0000 removes the local serverid wich leaves us with the location router
		return Instance.server.TrySendTo(new SourceReference(serverId & 0x7FFFFFFFFFFF0000, 0), data);
	}
}


[MessagePackObject]
public class ServerMessageData : MessageData
{
	[IgnoreMember]
	public IClientConnection Connection;

	/// <summary>
	/// Gets or sets the delivery attemts. Represents how often this message has been attempted to deliver.
	/// Will be 10000 if delivery was successful
	/// </summary>
	/// <value>The delivery attemts count.</value>
	[Key("da")]
	public short DeliveryAttemts { get; set; }

	[IgnoreMember]
	public bool IsDelivered
	{
		get
		{
			return DeliveryAttemts >= 10000;
		}
	}


	public static MessageData SerializeServerMessageData<T>(T target, string type, CoflnetEncoder encoder)
	{
		return new MessageData(type, encoder.Serialize<T>(target));
	}

	public override T GetAs<T>()
	{
		return Connection.Encoder.Deserialize<T>(this.message);
	}


	public ServerMessageData() : base()
	{
	}

	public ServerMessageData(MessageData data) : base(data)
	{
	}

	public override void SendBack(MessageData data)
	{
		data.rId = this.sId;
		Connection.SendBack(data);
	}
}

[MessagePack.Union(1, typeof(CoflnetWebsocketServer))]
public interface IClientConnection
{
	CoflnetUser User { get; set; }

	Device Device { get; set; }
	/// <summary>
	/// Gets or sets the identifiers that this Connection authenticated as.
	/// This connection is allowed to set all these ids as sender.
	/// </summary>
	/// <value>The authenticated identifiers.</value>
	List<SourceReference> AuthenticatedIds { get; set; }

	CoflnetEncoder Encoder { get; }

	void SendBack(MessageData data);
}


/// <summary>
/// Coflnet encoder extention methods.
/// </summary>
public static class CoflnetEncoderExtention
{
	public static T Deserialize<T>(this CoflnetEncoder encoder, MessageEventArgs args)
	{
		return encoder.Deserialize<T>(args.RawData);
	}

	public static ServerMessageData Deserialize(this CoflnetEncoder encoder, MessageEventArgs args)
	{
		return encoder.Deserialize<ServerMessageData>(args.RawData);
	}

	/// <summary>
	/// Send the specified data after encoding with the given socket.
	/// </summary>
	/// <param name="encoder">Encoder.</param>
	/// <param name="data">Data.</param>
	/// <param name="socket">Socket.</param>
	/// <typeparam name="T">The 1st type parameter.</typeparam>
	public static void Send<T>(this CoflnetEncoder encoder, T data, CoflnetWebsocketServer socket)
	{
		socket.SendBack(encoder.Serialize<T>(data));
	}
}




/// <summary>
/// Coflnet json encoder used to send back json.
/// </summary>
public class CoflnetJsonEncoder : CoflnetEncoder
{
	public static new CoflnetJsonEncoder Instance { get; }

	static CoflnetJsonEncoder()
	{
		Instance = new CoflnetJsonEncoder();
	}
	/*
	public override T Deserialize<T>(MessageEventArgs args)
	{
		var bytes = MessagePackSerializer.FromJson(args.Data);
		return CoflnetEncoder.Instance.Deserialize<T>(bytes);
	}


	public override ServerMessageData Deserialize(MessageEventArgs args)
	{
		var bytes = MessagePackSerializer.FromJson(args.Data);
		return (ServerMessageData)CoflnetEncoder.Instance.Deserialize<DevMessageData>(bytes);
	}
*/

	public override T Deserialize<T>(byte[] args)
	{
		var bytes = MessagePackSerializer.FromJson(Encoding.UTF8.GetString(args));
		Debug.Log(JsonUtility.ToJson(new ByteContainer(bytes)));
		return CoflnetEncoder.Instance.Deserialize<T>(bytes);
	}

	public class ByteContainer
	{
		public byte[] bytes;

		public ByteContainer(byte[] bytes)
		{
			this.bytes = bytes;
		}
	}

	public override byte[] Serialize<T>(T target)
	{
		return Encoding.UTF8.GetBytes(MessagePackSerializer.ToJson<T>(target));
	}
	/*
	public override void Send<T>(T data, CoflnetWebsocketServer socket)
	{
		socket.SendBack(MessagePackSerializer.ToJson<T>(data));
	}
*/
	[MessagePackObject]
	public class DevMessageData : ServerMessageData
	{
		byte[] ausgelagert;

		[Key("d")]
		public string data
		{
			get
			{
				return System.Convert.ToBase64String(message);
			}
			set
			{
				ausgelagert = Convert.FromBase64String(value);
			}
		}

		public override T GetAs<T>()
		{
			return Connection.Encoder.Deserialize<T>(this.ausgelagert);
		}


		public override string Data
		{
			get
			{
				return Encoding.UTF8.GetString(this.ausgelagert);

			}
		}


		public DevMessageData()
		{
		}

		public DevMessageData(MessageData data) : base(data)
		{
		}


	}
}


public class CoflEncoder<T>
{
	public T value;

	public void GetFromMessagePackSerializer(byte[] bytes)
	{
		MemoryStream memory = new MemoryStream(bytes);
		value = MessagePackSerializer.Deserialize<T>(memory);
	}

	public void FromJson(string json)
	{
		value = JsonUtility.FromJson<T>(json);
	}

	public T Value
	{
		get
		{
			return value;
		}
	}
}


[DataContract]
public class CoflnetWebsocketServer : WebSocketBehavior, IClientConnection
{
	protected CommandController commandController;
	protected static Dictionary<SourceReference, CoflnetWebsocketServer> Connections;

	/// <summary>
	/// Custom encoder, usually Json or messagepack
	/// </summary>
	[IgnoreDataMember]
	public CoflnetEncoder Encoder { get; protected set; }
	/// <summary>
	/// If authenticated and present the user
	/// </summary>
	private CoflnetUser _user;
	/// <summary>
	/// If authenticated and present the connected device
	/// </summary>
	private Device _device;

	/// <summary>
	/// If authenticated and present the server
	/// </summary>
	private CoflnetServer server;

	private List<SourceReference> _authenticatedIds;

	public CoflnetWebsocketServer(CommandController commandController)
	{
		this.commandController = commandController;
	}

	public CoflnetWebsocketServer()
	{
		this.commandController = new CommandController();
	}

	static CoflnetWebsocketServer()
	{
		Connections = new Dictionary<SourceReference, CoflnetWebsocketServer>();
	}


	public CommandController CommandController
	{
		get
		{
			return commandController;
		}
	}


	protected override void OnOpen()
	{
		var protocols = Context.SecWebSocketProtocols;


		Debug.Log("opening socket");
		foreach (var item in protocols)
		{
			if (item == "dev")
			{
				this.Encoder = CoflnetJsonEncoder.Instance;
				Debug.Log("Using dev encoder");
				Send("runing on dev, Format");
				Send(MessagePackSerializer.ToJson(
					new CoflnetJsonEncoder.DevMessageData(ServerMessageData.SerializeServerMessageData
					(new KeyValuePair<string, string>("hi", "ok"),
					 "setValue",
					 this.Encoder))));


				Send(JsonUtility.ToJson(
					new CoflnetJsonEncoder.ByteContainer(
						MessagePackSerializer.Serialize(new byte[] { 0x01, 0x01, 0x01, 0x01 }))));
				//	MessageData.SerializeMessageData("hi", "setValue")))));
			}
		}


		if (this.Encoder == null)
		{
			this.Encoder = CoflnetEncoder.Instance;
			Debug.Log("using standard encoder");
		}

		Debug.Log("Connection has " + Context.UserEndPoint.ToString());
		Debug.Log(Context.Headers);
		Send("heyho:D");
		Debug.Log("new connection");

		System.Timers.Timer timer = new System.Timers.Timer();
		timer.AutoReset = true;

		timer.Elapsed += t_Elapsed;

		timer.Start();


	}

	private static void t_Elapsed(object sender, System.Timers.ElapsedEventArgs e)

	{

		// do stuff every minute      
	}


	protected override void OnMessage(MessageEventArgs e)
	{
		// try to parse and execute the command sent
		try
		{
			UnityEngine.Debug.Log(e.Data);
			ServerMessageData messageData = Encoder.Deserialize(e);
			// if connection information is needed
			messageData.Connection = this;


			if (messageData.rId.ServerId == 0)
			{
				throw new CoflnetException("unknown_server", "this server is unknown (There is no server with the id 0)");
			}


			// prevent id spoofing
			if (messageData.sId != new SourceReference() && !AuthenticatedIds.Contains(messageData.sId))
			{
				Debug.Log("ids: " + MessagePackSerializer.ToJson(AuthenticatedIds));
				throw new NotAuthenticatedAsException(messageData.sId);
			}


			ReferenceManager.Instance.ExecuteForReference(messageData);
			//	var controllerForObject = ReferenceManager.Instance.GetResource(messageData.rId)
			//					.GetCommandController();

			//	controllerForObject.ExecuteCommand(messageData);
		}
		catch (CoflnetException ex)
		{
			Encoder.Send(new CoflnetExceptionTransmit(ex), this);
			Track.instance.Error(ex.Message, e.Data, ex.StackTrace);
		}
		/*
		catch (Exception ex)
		{
			Track.instance.Error(ex.Message, e.Data, ex.StackTrace);
		}*/
		//Debug.Log(ID);
		//Debug.Log("Got some data :)");      
	}


	protected override void OnError(WebSocketSharp.ErrorEventArgs e)
	{

		Debug.Log(e.Message + e.Exception.ToString());
		base.OnError(e);
	}
	protected override void OnClose(CloseEventArgs e)
	{
		Connections.Remove(User.Id);
		Connections.Remove(Device.Id);
		AuthenticatedIds.Clear();
		base.OnClose(e);
	}

	/// <summary>
	/// Sends some data over the connection
	/// </summary>
	/// <param name="data">Data.</param>
	public void SendBack(byte[] data)
	{
		Debug.Log("Sending data back");
		Send(data);
	}

	/// <summary>
	/// Sends a string over the connection
	/// </summary>
	/// <param name="data">Data.</param>
	public void SendBack(string data)
	{
		Send(data);
	}


	protected bool ValidateAuthorizationMessage(AuthorizationMessage message)
	{
		// validate that the connection is secure
		if (!Context.IsSecureConnection)
			throw new CoflnetException("connection_insecure", "The connection is not secure, please try again with a secure connection.");

		// validate that the device is local and the secrets match
		Device connectingDevice = DeviceController.instance.GetDevice(message.deviceId);
		if (connectingDevice == null || connectingDevice.Secret != message.deviceSecret)
			throw new CoflnetException("device_secret_invalid", "The device doesn't exist on this server or the secrets don't match");

		// 

		return true;
	}


	/// <summary>
	/// Tries the send data to some receiver
	/// </summary>
	/// <returns><c>true</c>, if send to was successful, <c>false</c> otherwise.</returns>
	/// <param name="receiver">Receiver.</param>
	/// <param name="data">Data to send.</param>
	public bool TrySendTo(SourceReference receiver, byte[] data)
	{
		CoflnetWebsocketServer target;
		Connections.TryGetValue(receiver, out target);
		if (target != null)
		{
			target.Send(data);
			return true;
		}
		return false;
	}

	/// <summary>
	/// Tries to send data to some receiver
	/// </summary>
	/// <returns><c>true</c>, if send was successful, <c>false</c> otherwise.</returns>
	/// <param name="data">Data to send with valid <see cref="MessageData.rId"/>.</param>
	public bool TrySend(MessageData data)
	{
		return TrySendTo(data.rId, data);

	}

	/// <summary>
	/// Tries the send data to some specific <see cref="SourceReference"/> may be a router or managing server
	/// </summary>
	/// <returns><c>true</c>, if send to was successful, <c>false</c> otherwise.</returns>
	/// <param name="receiver">Receiver.</param>
	/// <param name="data">Data.</param>
	public bool TrySendTo(SourceReference receiver, MessageData data)
	{
		return TrySendTo(receiver, Encoder.Serialize(data));

	}

	/// <summary>
	/// Gets the user if authenticated null otherwise.
	/// </summary>
	/// <returns>The user.</returns>
	public CoflnetUser User
	{
		get
		{
			return _user;
		}
		set
		{
			if (value != null)
			{
				Connections.Add(value.Id, this);
				AuthenticatedIds.Add(value.Id);
				Debug.Log($"Authenticated as {value.Id}");
			}
			_user = value;
		}
	}

	/// <summary>
	/// Gets the device if authenticated, null otherwise
	/// </summary>
	/// <returns>The device that this connection is going to.</returns>
	public Device Device
	{
		get
		{
			return _device;
		}
		set
		{
			Connections.Add(User.Id, this);
			AuthenticatedIds.Add(value.Id);
			_device = value;
		}
	}

	public List<SourceReference> AuthenticatedIds
	{
		get
		{
			if (_authenticatedIds == null)
			{
				_authenticatedIds = new List<SourceReference>();
			}
			return _authenticatedIds;
		}

		set
		{
			_authenticatedIds = value;
		}
	}

	public void SendBack(MessageData data)
	{
		SendBack(Encoder.Serialize(data));
	}
}





public class NotAuthenticatedAsException : CoflnetException
{
	public NotAuthenticatedAsException(SourceReference inquestion, string userMessage = null, int responseCode = 403, string info = null, long msgId = -1) : base("not_authenticated_as", $"You didn't authenticated as {inquestion.ToString()} over this connection", userMessage, responseCode, info)
	{
	}
}

[DataContract]
public class AuthorizationMessage
{
	[DataMember(Name = "v")]
	public int version;
	[DataMember(Name = "ds")]
	public string deviceSecret;
	[DataMember(Name = "did")]
	public SourceReference deviceId;

	public enum Format
	{
		MESSAGE_PACK,
		JSON,
		XML,
		PLAIN
	}

	[DataMember(Name = "f")]
	public Format format;
	[DataMember(Name = "ut")]
	public string userToken;
}



/// <summary>
/// Represents an oauth client application or some script capeable of performing commands.
/// </summary>
[DataContract]
public class OAuthClient : ILocalReferenceable
{
	private CommandController commandController;

	private static Dictionary<string, OAuthClient> clients = new Dictionary<string, OAuthClient>();

	public static OAuthClient Find(string id)
	{
		if (!clients.ContainsKey(id))
		{
			throw new ClientNotFoundException($"The client {id} wasn't found on this server");
		}
		return clients[id];
	}

	public override CommandController GetCommandController()
	{
		return commandController;
	}

	[DataMember]
	private int id;
	/// <summary>
	/// The client owner
	/// </summary>

	[DataMember]
	private SourceReference userId;
	[DataMember]
	private string name;
	[DataMember]
	private string publicId;
	[DataMember]
	private string description;
	[DataMember]
	private string secret;
	[DataMember]
	private string redirect;
	[DataMember]
	private bool revoked;
	[DataMember]
	private bool passwordClient;
	[DataMember]
	private string iconUrl;
	[DataMember]
	public OAuthClientSettings settings;
	/// <summary>
	/// Custom commands registered at runtime
	/// </summary>
	[IgnoreDataMember]
	public CommandController customCommands;

	public class ClientNotFoundException : CoflnetException
	{
		public ClientNotFoundException(string message, string userMessage = null, string info = null, long msgId = -1) : base("client_not_found", message, userMessage, 404, info, msgId)
		{
		}
	}
}


/// <summary>
/// Custom client settings
/// </summary>
[DataContract]
public class OAuthClientSettings
{
	[DataMember]
	private OAuthClient client;
	[DataMember]
	private int standardUserCalls;
	[DataMember]
	private int standardUserStorage;
	[DataMember]
	private int standardCallsUntilCaptcha;
	[DataMember]
	private bool allowSelfUserRegistration;
	[DataMember]
	private bool captchaRequiredForRegistration;
	[DataMember]
	private bool trackingEnabled;
	[DataMember]
	private UInt64 usageLeft;

	public void DecreaseUsage(ushort amount)
	{
		if (usageLeft < amount)
			throw new ClientLimitExeededException();
		usageLeft -= amount;
	}
}

public class ClientLimitExeededException : CoflnetException
{
	public ClientLimitExeededException(long msgId = -1) :
	base("client_limit_exeeded",
		 "This client has no usage left to create new Objects or submit calls. Please buy new usage or wait until next month",
		 null, 422, "/docs/pricing", msgId)
	{

	}
}

public class MonthlyDeveloperUsage
{
	/// <summary>
	/// Month index after the 1.1.1970
	/// </summary>
	private int monthIndex;
	private long apiCallsMade;
	private long dataUsage;
	private int usersCreated;
}






