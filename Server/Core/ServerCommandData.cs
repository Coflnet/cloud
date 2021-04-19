using MessagePack;
using Coflnet;

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
        if (this.SenderId.ServerId == 0)
        {
            // the senderId is local to the device
            // we don't know who he is, yet just return the data
            data.Recipient = this.SenderId;
            Connection.SendBack(data);
        }
        else
        {
            // use the senderId to send back the data
            base.SendBack(data);
        }
    }
}






