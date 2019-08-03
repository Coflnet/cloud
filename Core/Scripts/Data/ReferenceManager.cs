using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Coflnet {
	public class ReferenceManager {

		protected ConcurrentDictionary<SourceReference, InnerReference<Referenceable>> references = new ConcurrentDictionary<SourceReference, InnerReference<Referenceable>> ();
		protected static ReferenceManager instance;
		//protected CoflnetServer CurrentServer = new CoflnetServer(011111);

		/// <summary>
		/// Name of folder to store resources in that are unloaded from memory or persisted
		/// </summary>
		/// <value>Name of folder</value>
		public string RelativeStorageFolder { get; }

		/// <summary>
		/// The core Instance used for sending commands and getting the correct deviceId
		/// </summary>
		public CoflnetCore coreInstance;

		public static ReferenceManager Instance {
			get {
				if (instance == null) {
					instance = new ReferenceManager ();
				}
				return instance;
			}
		}

		public ReferenceManager () : this ("res") { }

		/// <summary>
		/// Creates a new instance of the ReferenceManager
		/// </summary>
		/// <param name="relativeStorageFolder">Relative sub-Directory to the data directory to store references and resources in</param>
		public ReferenceManager (string relativeStorageFolder) {
			RelativeStorageFolder = relativeStorageFolder;
		}

		public ConcurrentDictionary<SourceReference, InnerReference<Referenceable>> References {
			get {
				return references;
			}
		}

		/// <summary>
		/// Gets all Referenced objects for a server.
		/// Needed for Recoverys
		/// </summary>
		/// <returns>The for server.</returns>
		/// <param name="id">Identifier.</param>
		public List<InnerReference<Referenceable>> GetForServer (long id) {

			List<InnerReference<Referenceable>> storedReferences = new List<InnerReference<Referenceable>> ();

			foreach (var item in this.references) {
				//  if (item.Value.)
				if (item.Value.Resource.Id.ServerId == id) {
					storedReferences.Add (item.Value);
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

		public Referenceable GetResource (SourceReference id) {
			return GetResource<Referenceable> (id);
		}

		/// <summary>
		/// Serializes the <see cref="Referenceable"/> without the access attribute.
		/// </summary>
		/// <returns>The without access.</returns>
		/// <param name="id">Identifier.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public byte[] SerializeWithoutAccess<T> (SourceReference id) where T : Referenceable {
			return SerializeWithoutAccess<T> (id, CoflnetEncoder.Instance);
		}

		/// <summary>
		/// Serializes the <see cref="Referenceable"/> without the access attribute.
		/// </summary>
		/// <returns>The without access.</returns>
		/// <param name="id">Identifier.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public byte[] SerializeWithoutAccess (SourceReference id) {
			var data = GetResource (id);
			byte[] result;
			// NOTE: this could still cause trouble if Access would be accessed in a different Thread
			lock (data) {
				Access access = data.Access;
				data.Access = null;
				result = MessagePack.MessagePackSerializer.Typeless.Serialize (data);
				data.Access = access;
			}
			return result;
		}

		/// <summary>
		/// Save the specified <see cref="Referenceable"/> and remove it from Ram.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="removeFromRam">If set to <c>true</c> remove from ram.</param>
		public void Save (SourceReference id, bool removeFromRam = false) {

			//ValuesController.SetValue
			var data = references[id];
			//MessagePack.MessagePackSerializer.ser
			var bytes = MessagePack.MessagePackSerializer.Typeless.Serialize (data);

			var h = new PersistHelper () {
				reference = data,
					data = data.Resource
			};

			//var rr = new RedundantReference<CoflnetUser>((CoflnetUser)data, ((CoflnetUser)data).Id, new CoflnetServer(11111, "", 12, new byte[16]));

			FileController.WriteAllBytes ($"res/{id.ToString()}-a", MessagePack.MessagePackSerializer.Typeless.Serialize (h));
			DataController.Instance.SaveData ($"{RelativeStorageFolder}/{id.ToString()}", bytes);

			if (!removeFromRam) return;

			InnerReference<Referenceable> reference;
			references.TryRemove (id, out reference);
		}

		class PersistHelper {
			public object data;
			public InnerReference<Referenceable> reference;
		}

		/// <summary>
		/// Serializes the <see cref="Referenceable"/> without the access attribute.
		/// </summary>
		/// <returns>The without access.</returns>
		/// <param name="id">Identifier.</param>
		/// <param name="encoder">Encoder.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public byte[] SerializeWithoutAccess<T> (SourceReference id, CoflnetEncoder encoder) where T : Referenceable {
			T data = GetResource<T> (id);
			byte[] result;
			// NOTE: this could still cause trouble if Access would be accessed in a different Thread
			lock (data) {
				Access access = data.Access;
				data.Access = null;
				result = encoder.Serialize<T> (data);
				data.Access = access;
			}
			return result;
		}

		/// <summary>
		/// Clones a resource locally 
		/// </summary>
		/// <param name="id">The id of the resource to clone</param>
		public void CloneResource(SourceReference id,Action<Referenceable> afterReceive, bool subscribe = false)
		{
			if(Exists(id)){
				// break if the resource is already cloned
				return;
			}

			UnityEngine.Debug.Log($"cloning {id}");

			coreInstance.SendCommand<GetResourceCommand,short>(id,0,o =>{
				var resource = MessagePack.MessagePackSerializer.Typeless.Deserialize(o.message) as Referenceable;
				this.AddReference(resource);
				afterReceive?.Invoke(resource);
			});

			
		}

		/// <summary>
		/// Gets the resource.
		/// </summary>
		/// <returns>The resource.</returns>
		/// <param name="id">Identifier.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T GetResource<T> (SourceReference id) where T : Referenceable {
			InnerReference<Referenceable> reference;

			if (!TryGetReference (id, out reference) || reference.Resource == null) {
				throw new ObjectNotFound (id);
			}


			if (reference.Resource is T) {
				return (T) reference.Resource;
			}
			UnityEngine.Debug.Log($"this is {reference.Resource.GetType().Name} not the requested type {typeof(T).Name}");
			try {
				return (T) Convert.ChangeType (reference, typeof (T));
			} catch (InvalidCastException) {
				return default (T);
			}
		}

		/// <summary>
		/// Tries to load the  reference from disc.
		/// </summary>
		/// <returns><c>true</c>, if load resource was tryed, <c>false</c> otherwise.</returns>
		/// <param name="id">Identifier.</param>
		/// <param name="data">Data.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public bool TryLoadReference (SourceReference id, out InnerReference<Referenceable> data, bool cache = true) {
			var path = $"res/{id.ToString()}";
			if (!FileController.Exists (path)) {
				data = null;
				return false;
			}
			var bytes = DataController.Instance.LoadData (path);
			var result = MessagePack.MessagePackSerializer.Typeless.Deserialize (bytes)
			as InnerReference<Referenceable>;
			if (cache) {
				references[id] = result;
			}
			data = result;
			return true;
		}

		/// <summary>
		/// Tries the get resource.
		/// </summary>
		/// <returns><c>true</c>, if get resource was tryed, <c>false</c> otherwise.</returns>
		/// <param name="id">Identifier.</param>
		/// <param name="data">Data.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public bool TryGetResource<T> (SourceReference id, out T data) where T : Referenceable {
			InnerReference<Referenceable> reference;
			TryGetReference (id, out reference);

			if (reference == null || reference.Resource == null) {
				data = null;
				return false;
			}

			data = GetResource<T> (id);
			return true;
		}

		/// <summary>
		/// Attempts to find the resource, will create it default if not found and set correct id
		/// </summary>
		/// <param name="id">Id to search for</param>
		/// <typeparam name="T">What type the resource is</typeparam>
		/// <returns>The found/create resource</returns>
		public T GetOrDefault<T> (SourceReference id) where T : Referenceable {
			T value;
			if (!TryGetResource<T> (id, out value)) {
				// create default
				value = (T) Activator.CreateInstance (typeof (T));
			}
			return value;
		}

		/// <summary>
		/// Returns resource if found, otherwise uses creator to create the new resource.
		/// Also sets correct id for it.
		/// </summary>
		/// <param name="id">To search or set</param>
		/// <param name="creator">Function wich will return a new instance</param>
		/// <typeparam name="T">What type the resource is</typeparam>
		/// <returns>The found/create resource</returns>
		public T GetOrCreate<T> (SourceReference id, Func<T> creator) where T : Referenceable {
			T value;
			if (!TryGetResource<T> (id, out value)) {
				// create default
				value = creator.Invoke ();
				value.Id = id;
			}
			return value;
		}

		/// <summary>
		/// Returns value if found, otherwise uses alterer to alter the created resource and returns it.
		/// </summary>
		/// <param name="id">Of resource to search for</param>
		/// <param name="alterer">Method that receives and returns the resource after creation</param>
		/// <typeparam name="T">What type the resource is</typeparam>
		/// <returns>The found/create and altered resource</returns>
		public T GetOrCreate<T> (SourceReference id, Func<T, T> alterer) where T : Referenceable {
			T value;
			if (!TryGetResource<T> (id, out value)) {
				// create default
				value = (T) Activator.CreateInstance (typeof (T));
				value.Id = id;
				value = alterer.Invoke (value);
			}
			return value;
		}

		/// <summary>
		/// Wherether or not a given resource exists locally
		/// </summary>
		/// <returns>The contains.</returns>
		/// <param name="id">Identifier to search for.</param>
		public bool Exists (SourceReference id) {
			return references.ContainsKey (id);
		}

		/// <summary>
		/// Creates a new reference.
		/// </summary>
		/// <returns>The reference identifier.</returns>
		/// <param name="referenceable">Referencable object to store.</param>
		public virtual SourceReference CreateReference (Referenceable referenceable, bool force = false) {
			long nextIndex = ThreadSaveIdGenerator.NextId;

			SourceReference newReference;
			// generate new Id for the current server
			if(this.coreInstance == null){
				newReference  = new SourceReference (0, nextIndex);
			} else {
				newReference  = new SourceReference (this.coreInstance.Id.ServerId, nextIndex);
			}
			if (referenceable.Id != new SourceReference () && !force)
				throw new Exception ("The referenceable already had an id, it may already have been registered. Call with force = true to ignore");
			referenceable.Id = newReference;

			InnerReference<Referenceable> referenceObject = new InnerReference<Referenceable> (referenceable);

			if (!AddReference (referenceObject))
				throw new Exception ("adding failed");
			return newReference;
		}

		/// <summary>
		/// Adds a reference and maybe its data to the reference dictionary.
		/// </summary>
		/// <returns><c>true</c>, if reference was added, <c>false</c> otherwise.</returns>
		/// <param name="reference">Reference.</param>
		public bool AddReference (InnerReference<Referenceable> reference) {
			return references.TryAdd (reference.Resource.Id, reference);
		}

		/// <summary>
		/// Adds a referenceable and to the reference dictionary.
		/// </summary>
		/// <param name="referenceable"></param>
		/// <returns><c>true</c>, if reference was added, <c>false</c> otherwise.</returns>
		public bool AddReference (Referenceable referenceable) {
			return AddReference (new InnerReference<Referenceable> (referenceable));
		}

		public bool TryGetReference (SourceReference id, out InnerReference<Referenceable> result) {
			InnerReference<Referenceable> reference;
			references.TryGetValue (id, out reference);

			if (reference == null) {
				if (!TryLoadReference (id, out reference, true)) {
					result = null;
					return false;
				}
			}

			// special references 
			// Redirects occur if offline ids were used
			if (reference is RedirectReference<Referenceable>) {
				var redirectReference = reference as RedirectReference<Referenceable>;
				UnityEngine.Debug.Log ("redirecting :)");
				return TryGetReference (redirectReference.newId, out result);
			}

			result = reference;
			return true;
		}

		/// <summary>
		/// Executes a command for a <see cref="SourceReference"/>.
		/// Will forward the command to the right server if the current one is not the main managing node of the resource.
		/// </summary>
		/// <param name="data">Command data to execute.</param>
		/// <param name="sender">The vertified sender of the message (for distribution purposes)</param>
		public void ExecuteForReference (MessageData data, SourceReference sender = default (SourceReference)) {

			if (data.rId.ServerId == 0) {
				// special case it is myself
				coreInstance.ExecuteCommand (data);
				return;
			}

			UnityEngine.Debug.Log ($"searching {coreInstance.Id} ({coreInstance.GetType().Name}) for {data.rId}");
			InnerReference<Referenceable> reference;
			TryGetReference (data.rId, out reference);

			if (reference == null) {
				UnityEngine.Debug.Log ("not found");

				// is the current server the managing server?
				if (data.rId.ServerId != ConfigController.ApplicationSettings.id.ServerId) {
					UnityEngine.Debug.Log ("passing on to " + data.rId);
					// we are not the main managing server, pass it on to it
					CoflnetCore.Instance.SendCommand (data);
					return;
				}
				// we are the main managing server but this resource doesn't exist
				throw new ObjectNotFound (data.rId);
			}

			UnityEngine.Debug.Log ("found executing");

			var resource = reference.Resource;

			if (resource != null) {
				//UnityEngine.Debug.Log ($" on {this.coreInstance.Id} ({coreInstance.GetType().Name}) executing {data} ");
				var command = resource.GetCommandController ().GetCommand (data.t);

				// only execute changing commands command if we are the managing server 
				// or the managing server instructed us to do so
				if (command.Settings.IsChaning &&
					(sender.ServerId == resource.Id.ServerId && sender.IsServer) ||
					resource.Id.ServerId == ConfigController.ApplicationSettings.id.ServerId) {
					resource.ExecuteCommand (data, command);
				} else if (!command.Settings.IsChaning) {
					// if the command isn't updating we are done here
					resource.ExecuteCommand (data, command);
					return;
				}
				//resource.ExecuteCommand()
				//controller.ExecuteCommand(command, data);

			}
			/// IMPORTANT
			/// this could cause a loop if we are one of the sibblings managing nodes and are 
			/// distributing this to the managing nodes and it will distribute it back
			UnityEngine.Debug.Log ("distributing");

			// block distributing command received from the managing node
			if ((sender.ServerId != data.sId.ServerId || sender.ResourceId != 0) && sender.ServerId != 0)
				reference.ExecuteForResource (data);
		}

		/// <summary>
		/// The Id of the managing node for a given reference
		/// </summary>
		/// <returns>The node for.</returns>
		/// <param name="reference">Reference.</param>
		protected SourceReference ManagingNodeFor (Reference<Referenceable> reference) {
			CoflnetServer server;
			TryGetResource<CoflnetServer> (new SourceReference (reference.ReferenceId.ServerId, 0), out server);

			if (server != null && server.State != CoflnetServer.ServerState.UP) {
				// search general failover servers
				foreach (var siblingServer in server.SibblingServers) {
					var id = new SourceReference (siblingServer, 0);
					if (IsUp (id)) {
						return id;
					}
				}

				/// try to find another server if main managing node is down
				if (reference.InnerReference is RedundantInnerReference<Referenceable>) {
					foreach (var sibbling in (reference.InnerReference as RedundantInnerReference<Referenceable>)
							.SiblingNodes
							.ConvertAll<SourceReference> ((id) => new SourceReference (id, 0))) {
						TryGetResource<CoflnetServer> (sibbling, out server);
						if (server.State == CoflnetServer.ServerState.UP) {
							return sibbling;
						}
					}
				}
			}
			return new SourceReference (reference.ReferenceId.ServerId, 0);
		}

		protected bool IsManagingNodeFor (SourceReference node, SourceReference resource) {
			if (node.ServerId == resource.ServerId)
				return true;

			CoflnetServer server;
			TryGetResource<CoflnetServer> (new SourceReference (node.ServerId, 0), out server);

			return false;
		}

		/// <summary>
		/// Wherether or not the server 
		/// </summary>
		/// <param name="serverId"></param>
		/// <returns></returns>
		protected bool IsUp (SourceReference serverId) {
			CoflnetServer server;
			TryGetResource<CoflnetServer> (serverId, out server);
			return server != null && server.State == CoflnetServer.ServerState.UP;
		}

		public class ObjectNotFound : CoflnetException {
			public ObjectNotFound (SourceReference id) : base ("object_not_found", $"The resource {id} wasn't found on this server", null, 404) { }
		}

		/// <summary>
		/// A strong typed reference of the target <see cref="Referenceable"/>
		/// </summary>
		/// <param name="referenceId"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public InnerReference<T> GetNewReference<T> (SourceReference referenceId) where T : Referenceable {
			return new InnerReference<T> (references[referenceId].Resource as T);
		}

		public InnerReference<Referenceable> GetInnerReference (SourceReference referenceId) {
			return references[referenceId];
		}

		public void UpdateIdAndAddRedirect (SourceReference oldId, SourceReference newId) {
			references[newId] = references[oldId];
			// update the id
			references[newId].Resource.Id = newId;

			// replace old with a pointer
			references[oldId] = new RedirectReference<Referenceable> () { newId = newId };
		}

		public ICollection<SourceReference> AllIds () {
			return references.Keys;
		}
	}

	/// <summary>
	/// Defines objects that are Referenceable across servers.
	/// Only use for larger objects.
	/// </summary>
	[DataContract]
	public abstract class Referenceable {
		[DataMember]
		public SourceReference Id;

		[DataMember]
		public Access Access;

		/// <summary>
		/// Global commands useable for every <see cref="Referenceable"/> in the system.
		/// Contains commands for syncing resources between servers.
		/// </summary>
		[IgnoreDataMember]
		public static CommandController globalCommands;

		protected Referenceable (Access access, SourceReference id) {
			this.Id = id;
			this.Access = access;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.Server.Referenceable"/> class.
		/// </summary>
		/// <param name="owner">Owner creating this resource.</param>
		protected Referenceable (SourceReference owner) : this () {
			this.Access = new Access (owner);
		}

		public Referenceable () {

			//this.Id = ReferenceManager.Instance.CreateReference(this);
		}

		/// <summary>
		/// Initializes the <see cref="T:Coflnet.Referenceable"/> class.
		/// </summary>
		static Referenceable () {
			globalCommands = new CommandController ();
			globalCommands.RegisterCommand<ReturnResponseCommand> ();
			globalCommands.RegisterCommand<GetResourceCommand>();
			globalCommands.RegisterCommand<CreationResponseCommand>();
		}

		/// <summary>
		/// Assigns an identifier and registers the object in the <see cref="ReferenceManager"/>
		/// </summary>
		/// <param name="referenceManager">Optional other instance of an referencemanager</param>
		public void AssignId (ReferenceManager referenceManager = null) {
			if (referenceManager != null) {
				this.Id = referenceManager.CreateReference (this);
			} else {
				this.Id = ReferenceManager.Instance.CreateReference (this);
			}
		}

		/// <summary>
		/// Checks if a certain resource is allowed to access this one and or execute commands
		/// </summary>
		/// <returns><c>true</c>, if allowed access, <c>false</c> otherwise.</returns>
		/// <param name="requestingReference">Requesting reference.</param>
		/// <param name="mode">Mode.</param>
		public virtual bool IsAllowedAccess (SourceReference requestingReference, AccessMode mode = AccessMode.READ) {
			return (Access != null) && Access.IsAllowedToAccess (requestingReference, mode)
				// A user might access itself
				||
				requestingReference == this.Id;
		}

		/// <summary>
		/// Executes the command found in the <see cref="MessageData.t"/>
		/// Returns the <see cref="Command"/> when done
		/// </summary>
		/// <returns>The command.</returns>
		/// <param name="data">Data.</param>
		public virtual Command ExecuteCommand (MessageData data) {
			var controller = GetCommandController ();
			var command = controller.GetCommand (data.t);

			controller.ExecuteCommand (command, data, this);

			return command;
		}

		/// <summary>
		/// Executes the command with given data
		/// </summary>
		/// <param name="data">Data to pass on to the <see cref="Command"/>.</param>
		/// <param name="command">Command to execute.</param>
		public virtual void ExecuteCommand (MessageData data, Command command) {
			GetCommandController ().ExecuteCommand (command, data, this);
		}

		public abstract CommandController GetCommandController ();

		/// <summary>
		/// Will generate and add a new Access Instance if none exists yet.
		/// </summary>
		/// <returns>The Access Settings for this Resource</returns>
		public Access GetAccess()
		{
			if(Access == null){
				Access = new Access();
			}
			return Access;
		}
	}

	public class GetResourceCommand : ReturnCommand {
		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="MessageData"/> passed over the network .</param>
		public override MessageData ExecuteWithReturn (MessageData data) {
			data.message = data.CoreInstance.ReferenceManager.SerializeWithoutAccess (data.rId);
			
			UnityEngine.Debug.Log("returning resource");
			
			return data;
			//MessagePack.MessagePackSerializer.Typeless.Serialize()                
		}

		/// <summary>
		/// Special settings and Permissions for this <see cref="Command"/>
		/// </summary>
		/// <returns>The settings.</returns>
		public override CommandSettings GetSettings () {
			return new CommandSettings (ReadPermission.Instance);
		}
		/// <summary>
		/// Gets the globally unique slug (short human readable id).
		/// </summary>
		/// <returns>The slug .</returns>
		public override string Slug {
			get {

				return "GetResource";
			}
		}
	}

	public class GrantAccess : Command {
		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="MessageData"/> passed over the network .</param>
		public override void Execute (MessageData data) {
			var param = data.GetAs<KeyValuePair<SourceReference, AccessMode>> ();
			data.GetTargetAs<Referenceable> ().Access.Authorize (param.Key, param.Value);
		}

		/// <summary>
		/// Special settings and Permissions for this <see cref="Command"/>
		/// </summary>
		/// <returns>The settings.</returns>
		public override CommandSettings GetSettings () {
			return new CommandSettings (CanChangePermissionPermission.Instance);
		}
		/// <summary>
		/// Gets the globally unique slug (short human readable id).
		/// </summary>
		/// <returns>The slug .</returns>
		public override string Slug {
			get {

				return "GrantAccess";
			}
		}
	}

	public class RemoveAccess : Command {
		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="MessageData"/> passed over the network .</param>
		public override void Execute (MessageData data) {
			data.GetTargetAs<Referenceable> ().Access.Authorize (data.GetAs<SourceReference> (), AccessMode.NONE);
		}

		/// <summary>
		/// Special settings and Permissions for this <see cref="Command"/>
		/// </summary>
		/// <returns>The settings.</returns>
		public override CommandSettings GetSettings () {
			return new CommandSettings (CanChangePermissionPermission.Instance);
		}
		/// <summary>
		/// Gets the globally unique slug (short human readable id).
		/// </summary>
		/// <returns>The slug .</returns>
		public override string Slug {
			get {

				return "RemoveAccess";
			}
		}
	}

	/// <summary>
	/// Get the <see cref="Access"/> Property of an object which isn't part of the object itself by default.
	/// </summary>
	public class GetAccess : ReturnCommand {
		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="MessageData"/> passed over the network .</param>
		public override MessageData ExecuteWithReturn (MessageData data) {
			data.SerializeAndSet<Access> (data.GetTargetAs<Referenceable> ().Access);
			return data;
		}

		/// <summary>
		/// Special settings and Permissions for this <see cref="Command"/>
		/// </summary>
		/// <returns>The settings.</returns>
		public override CommandSettings GetSettings () {
			return new CommandSettings (WritePermission.Instance);
		}
		/// <summary>
		/// Gets the globally unique slug (short human readable id).
		/// </summary>
		/// <returns>The slug .</returns>
		public override string Slug {
			get {

				return "GetAccess";
			}
		}
	}

	[DataContract]
	public class Access {
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
		public enum GeneralAccess {
			NONE = 0,
			SERVER_READ = 1,
			SERVER_WRITE = 2,
			LOCAL_READ = 0x1 << 3,
			LOCAL_WRITE = 0x2 << 3,

			ALL_READ= 0x1 << 6,
			ALL_WRITE = 0x2 << 6,
			
			ALL_READ_LOCAL_WRITE = ALL_READ | LOCAL_WRITE,
			ALL_READ_AND_WRITE = ALL_READ | ALL_WRITE,
			LOCAL_READ_AND_WRITE = LOCAL_READ | LOCAL_WRITE,
			ALL_READ_LOCAL_READ_AND_WRITE = ALL_READ_LOCAL_WRITE | LOCAL_WRITE,
			SERVER_READ_AND_WRITE = SERVER_READ | SERVER_WRITE,
			ALL_READ_SERVER_READ_AND_WRITE = SERVER_READ_AND_WRITE | ALL_READ
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

		public Access (SourceReference owner) {
			Owner = owner;
		}

		public Access()
		{
			
		}

		/// <summary>
		/// Checks if some reference has some level of access to this resource
		/// </summary>
		/// <returns><c>true</c>, if allowed to access, <c>false</c> otherwise.</returns>
		/// <param name="requestingReference">Requesting reference.</param>
		/// <param name="mode">AccessMode.</param>
		public bool IsAllowedToAccess (SourceReference requestingReference, AccessMode mode = AccessMode.READ) {

			// unauthorized senders can't access
			if(requestingReference == default(SourceReference))
			{
				UnityEngine.Debug.Log("Sender is not set, access was blocked");
				return false;
			}

			// is it the owner or the resource itself
			if (requestingReference == Owner)
				return true;

				UnityEngine.Debug.Log($"{requestingReference} is requesting {mode}");

			//is there a special case?
			if (resourceAccess != null) {
				if(resourceAccess.ContainsKey (requestingReference))
					return resourceAccess[requestingReference].CompareTo (mode) >= 0;
				else if(resourceAccess.ContainsKey (requestingReference.FullServerId))
					return resourceAccess[requestingReference.FullServerId].CompareTo (mode) >= 0;
			} 

			// check for "all"
			if (((((int)generalAccess) >> 6)  & (int) mode) > 0)
				return true;

			// check for local
			if (((((int)generalAccess) >> 3)  & (int) mode) > 0)
				return requestingReference.IsSameLocationAs (Owner);
			// check for same server
			if (((((int)generalAccess))  & (int) mode) > 0)
				return Owner.ServerId == requestingReference.ServerId;



			// requestingReference doesn't have access
			return false;
		}

		public void Authorize (SourceReference sourceReference, AccessMode mode = AccessMode.READ) {
			if (resourceAccess == null)
				resourceAccess = new Dictionary<SourceReference, AccessMode> ();
			resourceAccess[sourceReference] = mode;
		}
	}

	[Flags]
	public enum AccessMode {
		/// <summary>
		/// Essentially blocks
		/// </summary>
		NONE = 0,
		READ = 1,
		WRITE = 2,
		READ_AND_WRITE = READ | WRITE,
		/// <summary>
		/// Permission to change others permission, includes read and write
		/// </summary>
		CHANGE_PERMISSIONS = 4,
		/// <summary>
		/// This level will be ignored
		/// </summary>
		ASNORMAL = 0xFF
	}

	/// <summary>
	/// Defines an object that is Referenceable and has to have a local copy.
	/// Use this very carefully.
	/// </summary>
	public abstract class ILocalReferenceable : Referenceable {

	}

	[DataContract (Name = "ref", Namespace = "")]
	public class Reference<T> where T : Referenceable {
		/// <summary>
		/// Internal reference used for storing referenced object 
		/// if present in memory on the current server
		/// </summary>
		/// <value></value>
		[IgnoreDataMember]
		public InnerReference<T> InnerReference { get; set; }

		/// <summary>
		/// Executes a command on the server containing the resource referenced by this object
		/// </summary>
		/// <param name="data">Command data to send</param>
		public void ExecuteForResource (MessageData data) {
			ReferenceManager.Instance.ExecuteForReference (data);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.Server.Reference`1"/>
		/// </summary>
		/// <param name="resource">Resource.</param>
		/*public Reference(T resource, SourceReference referenceId)
		{
			this.resource = resource;
			this.referenceId = referenceId;
			//referenceId = ReferenceManager.Instance.AddReference(resource);
		}*/

		public Reference (T resource) {
			this.InnerReference = new InnerReference<T> () { Resource = resource };
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.Server.Reference`1"/> class without actually knowing the data.
		/// </summary>
		/// <param name="referenceId">Reference identifier.</param>
		public Reference (SourceReference referenceId) {
			this.ReferenceId = referenceId;
		}

		public Reference () {

		}

		[IgnoreDataMember]
		public T Resource {
			get {
				if (InnerReference == null) {
					InnerReference = ReferenceManager.Instance.GetNewReference<T> (this.ReferenceId);
				}
				return InnerReference.Resource;
			}
			set {
				InnerReference.Resource = value;
			}
		}

		[DataMember (Name = "id")]
		public SourceReference ReferenceId {
			get;
			set;
		}

		/// <summary>
		/// Gets the referenced resource as some type if present on the server, null otherwise
		/// </summary>
		/// <returns>The referenced data as specific object.</returns>
		/// <typeparam name="Y">The 1st type parameter.</typeparam>
		public Y GetAsIfPresent<Y> () where Y : class {
			return Resource as Y;
		}

		/// <summary>
		/// Is this instance of type Y.
		/// </summary>
		/// <returns>If this reference contains a resource with type Y.</returns>
		/// <typeparam name="Y">The type parameter to test against.</typeparam>
		public bool Is<Y> () {
			return Resource is Y;
		}
	}

	/// <summary>
	/// Redundant reference Data exists on one or more servers (recomended)
	/// </summary>
	[DataContract]
	public class RedundantReference<T> : Reference<T> where T : Referenceable {
		[DataMember]
		protected List<long> failoverServers = new List<long> ();

		/// <summary>
		/// Adds a new server for extra redundancy.
		/// </summary>
		/// <param name="server">Server to add.</param>
		public void AddServer (CoflnetServer server) {
			AddServer (server.Id.ServerId);
		}

		/// <summary>
		/// Adds a new server for extra redundancy.
		/// </summary>
		/// <param name="serverId">Server to add.</param>
		public void AddServer (long serverId) {
			failoverServers.Add (serverId);
		}

		public void RemoveServer (CoflnetServer server) {
			RemoveServer (server.Id.ServerId);
		}
		/// <summary>
		/// Removes the server from the failover list.
		/// </summary>
		/// <param name="serverId">Server identifier.</param>
		public void RemoveServer (long serverId) {
			failoverServers.Remove (serverId);
		}

		/// <summary>
		/// Executes a command on the server containing the resource referenced by this object
		/// </summary>
		/// <param name="data">Command data to send</param>
		public new void ExecuteForResource (MessageData data) {
			CoflnetCore.Instance.SendCommand (data, ReferenceId.ServerId);
			// also send it to the failover servers
			foreach (var item in failoverServers) {
				CoflnetCore.Instance.SendCommand (data, item);
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:Coflnet.Server.RedundantReference`1"/> s Resource is lokally available.
		/// </summary>
		/// <value><c>true</c> if is lokal; otherwise, <c>false</c>.</value>
		[IgnoreDataMember]
		public bool IsLokal {
			get {

				return ReferenceManager.Instance.Exists (this.ReferenceId);
			}
		}

		/// <summary>
		/// Gets the nearest server which has a copy of this resource.
		/// </summary>
		/// <value>The nearest server.</value>
		[IgnoreDataMember]
		public CoflnetServer NearestServer {
			get {
				CoflnetServer closest = ServerController.Instance.GetOrCreate (ReferenceId.ServerId);
				foreach (var item in failoverServers) {
					var server = ServerController.Instance.GetOrCreate (item);
					// if another server that is closer or as fast as the managing server use it instead
					if (server.PingTimeMS <= closest.PingTimeMS)
						closest = server;
				}
				return closest;
			}
		}

		public RedundantReference () { }

		public RedundantReference (T resource, params CoflnetServer[] failoverServer) : base (resource) {
			if (failoverServer == null)
				return;
			foreach (var item in failoverServer) {
				this.failoverServers.Add (item.Id.ServerId);
			}
		}
	}

	public class InstanceAllreadyExistsException : Exception {
		public InstanceAllreadyExistsException (string message = "There already is an Instance of this class, there can only be one") : base (message) { }
	}
}