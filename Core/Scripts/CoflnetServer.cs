using System.Collections;
using System.Collections.Generic;
using MessagePack;

namespace Coflnet
{

	[MessagePackObject]
	public class CoflnetServer //: Referenceable
	{
		[Key(0)]
		private long pId;
		[IgnoreMember]
		private string ip;
		[IgnoreMember]
		private long lastPong;
		/// <summary>
		/// The average ping time in ms of this server.
		/// </summary>
		[IgnoreMember]
		private long pingTimeMS;
		[IgnoreMember]
		private byte[] publicKey;
		/// <summary>
		/// Connection to this server
		/// </summary>
		[IgnoreMember]
		protected ICommandTransmit connection;
		public enum ServerRole
		{
			master,
			file,
			worker,
			db,
			cache,
			socket,
			recover,
			backup,
			relay,
			proxy,
			game,
			reserved
		}
		[IgnoreMember]
		protected List<ServerRole> roles;

		/// <summary>
		/// Server status
		/// </summary>
		public enum ServerState
		{
			/// <summary>
			/// Server status is unknown
			/// </summary>
			UNKNOWN,
			/// <summary>
			/// Server is temporaly not available
			/// </summary>
			DOWN,
			/// <summary>
			/// Server is restarting
			/// </summary>
			RESTARTING,
			/// <summary>
			/// Server is up and running, all fine
			/// </summary>
			UP,
			/// <summary>
			/// Server is about to be shut down
			/// </summary>
			SHUTTING_DOWN,
			/// <summary>
			/// This server is dead forever :(
			/// </summary>
			DEAD
		}
		/// <summary>
		/// Last recorded state
		/// </summary>
		[IgnoreMember]
		protected ServerState state;

		/// <summary>
		/// Gets the public identifier of this server as string
		/// </summary>
		/// <value>The public identifier.</value>
		[IgnoreMember]
		public string PublicId
		{
			get
			{
				return pId.ToString();
			}
		}

		/// <summary>
		/// Gets the server identifier as long.
		/// </summary>
		/// <value>The server identifier.</value>
		[IgnoreMember]
		public long ServerId
		{
			get
			{
				return pId;
			}
		}


		[IgnoreMember]
		public string Ip
		{
			get
			{
				return ip;
			}
		}

		[IgnoreMember]
		public long LastPong
		{
			get
			{
				return lastPong;
			}
		}

		/// <summary>
		/// Gets the latest or average ping time in ms.
		/// Useful for calculating how near the server is.
		/// </summary>
		/// <value>The ping time ms.</value>
		[IgnoreMember]
		public long PingTimeMS
		{
			get
			{
				return pingTimeMS;
			}
		}

		[IgnoreMember]
		public byte[] PublicKey
		{
			get
			{
				return publicKey;
			}
		}

		[IgnoreMember]
		public ServerState State
		{
			get
			{
				return state;
			}
		}

		/// <summary>
		/// Gets or sets the connection to this server.
		/// </summary>
		/// <value>The connection.</value>
		[IgnoreMember]
		public ICommandTransmit Connection
		{
			get
			{
				return connection;
			}
			set
			{
				connection = value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:CoflnetServer"/> class.
		/// 
		/// </summary>
		/// <param name="publicId">Public identifier of this server.</param>
		public CoflnetServer(long publicId)
		{
			this.pId = publicId;
			this.pingTimeMS = long.MaxValue;
			// TODO: try to load the server info from the master
		}

		public CoflnetServer(long publicId, string ip, long lastPong, byte[] publicKey)
		{
			this.pId = publicId;
			this.ip = ip;
			this.lastPong = lastPong;
			this.publicKey = publicKey;
		}

		public CoflnetServer(long pId, string ip, byte[] publicKey, List<ServerRole> roles, ServerState state)
		{
			this.pId = pId;
			this.ip = ip;
			this.publicKey = publicKey;
			this.roles = roles;
			this.state = state;
		}

		public static implicit operator List<object>(CoflnetServer v)
		{
			throw new System.NotImplementedException();
		}

	}
}