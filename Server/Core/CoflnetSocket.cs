﻿using System.Collections;
using System.Collections.Generic;
using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Security.Cryptography.X509Certificates;
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


	public void SetBirthDay(CommandData data)
	{
		UserController.instance.GetUser(data.SenderId).Birthday = data.GetAs<DateTime>();

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
		//      wssv.Log.Output = Logger.Log;


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
	/// Tries to send a command to the <see cref="CommandData.Recipient"/> will try direct connection, Managing Server and Location router.
	/// </summary>
	/// <returns><c>true</c>, if command was sent, <c>false</c> otherwise.</returns>
	/// <param name="data">Data.</param>
	public static bool TrySendCommand(CommandData data, long serverId = 0)
	{
		if (serverId == 0)
		{
			if (Instance.server.TrySend(data))
			{
				return true;
			}
			// resource isn't connected, try to route the data forward
			serverId = data.Recipient.ServerId;
		}

		if (Instance.server.TrySendTo(new EntityId(serverId, 0), data))
		{
			return true;
		}
		// & 0x7FFFFFFFFFFF0000 removes the local serverid wich leaves us with the location router
		return Instance.server.TrySendTo(new EntityId(serverId & 0x7FFFFFFFFFFF0000, 0), data);
	}
}


[MessagePackObject]
public class ServerCommandData : CommandData
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


	public static CommandData SerializeServerCommandData<T>(T target, string type, CoflnetEncoder encoder)
	{
		return new CommandData(type, encoder.Serialize<T>(target));
	}

	public override T GetAs<T>()
	{
		return Connection.Encoder.Deserialize<T>(this.message);
	}


	public ServerCommandData() : base()
	{
	}

	public ServerCommandData(CommandData data) : base(data)
	{
	}

	public override void SendBack(CommandData data)
	{
		if(this.SenderId.ServerId == 0)
		{
			// the senderId is local to the device
			// we don't know who he is, yet just return the data
			data.Recipient = this.SenderId;
			Connection.SendBack(data);
		} else 
		{
			// use the senderId to send back the data
			base.SendBack(data);
		}
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
	List<EntityId> AuthenticatedIds { get; set; }

	/// <summary>
	/// Additional Tokens for accessing protected resources with specific scopes
	/// </summary>
	/// <value>The tokens</value>
	Dictionary<EntityId,Token> Tokens {get;set;}

	CoflnetEncoder Encoder { get; }

	void SendBack(CommandData data);
}




/// <summary>
/// Coflnet json encoder used to send back json.
/// </summary>
public partial class CoflnetJsonEncoder : CoflnetEncoder
{
	public static new CoflnetJsonEncoder Instance { get; }

	static CoflnetJsonEncoder()
	{
		Instance = new CoflnetJsonEncoder();
	}

    public CoflnetJsonEncoder()
    {
    }

    /*
public override T Deserialize<T>(MessageEventArgs args)
{
    var bytes = MessagePackSerializer.FromJson(args.Data);
    return CoflnetEncoder.Instance.Deserialize<T>(bytes);
}


public override ServerCommandData Deserialize(MessageEventArgs args)
{
    var bytes = MessagePackSerializer.FromJson(args.Data);
    return (ServerCommandData)CoflnetEncoder.Instance.Deserialize<DevCommandData>(bytes);
}
*/

    public override T Deserialize<T>(byte[] args)
	{
		var bytes = MessagePackSerializer.ConvertFromJson(Encoding.UTF8.GetString(args));
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
		return Encoding.UTF8.GetBytes(MessagePackSerializer.SerializeToJson<T>(target));
	}
}



[DataContract]
public class CoflnetWebsocketServer : WebSocketBehavior, IClientConnection
{
	protected CommandController commandController;
	protected static Dictionary<EntityId, CoflnetWebsocketServer> Connections;

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

	private List<EntityId> _authenticatedIds;

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
		Connections = new Dictionary<EntityId, CoflnetWebsocketServer>();
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


		foreach (var item in protocols)
		{
			if (item == "dev")
			{
				this.Encoder = CoflnetJsonEncoder.Instance;
				Send("runing on dev, Format");
				Send(MessagePackSerializer.SerializeToJson(
					new CoflnetJsonEncoder.DevCommandData(ServerCommandData.SerializeServerCommandData
					(new KeyValuePair<string, string>("exampleKey", "This is an example of a valid command"),
					 "setValue",
					 this.Encoder))));
				//	CommandData.SerializeCommandData("hi", "setValue")))));
			}
		}


		if (this.Encoder == null)
		{
			this.Encoder = CoflnetEncoder.Instance;
		}

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
			ServerCommandData commandData = Encoder.Deserialize(e);
			// if connection information is needed
			commandData.Connection = this;


			if (commandData.Recipient.ServerId == 0)
			{
				throw new CoflnetException("unknown_server", "this server is unknown (There is no server with the id 0)");
			}


			// prevent id spoofing
			if (commandData.SenderId != new EntityId() && !AuthenticatedIds.Contains(commandData.SenderId))
			{
				throw new NotAuthenticatedAsException(commandData.SenderId);
			}


			EntityManager.Instance.ExecuteForReference(commandData);
			//	var controllerForObject = ReferenceManager.Instance.GetResource(commandData.rId)
			//					.GetCommandController();

			//	controllerForObject.ExecuteCommand(commandData);
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
		//Logger.Log(ID);
		//Logger.Log("Got some data :)");      
	}


	protected override void OnError(WebSocketSharp.ErrorEventArgs e)
	{
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
	public bool TrySendTo(EntityId receiver, byte[] data)
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
	/// <param name="data">Data to send with valid <see cref="CommandData.Recipient"/>.</param>
	public bool TrySend(CommandData data)
	{
		return TrySendTo(data.Recipient, data);

	}

	/// <summary>
	/// Tries the send data to some specific <see cref="EntityId"/> may be a router or managing server
	/// </summary>
	/// <returns><c>true</c>, if send to was successful, <c>false</c> otherwise.</returns>
	/// <param name="receiver">Receiver.</param>
	/// <param name="data">Data.</param>
	public bool TrySendTo(EntityId receiver, CommandData data)
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

	public List<EntityId> AuthenticatedIds
	{
		get
		{
			if (_authenticatedIds == null)
			{
				_authenticatedIds = new List<EntityId>();
			}
			return _authenticatedIds;
		}

		set
		{
			_authenticatedIds = value;
		}
	}

    public Dictionary<EntityId, Token> Tokens
    {
        get;set;
    }

    public void SendBack(CommandData data)
	{
		SendBack(Encoder.Serialize(data));
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
	public EntityId deviceId;

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






