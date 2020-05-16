using Coflnet.Core.Crypto;
using MessagePack;
using System;
using System.Text;

namespace Coflnet
{

    /// <summary>
    /// Messagedata represents a message, contains
    /// m = actual message
    /// d = delete timestamp // deprecated
    /// t = type (command slug)
    /// s = sender Id
    /// r = receiverId
    /// i = message identifier choosen by the sender
    /// x = senders signature of the message content Ed25519(m|t|s|r|i)
    /// </summary>
    [MessagePackObject]
    public class MessageData : IMessageData
    {
        /// <summary>
        /// The sender identifier.
        /// </summary>
        [Key("s")]
        public SourceReference sId;
        /// <summary>
        /// The recipient id
        /// </summary>
        [Key("r")]
        public SourceReference rId;
        /// <summary>
        /// The message identifier choosen by the sender
        /// </summary>
        [Key("i")]
        public long mId;
        [Key("m")]
        public virtual byte[] message { get; set; }
        /// <summary>
        /// Type aka slug of a command
        /// </summary>
        [Key("t")]
        public string type;

        [Key("h")]
        public MessageDataHeader headers;

        /// <summary>
        /// senders signature of the message content Ed25519(m|t|s|r|i|h)
        /// </summary>
        [Key("x")]
        public Signature signature;

        /// <summary>
        /// Event invoked when message got send (or moved to the sending chain)
        /// Can be used to execute longer running tasks
        /// </summary>
        public event Action<MessageData> AfterSend;

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
        public virtual string Data
        {
            get
            {
                return Encoding.UTF8.GetString(message);
            }
        }

        /// <summary>
        /// Gets or sets the deserialized value of this message
        /// </summary>
        /// <value>The deserialized.</value>
        [IgnoreMember]
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
        /// Initializes a new instance of the <see cref="T:Coflnet.MessageData"/> class.
        /// </summary>
        public MessageData()
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
        public static MessageData SerializeMessageData<T>(T data, string type, long m_id = 0)
        {
            return new MessageData(new SourceReference(), m_id, MessagePackSerializer.Serialize<T>(data), type);
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
        public static MessageData CreateMessageData<C, T>(SourceReference target, T data, long m_id = 0, SourceReference sender = default(SourceReference)) where C : Command
        {
            return new MessageData(sender, target, m_id, System.Activator.CreateInstance<C>().Slug, MessagePackSerializer.Serialize<T>(data));
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="T:Coflnet.MessageData"/> class.
        /// </summary>
        /// <param name="rId">R identifier.</param>
        /// <param name="m_id">M identifier.</param>
        /// <param name="m">M.</param>
        /// <param name="type">Type.</param>
        public MessageData(SourceReference rId, long m_id, byte[] m, string type)
        {
            this.rId = rId;
            if (m_id == 0)
                this.mId = NextMsgId();
            else
                this.mId = m_id;
            this.message = m;
            this.type = type;
        }

        public MessageData(MessageData data) : this(data.rId, data.mId, data.message, data.type)
        {
            this.sId = data.sId;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="T:Coflnet.MessageData"/> class.
        /// </summary>
        /// <param name="rId">Receiver identifier.</param>
        /// <param name="m_id">M identifier.</param>
        /// <param name="m">Message data content.</param>
        /// <param name="type">Type of content (command slug).</param>
        public MessageData(SourceReference rId, long m_id = 0, string m = "", string type = "") : this(rId, m_id, Encoding.UTF8.GetBytes(m), type)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Coflnet.MessageData"/> class.
        /// </summary>
        /// <param name="command">Command object to send.</param>
        /// <param name="m">The command data to send with the command.</param>
        /// <param name="m_id">Message- identifier.</param>
        public MessageData(Command command, byte[] m, long m_id = 0) : this(new SourceReference(), m_id, m, command.Slug)
        {

        }


        public MessageData(string type, byte[] data)
        {
            this.type = type;
            this.message = data;
        }

        /// <summary>
        /// simplest encoding recomended for sending commands to other systems
        /// </summary>
        /// <param name="type">The identifier of the command</param>
        /// <param name="data">The data to be passed</param>
        /// <returns></returns>
        public MessageData(string type, string data) : this(type,Encoding.UTF8.GetBytes(data))
        {

        }

        public MessageData(SourceReference rId, byte[] message, string t)
        {
            this.rId = rId;
            this.message = message;
            this.type = t;
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
        public void Deserialize<T>()
        {
            deserialized = MessagePackSerializer.Deserialize<T>(message);
        }

        /// <summary>
        /// Serialize the specified object and stores the result in the message field
        /// </summary>
        /// <param name="ob">The object to serialize</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public MessageData SerializeAndSet<T>(T ob)
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
            this.type = (new C()).Slug;
        }


        public override bool Equals(object obj)
        {
            var converted = obj as MessageData;

            if (converted == null)
                return false;

            return converted.sId == this.sId
                            && converted.mId == this.mId
                            && converted.rId == this.rId;
        }

        public override int GetHashCode()
        {
            return (sId.GetHashCode() * mId).GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("[MessageData: sId={0}, rId={1}, mId={2}, t={3}, Data={4}]", sId, rId, mId, type, Data);
        }

        /// <summary>
        /// Sends the command back.
        /// </summary>
        /// <param name="data">Data.</param>
        public virtual void SendBack(MessageData data)
        {
            data.rId = this.sId;
            CoreInstance.SendCommand(data);

            AfterSend?.Invoke(this);
        }

        public MessageData(SourceReference sId, SourceReference rId, long mId, string t, byte[] message) : this(rId, mId, message, t)
        {
            this.sId = sId;
        }

        /// <summary>
        /// Gets the target resource as.
        /// </summary>
        /// <returns>The target as.</returns>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public virtual T GetTargetAs<T>() where T : Referenceable
        {
            return CoreInstance.ReferenceManager.GetResource<T>(rId);
        }


        /// <summary>
        /// Signs the message contents with the given keyPairs private key
        /// </summary>
        /// <param name="singKeyPair">The keypair containing the private key of the advertised <see cref="sId"/></param>
        public void Sign(KeyPair singKeyPair)
        {
            this.signature = new Signature()
            { algorythm = EncryptionController.Instance.SigningAlgorythm };
            this.signature.GenerateSignature(SignableContent, singKeyPair);
        }

        /// <summary>
        /// Validates that this message was signed by the private part of the given publicKey
        /// </summary>
        /// <param name="publicKey">The public key of the sending resource</param>
        public bool ValidateSignature(byte[] publicKey)
        {
            if (signature == null)
            {
                throw new SignatureInvalidException("No signature was set");
            }
            return this.signature.ValidateSignature(SignableContent, publicKey);
        }


        [IgnoreMember]
        public byte[] SignableContent
        {
            get
            {
                return IEncryption.ConcatBytes(
                    message,
                    type != null ? Encoding.UTF8.GetBytes(type) : null,
                    sId.AsByte,
                    rId.AsByte,
                    BitConverter.GetBytes(mId),
                    headers?.Serialized
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


