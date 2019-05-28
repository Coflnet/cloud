using MessagePack;
using System;
using System.Runtime.Serialization;
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
	public class MessageData
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
		public string t;

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
				if(_coreInstance == null){
					UnityEngine.Debug.Log("falling back to default");
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
		/// <typeparam name="C">The 1st type parameter.</typeparam>
		/// <typeparam name="T">The 2nd type parameter.</typeparam>
		public static MessageData CreateMessageData<C, T>(SourceReference target,T data,  long m_id = 0) where C : Command
		{
			return new MessageData(target, m_id, MessagePackSerializer.Serialize<T>(data), System.Activator.CreateInstance<C>().GetSlug());
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
			this.t = type;
		}

		public MessageData(MessageData data) : this(data.rId, data.mId, data.message, data.t)
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
		public MessageData(Command command, byte[] m, long m_id = 0) : this(new SourceReference(), m_id, m, command.GetSlug())
		{

		}


		public MessageData(string type, byte[] data)
		{
			this.t = type;
			this.message = data;
		}

		public MessageData(SourceReference rId, byte[] message, string t)
		{
			this.rId = rId;
			this.message = message;
			this.t = t;
		}

		#endregion

		/// <summary>
		/// Gets the deserialized content of this message, cached the deserialized object
		/// </summary>
		/// <returns>The as.</returns>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public virtual T GetAs<T>()
		{
			if (Object.ReferenceEquals(null, deserialized))
			{
				if (message.Length == 0)
				{
					throw new System.InvalidOperationException($"Could not get data as {nameof(T)} because it is empty");
				}
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
		public void SerializeAndSet<T>(T ob)
		{
			message = Serialize<T>(ob);
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

		/// <summary>
		/// The user who sent the message, may be not present on the current server!
		/// </summary>
		/// <value>The user.</value>
		[IgnoreMember]
		[System.Obsolete("use the Reference instead")]
		public CoflnetUser User
		{
			get
			{
				return null;//UserController.instance.GetUser(sId);
			}
		}
		[IgnoreMember]
		public Reference<CoflnetUser> UserReference
		{
			get
			{
				return new Reference<CoflnetUser>(new SourceReference());
			}
		}

		public void SetCommand<C>() where C : Command, new()
		{
			this.t = (new C()).GetSlug();
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
			return string.Format("[MessageData: sId={0}, rId={1}, mId={2}, t={3}, Data={4}]", sId, rId, mId, t, Data);
		}

		/// <summary>
		/// Sends the command back.
		/// </summary>
		/// <param name="data">Data.</param>
		public virtual void SendBack(MessageData data)
		{
			data.rId = this.sId;
			CoreInstance.SendCommand(data);
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
	}
}


