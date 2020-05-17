using MessagePack;
using System;
using System.Text;
using MessagePack.Formatters;
using System.Runtime.Serialization;

namespace Coflnet
{

	/// <summary>
	/// Identifier of some resource on some server
	/// </summary>
	//[MessagePackFormatter(typeof(CustomObjectFormatter))]
	[MessagePackObject]
	[DataContract]
	public struct SourceReference
	{
		[Key(0)]
		[DataMember]
		public readonly long ServerId;
		[DataMember]
		[Key(1)]
		public readonly long ResourceId;


		public static readonly SourceReference Default = new SourceReference(0, 0);



		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.Server.SourceReference"/> struct.
		/// Recomended constructor since arguments can't be switched
		/// </summary>
		/// <param name="server">Server which contains the resource.</param>
		/// <param name="resourceId">Resource identifier on that server.</param>
		public SourceReference(CoflnetServer server, long resourceId) : this(server.ServerId, resourceId) { }


		public SourceReference(string asString) : this(Encoding.UTF8.GetBytes(asString))
		{

		}


		/// <summary>
		/// Convertes the string representation of a <see cref="SourceReference"/> into its
		/// </summary>
		/// <param name="s"></param>
		/// <param name="reference"></param>
		/// <returns></returns>
		public static bool TryParse(string s,out SourceReference reference)
		{
			var parts = s.Split('.');
			long serverId;
			long resourceId;

			reference = default(SourceReference);

			if(!long.TryParse(parts[0],out serverId))
			{
				return false;
			}

			if(!long.TryParse(parts[1],out resourceId))
			{
				return false;
			}

			reference = new SourceReference(serverId,resourceId);

			return true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.Server.SourceReference"/> struct.
		/// </summary>
		/// <param name="serverId">Server identifier.</param>
		/// <param name="resourceId">Resource identifier.</param>
		[SerializationConstructor]
		public SourceReference(long serverId, long resourceId)
		{
			this.ServerId = serverId;
			this.ResourceId = resourceId;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.Server.SourceReference"/> struct from a byte array.
		/// </summary>
		/// <param name="asByte">As byte.</param>
		public SourceReference(byte[] asByte)
		{
			if (asByte.Length < 16)
			{
				throw new CoflnetException("referencid_invalid", "The reference Id passed is to short");
			}
			try
			{
				ServerId = BitConverter.ToInt64(asByte, 0);
				ResourceId = BitConverter.ToInt64(asByte, 8);
			}
			catch (Exception)
			{
				throw new CoflnetException("referencid_invalid", "The reference Id passed is invalid");
			}
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.SourceReference"/> struct.
		/// </summary>
		/// <param name="region">ServerRegion.</param>
		/// <param name="location">ServerLocation.</param>
		/// <param name="server">Server id relative to the location.</param>
		/// <param name="resourceId">Resource identifier.</param>
		public SourceReference(int region, int location, ushort server, long resourceId)
		{
			if (region > 1 << 24 && region >= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(region), "has to be in the range of 0 to 2^24");
			}

			if (location > 1 << 24 && region >= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(location), "has to be in the range of 0 to 2^24");
			}


			this.ResourceId = resourceId;
			// set the server with offset
			this.ServerId = (((long)region) << 40) + ((long)location << 16) + server;
		}


		public override int GetHashCode()
		{
			return (ServerId * ResourceId).GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
			{
				return false;
			}
			var other = (SourceReference)obj;
			return other.ServerId == ServerId && other.ResourceId == ResourceId;
		}


		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:Coflnet.Server.SourceReference"/>.
		/// </summary>
		/// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:Coflnet.Server.SourceReference"/>.</returns>

		public override string ToString()
		{
			return $"{Convert.ToString(ServerId, 16)}.{Convert.ToString(ResourceId, 16)}";
		}

		/// <summary>
		/// Returns the raw bytes of this <see cref="SourceReference"/>. 
		/// 16 in total, long(8) <see cref="ServerId"/> and long(8) <see cref="ResourceId"/>
		/// </summary>
		/// <value></value>
		[IgnoreMember]
		[IgnoreDataMember]
		public byte[] AsByte
		{
			get
			{
				byte[] byteRepresentation = new byte[16];
				Buffer.BlockCopy(BitConverter.GetBytes(ServerId), 0, byteRepresentation, 0, 8);
				Buffer.BlockCopy(BitConverter.GetBytes(ResourceId), 0, byteRepresentation, 8, 8);
				return byteRepresentation;
			}
		}


		/// <summary>
		/// Wherether or not this Reference was created offline/is local
		/// </summary>
		/// <value></value>
		[IgnoreMember]
		[IgnoreDataMember]
		public bool IsLocal
		{
			get{
				return ServerId == 0;
			}
		}

		/// <summary>
		/// Executes a command on the server containing the resource referenced by this object
		/// </summary>
		/// <param name="data">Command data to send</param>
		public void ExecuteForResource(MessageData data)
		{
			data.rId = this;
			CoflnetCore.Instance.SendCommand(data);
		}

		/// <summary>
		/// Executes a command for a resource.
		/// </summary>
		/// <param name="data">Data to send as arguments.</param>
		/// <typeparam name="C">Command which to execute.</typeparam>
		/// <typeparam name="D">Type of the data.</typeparam>
		public void ExecuteForResource<C, D>(D data) where C : Command
		{
			CoflnetCore.Instance.SendCommand<C, D>(this, data);
		}


		public static bool operator ==(SourceReference sr1, SourceReference sr2)
		{
			return sr1.Equals(sr2);
		}

		public static bool operator !=(SourceReference sr1, SourceReference sr2)
		{
			return !sr1.Equals(sr2);
		}

		/// <summary>
		/// Is the second server in the same location as this one.
		/// </summary>
		/// <returns><c>true</c>, if the resource is in the same location <c>false</c> otherwise.</returns>
		/// <param name="sourceReference">Source referenceto some resource.</param>
		public bool IsSameLocationAs(SourceReference sourceReference)
		{
			return this.ServerId / 65536 == sourceReference.ServerId / 65536;
		}

		[IgnoreMember]
		[IgnoreDataMember]
		public bool IsServer
		{
			get
			{
				return ResourceId == 0;
			}
		}

		[IgnoreMember]
		[IgnoreDataMember]
		public SourceReference FullServerId
		{
			get{
				return new SourceReference(this.ServerId,0);
			}
		}

		[IgnoreMember]
		[IgnoreDataMember]
		public int Region
		{
			get
			{
				return (int)(ServerId >> 40);
			}
		}

		/// <summary>
		/// Gets the location relative to the region.
		/// </summary>
		/// <value>The location relative to region.</value>
		[IgnoreMember]
		[IgnoreDataMember]
		public int LocationInRegion
		{
			get
			{
				return (int)((ServerId >> 16) % (1 << 24));
			}
		}

		[IgnoreMember]
		[IgnoreDataMember]
		public ushort ServerRelativeToLocation
		{
			get
			{
				return (ushort)((ServerId) % (1 << 16));
			}
		}


		public static SourceReference NextLocalId
		{
			get
			{
				return new SourceReference(0,ThreadSaveIdGenerator.NextId);
			}
		}
	}
}
