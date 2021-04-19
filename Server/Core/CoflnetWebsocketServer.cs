using System.Collections.Generic;
using WebSocketSharp;
using WebSocketSharp.Server;
using MessagePack;
using Coflnet.Server;
using System.Runtime.Serialization;
using Coflnet;

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
        get; set;
    }

    public void SendBack(CommandData data)
    {
        SendBack(Encoder.Serialize(data));
    }
}






