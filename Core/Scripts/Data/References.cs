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
    public struct EntityId
    {
        [Key(0)]
        [DataMember]
        public readonly long ServerId;
        [DataMember]
        [Key(1)]
        public readonly long LocalId;


        public static readonly EntityId Default = new EntityId(0, 0);



        /// <summary>
        /// Initializes a new instance of the <see cref="T:Coflnet.Server.SourceReference"/> struct.
        /// Recomended constructor since arguments can't be switched
        /// </summary>
        /// <param name="server">Server which contains the resource.</param>
        /// <param name="resourceId">Entity identifier on that server.</param>
        public EntityId(CoflnetServer server, long resourceId) : this(server.ServerId, resourceId) { }


        public EntityId(string asString) : this(Encoding.UTF8.GetBytes(asString))
        {

        }


        /// <summary>
        /// Convertes the string representation of a <see cref="Coflnet.EntityId"/> into its
        /// </summary>
        /// <param name="s"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static bool TryParse(string s, out EntityId reference)
        {
            var parts = s.Split('.');
            long serverId;
            long resourceId;

            reference = default(EntityId);

            if (!long.TryParse(parts[0], out serverId))
            {
                return false;
            }

            if (!long.TryParse(parts[1], out resourceId))
            {
                return false;
            }

            reference = new EntityId(serverId, resourceId);

            return true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Coflnet.Server.SourceReference"/> struct.
        /// </summary>
        /// <param name="serverId">Server identifier.</param>
        /// <param name="resourceId">Entity identifier.</param>
        [SerializationConstructor]
        public EntityId(long serverId, long resourceId)
        {
            this.ServerId = serverId;
            this.LocalId = resourceId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Coflnet.Server.SourceReference"/> struct from a byte array.
        /// </summary>
        /// <param name="asByte">As byte.</param>
        public EntityId(byte[] asByte)
        {
            if (asByte.Length < 16)
            {
                throw new CoflnetException("referencid_invalid", "The reference Id passed is to short");
            }
            try
            {
                ServerId = BitConverter.ToInt64(asByte, 0);
                LocalId = BitConverter.ToInt64(asByte, 8);
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
        /// <param name="resourceId">Entity identifier.</param>
        public EntityId(int region, int location, ushort server, long resourceId)
        {
            if (region > 1 << 24 && region >= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(region), "has to be in the range of 0 to 2^24");
            }

            if (location > 1 << 24 && region >= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(location), "has to be in the range of 0 to 2^24");
            }


            this.LocalId = resourceId;
            // set the server with offset
            this.ServerId = (((long)region) << 40) + ((long)location << 16) + server;
        }


        public override int GetHashCode()
        {
            return (ServerId * LocalId).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            var other = (EntityId)obj;
            return other.ServerId == ServerId && other.LocalId == LocalId;
        }


        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:Coflnet.Server.SourceReference"/>.
        /// </summary>
        /// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:Coflnet.Server.SourceReference"/>.</returns>

        public override string ToString()
        {
            var localId = Convert.ToString(LocalId, 10);
            if (localId.Length > 8)
                localId = localId.Insert(8, "-");
            return $"{Convert.ToString(ServerId, 16)}:{localId}";
        }

        /// <summary>
        /// Returns the raw bytes of this <see cref="Coflnet.EntityId"/>. 
        /// 16 in total, long(8) <see cref="ServerId"/> and long(8) <see cref="LocalId"/>
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
                Buffer.BlockCopy(BitConverter.GetBytes(LocalId), 0, byteRepresentation, 8, 8);
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
            get
            {
                return ServerId == 0;
            }
        }

        /// <summary>
        /// Executes a command on the server containing the resource referenced by this object
        /// </summary>
        /// <param name="data">Command data to send</param>
        public void ExecuteForEntity(CommandData data)
        {
            data.Recipient = this;
            CoflnetCore.Instance.SendCommand(data);
        }

        /// <summary>
        /// Executes a command for a resource.
        /// </summary>
        /// <param name="data">Data to send as arguments.</param>
        /// <typeparam name="C">Command which to execute.</typeparam>
        /// <typeparam name="D">Type of the data.</typeparam>
        public void ExecuteForEntity<C, D>(D data) where C : Command
        {
            CoflnetCore.Instance.SendCommand<C, D>(this, data);
        }


        public static bool operator ==(EntityId sr1, EntityId sr2)
        {
            return sr1.Equals(sr2);
        }

        public static bool operator !=(EntityId sr1, EntityId sr2)
        {
            return !sr1.Equals(sr2);
        }

        /// <summary>
        /// Is the second server in the same location as this one.
        /// </summary>
        /// <returns><c>true</c>, if the resource is in the same location <c>false</c> otherwise.</returns>
        /// <param name="sourceReference">Source referenceto some resource.</param>
        public bool IsSameLocationAs(EntityId sourceReference)
        {
            return this.ServerId / 65536 == sourceReference.ServerId / 65536;
        }

        [IgnoreMember]
        [IgnoreDataMember]
        public bool IsServer
        {
            get
            {
                return LocalId == 0;
            }
        }

        [IgnoreMember]
        [IgnoreDataMember]
        public EntityId FullServerId
        {
            get
            {
                return new EntityId(this.ServerId, 0);
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


        public static EntityId NextLocalId
        {
            get
            {
                return new EntityId(0, ThreadSaveIdGenerator.NextId);
            }
        }
    }
}
