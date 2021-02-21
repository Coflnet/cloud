using Coflnet.Core.Crypto;
using MessagePack;
using System;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Coflnet
{

    /// <summary>
    /// <see cref="CommandData"/> represents a message, contains
    /// m = actual message
    /// d = delete timestamp // deprecated
    /// t = type (command slug)
    /// s = sender Id
    /// r = receiverId
    /// i = message identifier choosen by the sender
    /// x = senders signature of the message content Ed25519(m|t|s|r|i)
    /// </summary>
    [MessagePackObject]
    [DataContract]
    public class CommandData : ICommandData
    {
        /// <summary>
        /// The sender identifier.
        /// </summary>
        [Key("s")]
        [DataMember]
        public EntityId SenderId;
        /// <summary>
        /// The recipient id
        /// </summary>
        [Key("r")]
        [DataMember]
        public EntityId Recipient;
        /// <summary>
        /// The message identifier choosen by the sender
        /// </summary>
        [Key("i")]
        [DataMember]
        public long MessageId;
        [Key("m")]
        [DataMember]
        public virtual byte[] message { get; set; }
        /// <summary>
        /// Type aka slug of a command
        /// </summary>
        [Key("t")]
        [DataMember]
        public string Type;

        [Key("h")]
        [DataMember]
        public CommandDataHeader Headers;

        /// <summary>
        /// senders signature of the message content Ed25519(m|t|s|r|i|h)
        /// </summary>
        [Key("x")]
        [DataMember]
        public Signature Signature;

        /// <summary>
        /// Event invoked when message got send (or moved to the sending chain)
        /// Can be used to execute longer running tasks
        /// </summary>
        public event Action<CommandData> AfterSend;

        protected dynamic deserialized;

        private CoflnetCore _coreInstance;

        /// <summary>
        /// Nexts the message identifier.
        /// </summary>
        /// <returns>The message identifier.</returns>
        protected long NextMsgId()
        {
            return ThreadSaveIdGenerator.NextId;
        }

        /// <summary>
        /// Gets the data as UTF8 encoded string.
        /// </summary>
        /// <value>The data as UTF8 string.</value>
        [IgnoreMember]
        [IgnoreDataMember]
        public virtual string Data
        {
            get
            {
                return Encoding.UTF8.GetString(message);
            }
            set
            {
                message = Encoding.UTF8.GetBytes(value);
            }
        }

        /// <summary>
        /// Gets or sets the deserialized value of this message
        /// </summary>
        /// <value>The deserialized.</value>
        [IgnoreMember]
        [IgnoreDataMember]
        public dynamic DeSerialized
        {
            get
            {
                return deserialized;
            }
            set
            {
                deserialized = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="CoflnetCore"/> used for this message.
        /// </summary>
        /// <value>The core wich should be used.</value>
        [IgnoreMember]
        [IgnoreDataMember]
        public CoflnetCore CoreInstance
        {
            get
            {
                if (_coreInstance == null)
                {
                    return CoflnetCore.Instance;
                }
                return _coreInstance;
            }
            set
            {
                _coreInstance = value;
            }
        }

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Coflnet.CommandData"/> class.
        /// </summary>
        public CommandData()
        {
        }

        /// <summary>
        /// Serializes and creates a new message data object.
        /// </summary>
        /// <returns>The message data object.</returns>
        /// <param name="data">Data.</param>
        /// <param name="type">Type.</param>
        /// <param name="m_id">M identifier.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static CommandData SerializeCommandData<T>(T data, string type, long m_id = 0)
        {
            return new CommandData(new EntityId(), m_id, MessagePackSerializer.Serialize<T>(data), type);
        }


        /// <summary>
        /// Creates the message data.
        /// </summary>
        /// <returns>The message data.</returns>
        /// <param name="data">Data.</param>
        /// <param name="target">Target.</param>
        /// <param name="m_id">M identifier.</param>
        /// <param name="sender">Sender of the message.</param>
        /// <typeparam name="C">The 1st type parameter.</typeparam>
        /// <typeparam name="T">The 2nd type parameter.</typeparam>
        public static CommandData CreateCommandData<C, T>(EntityId target, T data, long m_id = 0, EntityId sender = default(EntityId)) where C : Command
        {
            return new CommandData(sender, target, m_id, System.Activator.CreateInstance<C>().Slug, MessagePackSerializer.Serialize<T>(data));
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="T:Coflnet.CommandData"/> class.
        /// </summary>
        /// <param name="rId">R identifier.</param>
        /// <param name="m_id">M identifier.</param>
        /// <param name="m">M.</param>
        /// <param name="type">Type.</param>
        public CommandData(EntityId rId, long m_id, byte[] m, string type)
        {
            this.Recipient = rId;
            if (m_id == 0)
                this.MessageId = NextMsgId();
            else
                this.MessageId = m_id;
            this.message = m;
            this.Type = type;
        }

        public CommandData(CommandData data) : this(data.Recipient, data.MessageId, data.message, data.Type)
        {
            this.SenderId = data.SenderId;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="T:Coflnet.CommandData"/> class.
        /// </summary>
        /// <param name="rId">Receiver identifier.</param>
        /// <param name="m_id">M identifier.</param>
        /// <param name="m">Message data content.</param>
        /// <param name="type">Type of content (command slug).</param>
        public CommandData(EntityId rId, long m_id = 0, string m = "", string type = "") : this(rId, m_id, Encoding.UTF8.GetBytes(m), type)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Coflnet.CommandData"/> class.
        /// </summary>
        /// <param name="command">Command object to send.</param>
        /// <param name="m">The command data to send with the command.</param>
        /// <param name="m_id">Message- identifier.</param>
        public CommandData(Command command, byte[] m, long m_id = 0) : this(new EntityId(), m_id, m, command.Slug)
        {

        }


        public CommandData(string type, byte[] data)
        {
            this.Type = type;
            this.message = data;
        }

        /// <summary>
        /// simplest encoding recomended for sending commands to other systems
        /// </summary>
        /// <param name="type">The identifier of the command</param>
        /// <param name="data">The data to be passed</param>
        /// <returns></returns>
        public CommandData(string type, string data = "") : this(type, Encoding.UTF8.GetBytes(data))
        {

        }

        public CommandData(EntityId rId, byte[] message, string t)
        {
            this.Recipient = rId;
            this.message = message;
            this.Type = t;
        }

        #endregion

        /// <summary>
        /// Gets the deserialized content of this message, cached the deserialized object
        /// </summary>
        /// <returns>The as.</returns>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public virtual T GetAs<T>()
        {
            if (deserialized == null) // Object.ReferenceEquals(null, deserialized))
            {
                if (message.Length == 0)
                {
                    throw new System.InvalidOperationException($"Could not get data as {nameof(T)} because it is empty");
                }
                Deserialize<T>();
            }
            else if (!(deserialized is T))
            {
                // if eg a derived class is needed this will redeserialize it correctly
                Deserialize<T>();
            }
            return (T)deserialized;

        }

        /// <summary>
        /// Deserializes the message content to the given type
        /// Stores the result in serialized attribute 
        /// </summary>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public virtual void Deserialize<T>()
        {
            deserialized = MessagePackSerializer.Deserialize<T>(message);
        }

        /// <summary>
        /// Serialize the specified object and stores the result in the message field
        /// </summary>
        /// <param name="ob">The object to serialize</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public CommandData SerializeAndSet<T>(T ob)
        {
            message = Serialize<T>(ob);
            return this;
        }

        /// <summary>
        /// Serialize the specified object
        /// </summary>
        /// <returns>The serialized byte array.</returns>
        /// <param name="ob">The object which to serialize.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public byte[] Serialize<T>(T ob)
        {
            return MessagePackSerializer.Serialize<T>(ob);
        }

        public void SetCommand<C>() where C : Command, new()
        {
            this.Type = (new C()).Slug;
        }


        public override bool Equals(object obj)
        {
            var converted = obj as CommandData;

            if (converted == null)
                return false;

            return converted.SenderId == this.SenderId
                            && converted.MessageId == this.MessageId
                            && converted.Recipient == this.Recipient;
        }

        public override int GetHashCode()
        {
            return (SenderId.GetHashCode() * MessageId).GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("[CommandData: sId={0}, rId={1}, mId={2}, t={3}, Data={4}]", SenderId, Recipient, MessageId, Type, Data);
        }

        /// <summary>
        /// Sends the command back.
        /// </summary>
        /// <param name="data">Data.</param>
        public virtual void SendBack(CommandData data)
        {
            data.Recipient = this.SenderId;
            CoreInstance.SendCommand(data);

            AfterSend?.Invoke(this);
        }

        public virtual void SendCommandTo<TCom, TDat>(EntityId target, TDat data) where TCom : Command
        {
            CoreInstance.SendCommand<TCom, TDat>(target, data, this.Recipient);
        }

        public virtual Task<TRes> SendGetCommand<TCom, TDat, TRes>(EntityId target, TDat data) where TCom : ReturnCommand
        {
            var source = new TaskCompletionSource<TRes>();
            CoreInstance.SendGetCommand<TCom, TDat>(target, data, this.Recipient, response=>{
                source.SetResult(response.GetAs<TRes>());
            });
            return source.Task;
        }

        public CommandData(EntityId sId, EntityId rId, long mId, string t, byte[] message) : this(rId, mId, message, t)
        {
            this.SenderId = sId;
        }

        /// <summary>
        /// Gets the target resource as.
        /// </summary>
        /// <returns>The target as.</returns>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public virtual T GetTargetAs<T>() where T : Entity
        {
            return CoreInstance.EntityManager.GetEntity<T>(Recipient);
        }


        /// <summary>
        /// Signs the message contents with the given keyPairs private key
        /// </summary>
        /// <param name="singKeyPair">The keypair containing the private key of the advertised <see cref="SenderId"/></param>
        public void Sign(KeyPair singKeyPair)
        {
            this.Signature = new Signature()
            { algorythm = EncryptionController.Instance.SigningAlgorythm };
            this.Signature.GenerateSignature(SignableContent, singKeyPair);
        }

        /// <summary>
        /// Validates that this message was signed by the private part of the given publicKey
        /// </summary>
        /// <param name="publicKey">The public key of the sending resource</param>
        public bool ValidateSignature(byte[] publicKey)
        {
            if (Signature == null)
            {
                throw new SignatureInvalidException("No signature was set");
            }
            return this.Signature.ValidateSignature(SignableContent, publicKey);
        }


        [IgnoreMember]
        public byte[] SignableContent
        {
            get
            {
                return IEncryption.ConcatBytes(
                    message,
                    Type != null ? Encoding.UTF8.GetBytes(Type) : null,
                    SenderId.AsByte,
                    Recipient.AsByte,
                    BitConverter.GetBytes(MessageId),
                    Headers?.Serialized
                    );
            }
        }


        public class SignatureInvalidException : CoflnetException
        {
            public SignatureInvalidException(string message = "The signature set is invalid", string userMessage = null, string info = null, long msgId = -1)
            : base("signature_invalid", message, userMessage, 401, info, msgId)
            {
            }
        }
    }
}


