using System.Collections.Generic;
using System;
using System.Runtime.Serialization;
using System.Collections.Concurrent;

namespace Coflnet
{
	public class ReferenceManager
	{
		protected ConcurrentDictionary<SourceReference, Reference<Referenceable>> references = new ConcurrentDictionary<SourceReference, Reference<Referenceable>>();
		protected static ReferenceManager instance;
		protected CoflnetServer CurrentServer = new CoflnetServer(011111);

		public static ReferenceManager Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new ReferenceManager();
				}
				return instance;
			}
		}

		public ConcurrentDictionary<SourceReference, Reference<Referenceable>> References
		{
			get
			{
				return references;
			}
		}

		/// <summary>
		/// Gets all Referenced objects for a server.
		/// Needed for Recoverys
		/// </summary>
		/// <returns>The for server.</returns>
		/// <param name="id">Identifier.</param>
		public List<Reference<Referenceable>> GetForServer(long id)
		{

			List<Reference<Referenceable>> storedReferences = new List<Reference<Referenceable>>();

			foreach (var item in this.references)
			{
				//  if (item.Value.)
				if (item.Value.ReferenceId.ServerId == id)
				{
					storedReferences.Add(item.Value);
				}
			}

			return storedReferences;
		}
		/*
		/// <summary>
		/// Gets a resource for the current server.
		/// </summary>
		/// <returns>The resource if found.</returns>
		/// <param name="id">Identifier.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T GetResource<T>(long id)
		{
			return GetResource<T>(new SourceReference(Coflnet.Server.ServerController.ServerInstance.CurrentServer.ServerId, id));
		}
        */



		public Referenceable GetResource(SourceReference id)
		{
			return GetResource<Referenceable>(id);
		}


		/// <summary>
		/// Gets the resource.
		/// </summary>
		/// <returns>The resource.</returns>
		/// <param name="id">Identifier.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T GetResource<T>(SourceReference id) where T : Referenceable
		{
			Reference<Referenceable> reference;
			references.TryGetValue(id, out reference);

			if (reference == null || reference.Resource == null)

				throw new ObjectNotFound(id);

			if (reference.Resource is T)
			{
				return (T)reference.Resource;
			}
			try
			{
				return (T)Convert.ChangeType(reference, typeof(T));
			}
			catch (InvalidCastException)
			{
				return default(T);
			}
		}


		/// <summary>
		/// Wherether or not a given resource exists locally
		/// </summary>
		/// <returns>The contains.</returns>
		/// <param name="id">Identifier to search for.</param>
		public bool Contains(SourceReference id)
		{
			return references.ContainsKey(id);
		}

		/// <summary>
		/// Creates a new reference.
		/// </summary>
		/// <returns>The reference identifier.</returns>
		/// <param name="referenceable">Referencable object to store.</param>
		public SourceReference CreateReference(Referenceable referenceable, bool force = false)
		{
			long nextIndex = ThreadSaveIdGenerator.NextId;
			SourceReference newReference = new SourceReference(CurrentServer, nextIndex);
			Reference<Referenceable> referenceObject = new Reference<Referenceable>(referenceable, newReference);
			if (referenceable.Id != new SourceReference() && !force)
				throw new Exception("The referenceable already had an id, it may already have been registered. Call with force = true to ignore");
			referenceable.Id = newReference;
			if (!AddReference(referenceObject))
				throw new Exception("adding failed");
			return newReference;
		}

		/// <summary>
		/// Adds a reference and maybe its data to the reference dictionary.
		/// </summary>
		/// <returns><c>true</c>, if reference was added, <c>false</c> otherwise.</returns>
		/// <param name="reference">Reference.</param>
		public bool AddReference(Reference<Referenceable> reference)
		{
			return references.TryAdd(reference.ReferenceId, reference);
		}

		public bool AddReference(Referenceable referenceable)
		{
			return references.TryAdd(referenceable.Id, new Reference<Referenceable>(referenceable));
		}


		public void ExecuteForReference(MessageData data)
		{
			Reference<Referenceable> value;
			references.TryGetValue(data.rId, out value);
			if (value == null)
			{
				if (data.sId.ServerId != ConfigController.ApplicationSettings.id.ServerId)
				{
					// we are not the main managing server, pass it on to it
					CoflnetCore.Instance.SendCommand(data);
					return;
				}
				throw new ObjectNotFound(data.rId);
			}
			var resource = value.GetAsIfPresent<Referenceable>();
			if (resource != null)
			{
				var controller = resource.GetCommandController();
				var command = controller.GetCommand(data.t);
				controller.ExecuteCommand(command, data);
				if (!command.Settings.IsChaning)
				{
					return;
				}
			}
			value.ExecuteForResource(data);
		}

		public class ObjectNotFound : CoflnetException
		{
			public ObjectNotFound(SourceReference id) : base("object_not_found", $"The resource {id} wasn't found on this server", null, 404)
			{
			}
		}
	}

	/// <summary>
	/// Defines objects that are Referenceable across servers.
	/// Only use for larger objects.
	/// </summary>
	[DataContract]
	public abstract class Referenceable
	{
		[DataMember]
		public SourceReference Id;

		[DataMember]
		public Access Access;


		protected Referenceable(Access access, SourceReference id)
		{
			this.Id = id;
			this.Access = access;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.Server.Referenceable"/> class.
		/// </summary>
		/// <param name="owner">Owner creating this resource.</param>
		protected Referenceable(SourceReference owner) : this()
		{
			this.Access = new Access(owner);
		}

		protected Referenceable()
		{

			this.Id = ReferenceManager.Instance.CreateReference(this);
		}

		/// <summary>
		/// Checks if a certain resource is allowed to access this one and or execute commands
		/// </summary>
		/// <returns><c>true</c>, if allowed access, <c>false</c> otherwise.</returns>
		/// <param name="requestingReference">Requesting reference.</param>
		/// <param name="mode">Mode.</param>
		public virtual bool IsAllowedAccess(SourceReference requestingReference, AccessMode mode = AccessMode.READ)
		{
			return (Access != null) && Access.IsAllowedToAccess(requestingReference, mode);
		}


		public virtual void ExecuteCommand(MessageData data)
		{
			var controller = GetCommandController();
			var command = controller.GetCommand(data.t);

			AccessMode mode = AccessMode.READ;
			if (!command.Settings.ThreadSave)
			{
				mode = AccessMode.WRITE;
			}

			if (!IsAllowedAccess(data.sId, mode))
				throw new CoflnetException("access_denied", "The executing resource doesn't have rights to execute that command on this resource");

			controller.ExecuteCommand(command, data);
		}

		public abstract CommandController GetCommandController();
	}


	[DataContract]
	public class Access
	{
		[DataMember]
		public SourceReference Owner;

		/// <summary>
		/// enum values are given by
		/// same server, same location, everybody
		/// 1 = read
		/// 2 = write
		/// 4 = change access
		/// For any combination add them together
		/// </summary>
		public enum GeneralAccess
		{
			ALL_READ = 1,
			LOCAL_READ = 10,
			SERVER_READ = 100,
			ALL_WRITE = 2,
			LOCAL_WRITE = 20,
			ALL_READ_LOCAL_WRITE = 21,
			SERVER_WRITE = 200,
			ALL_READ_AND_WRITE = 3,
			LOCAL_READ_AND_WRITE = 30,
			ALL_READ_LOCAL_READ_AND_WRITE = 31,
			SERVER_READ_AND_WRITE = 300,
			ALL_READ_SERVER_READ_AND_WRITE = 301
		}
		[DataMember]
		public GeneralAccess generalAccess;
		[DataMember]
		public Dictionary<SourceReference, AccessMode> resourceAccess;
		/// <summary>
		/// SourceReferences which want to be notified if this resource changes
		/// </summary>
		/// <value>The subscribers.</value>
		[DataMember]
		public List<SourceReference> Subscribers { get; set; }




		public Access(SourceReference owner)
		{
			Owner = owner;
		}

		/// <summary>
		/// Checks if some reference has some level of access to this resource
		/// </summary>
		/// <returns><c>true</c>, if allowed to access, <c>false</c> otherwise.</returns>
		/// <param name="requestingReference">Requesting reference.</param>
		/// <param name="mode">AccessMode.</param>
		public bool IsAllowedToAccess(SourceReference requestingReference, AccessMode mode = AccessMode.READ)
		{
			// is it the owner
			if (requestingReference == Owner)
				return true;

			//is there a special case?
			if (resourceAccess != null && resourceAccess.ContainsKey(requestingReference))
			{
				return resourceAccess[requestingReference].CompareTo(mode) >= 0;
			}

			// check for "all"
			if ((int)mode - (int)generalAccess % 10 >= 0)
				return true;
			// check for local
			if ((int)mode * 10 - (int)generalAccess >= 0)
				return requestingReference.IsSameLocationAs(Owner);
			// check for same server
			if ((int)mode * 100 - (int)generalAccess >= 0)
				return Owner.ServerId == requestingReference.ServerId;

			// requestingReference doesn't have access
			return false;
		}

		public void Authorize(SourceReference sourceReference, AccessMode mode = AccessMode.READ)
		{
			if (resourceAccess == null)
				resourceAccess = new Dictionary<SourceReference, AccessMode>();
			resourceAccess[sourceReference] = mode;
		}
	}


	public enum AccessMode
	{
		NONE = 0,
		READ = 1,
		WRITE = 2,
		READ_AND_WRITE = 3,
		/// <summary>
		/// Permission to change others permission, includes read and write
		/// </summary>
		CHANGE_PERMISSIONS = 4
	}

	/// <summary>
	/// Defines an object that is Referenceable and has to have a local copy.
	/// Use this very carefully.
	/// </summary>
	public abstract class ILocalReferenceable : Referenceable
	{

	}



	[DataContract(Name = "ref", Namespace = "")]
	public class Reference<T> where T : Referenceable
	{
		[DataMember(Name = "id")]
		protected SourceReference referenceId;
		[IgnoreDataMember]
		protected T resource;

		/// <summary>
		/// Executes a command on the server containing the resource referenced by this object
		/// </summary>
		/// <param name="data">Command data to send</param>
		public void ExecuteForResource(MessageData data)
		{
			ServerController.Instance.SendCommandToServer(data, ReferenceId.ServerId);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.Server.Reference`1"/> class and ads it to the ReferenceManager.
		/// </summary>
		/// <param name="resource">Resource.</param>
		public Reference(T resource, SourceReference referenceId)
		{
			this.resource = resource;
			this.referenceId = referenceId;
			//referenceId = ReferenceManager.Instance.AddReference(resource);
		}

		public Reference(T resource)
		{
			this.resource = resource;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.Server.Reference`1"/> class without actually knowing the data.
		/// </summary>
		/// <param name="referenceId">Reference identifier.</param>
		public Reference(SourceReference referenceId)
		{
			this.referenceId = referenceId;
		}

		public Reference()
		{

		}

		[IgnoreDataMember]
		public T Resource
		{
			get
			{
				return resource;
			}
		}
		[IgnoreDataMember]
		public SourceReference ReferenceId
		{
			get
			{
				return referenceId;
			}
		}

		/// <summary>
		/// Gets the referenced resource as some type if present on the server, null otherwise
		/// </summary>
		/// <returns>The referenced data as specific object.</returns>
		/// <typeparam name="Y">The 1st type parameter.</typeparam>
		public Y GetAsIfPresent<Y>() where Y : class
		{
			return resource as Y;
		}

		/// <summary>
		/// Is this instance of type Y.
		/// </summary>
		/// <returns>If this reference contains a resource with type Y.</returns>
		/// <typeparam name="Y">The type parameter to test against.</typeparam>
		public bool Is<Y>()
		{
			return resource is Y;
		}
	}

	/// <summary>
	/// Redundant reference Data exists on one or more servers (recomended)
	/// </summary>
	public class RedundantReference<T> : Reference<T> where T : Referenceable
	{
		protected List<CoflnetServer> failoverServers = new List<CoflnetServer>();

		/// <summary>
		/// Adds a new server for extra redundancy.
		/// </summary>
		/// <param name="server">Server to add.</param>
		public void AddServer(CoflnetServer server)
		{
			failoverServers.Add(server);
		}


		public void RemoveServer(CoflnetServer server)
		{
			failoverServers.Remove(server);
		}

		/// <summary>
		/// Executes a command on the server containing the resource referenced by this object
		/// </summary>
		/// <param name="data">Command data to send</param>
		public new void ExecuteForResource(MessageData data)
		{
			ServerController.Instance.SendCommandToServer(data, referenceId.ServerId);
			// also send it to the failover servers
			foreach (var item in failoverServers)
			{
				ServerController.Instance.SendCommandToServer(data, item);
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:Coflnet.Server.RedundantReference`1"/> s Resource is lokally available.
		/// </summary>
		/// <value><c>true</c> if is lokal; otherwise, <c>false</c>.</value>
		public bool IsLokal
		{
			get
			{

				return ReferenceManager.Instance.Contains(this.referenceId);
			}
		}

		/// <summary>
		/// Gets the nearest server which has a copy of this resource.
		/// </summary>
		/// <value>The nearest server.</value>
		public CoflnetServer NearestServer
		{
			get
			{
				CoflnetServer closest = ServerController.Instance.GetOrCreate(referenceId.ServerId);
				foreach (var item in failoverServers)
				{
					// if another server that is closer or as fast as the managing server use it instead
					if (item.PingTimeMS <= closest.PingTimeMS)
						closest = item;
				}
				return closest;
			}
		}

		public RedundantReference(T resource, SourceReference resourceId, params CoflnetServer[] failoverServer) : base(resource, resourceId)
		{
			if (failoverServer == null)
				return;
			this.failoverServers.AddRange(failoverServers);
		}
	}


	public class InstanceAllreadyExistsException : Exception
	{
		public InstanceAllreadyExistsException(string message = "There already is an Instance of this class, there can only be one") : base(message)
		{
		}
	}
}

