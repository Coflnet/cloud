using System.Collections;
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
using System.Runtime.Serialization;
using Coflnet;
using MessagePack.Resolvers;

/// <summary>
/// Coflnet socket.
/// Handles transfering of data
/// </summary>
public class CoflnetSocket : ICommandTransmit
{
    /*
     * Settings
     */

    protected CoflnetWebsocketServer server;

    public static CoflnetSocket Instance;

    public static WebSocketServer socketServer;

    public CoflnetServices Services { get ; set; }

    //static readonly RestClient client = new RestClient();


    static CoflnetSocket()
    {
        var certificatePath = SimplerConfig.Config.Instance["X509Certificate"];
        var sslEnabled = certificatePath != null;
		if(!sslEnabled)
			Coflnet.Logger.Log("Config Key X509Certificate not found, please add a path to your cert.pfx");
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
        {

            socketServer.SslConfiguration.ServerCertificate =
                    new X509Certificate2("/home/ekwav/dev/ssl/cert.pfx", "adh3o8UBIZUZHBTTUZIUgvghHU");

        }
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
        if(Instance.server == null)
        {
            Coflnet.Logger.Log("no server instance set");
            return false;
        }
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

    public bool SendCommand(CommandData data)
    {
        return TrySendCommand(data);
    }

    public void AddCallback(ReceiveCommandData callback)
    {
        throw new NotImplementedException();
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
    Dictionary<EntityId, Token> Tokens { get; set; }

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






