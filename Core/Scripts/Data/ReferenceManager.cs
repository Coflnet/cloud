using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Coflnet.Core.Commands;
using MessagePack;

namespace Coflnet
{
    public partial class EntityManager {

		protected ConcurrentDictionary<EntityId, InnerReference<Entity>> references = new ConcurrentDictionary<EntityId, InnerReference<Entity>> ();
		protected static EntityManager instance;
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

		public static EntityManager Instance {
			get {
				if (instance == null) {
					instance = new EntityManager ();
				}
				return instance;
			}
		}

		public EntityManager () : this ("res") { }

		/// <summary>
		/// Creates a new instance of the ReferenceManager
		/// </summary>
		/// <param name="relativeStorageFolder">Relative sub-Directory to the data directory to store references and resources in</param>
		public EntityManager (string relativeStorageFolder) {
			RelativeStorageFolder = relativeStorageFolder;
		}

		public ConcurrentDictionary<EntityId, InnerReference<Entity>> References {
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
		public List<InnerReference<Entity>> GetForServer (long id) {

			List<InnerReference<Entity>> storedReferences = new List<InnerReference<Entity>> ();

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
		public T GetEntity<T>(long id)
		{
			return GetEntity<T>(new SourceReference(Coflnet.Server.ServerController.ServerInstance.CurrentServer.ServerId, id));
		}
        */

		public string GetReferences()
		{
			string content = "";

			foreach (var item in references)
			{
				content += $"{item.Key}={item.Value.Resource}";
			}
			return content;
		}

		public Entity GetResource (EntityId id) {
			return GetEntity<Entity> (id);
		}

		/// <summary>
		/// Serializes the <see cref="Entity"/> without the access attribute.
		/// </summary>
		/// <returns>The without access.</returns>
		/// <param name="id">Identifier.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public byte[] SerializeWithoutAccess<T> (EntityId id) where T : Entity {
			return SerializeWithoutAccess<T> (id, CoflnetEncoder.Instance);
		}

		/// <summary>
		/// Serializes the <see cref="Entity"/> without the access.subscriber attribute.
		/// </summary>
		/// <returns>The serialized object without the subscribers</returns>
		/// <param name="id">Identifier.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public byte[] SerializeWithoutLocalInfo (EntityId id) {
			var data = GetResource (id);
			byte[] result;
			// NOTE: this could still cause trouble if Access would be accessed in a different Thread
			lock (data) {
				var subscribers = data.Access.Subscribers;
				data.Access.Subscribers = null;
				result = MessagePack.MessagePackSerializer.Typeless.Serialize (data);
				data.Access.Subscribers = subscribers;
			}
			return result;
		}

		/// <summary>
		/// Save the specified <see cref="Entity"/> and remove it from Ram.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="removeFromRam">If set to <c>true</c> remove from ram.</param>
		public void Save (EntityId id, bool removeFromRam = false) {

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

			InnerReference<Entity> reference;
			references.TryRemove (id, out reference);
		}

		class PersistHelper {
			public object data;
			public InnerReference<Entity> reference;
		}

		/// <summary>
		/// Serializes the <see cref="Entity"/> without the access attribute.
		/// </summary>
		/// <returns>The without access.</returns>
		/// <param name="id">Identifier.</param>
		/// <param name="encoder">Encoder.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public byte[] SerializeWithoutAccess<T> (EntityId id, CoflnetEncoder encoder) where T : Entity {
			T data = GetEntity<T> (id);
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
		public void CloneEntity(EntityId id,Action<Entity> afterReceive)
		{
			if(Exists(id)){
				// break if the resource is already cloned
				return;
			}


			coreInstance.SendCommand<GetResourceCommand,short>(id,0,o =>{
				var resource = MessagePack.MessagePackSerializer.Typeless.Deserialize(o.message) as Entity;
				this.AddReference(resource);
				afterReceive?.Invoke(resource);
			});
		}


		/// <summary>
		/// Copies a resource to a new id
		/// </summary>
		/// <param name="originId">The idof the object to copy</param>
		/// <param name="newId">The id to assign the new object</param>
		public Entity CopyResource(EntityId originId, EntityId newId)
		{
			return CopyResource(GetEntity<Entity>(originId),newId);
		}


		/// <summary>
		/// Copies a resource to a new id
		/// </summary>
		/// <param name="originId">The idof the object to copy</param>
		/// <param name="newId">The id to assign the new object</param>
		public Entity CopyResource(Entity originId, EntityId newId = default(EntityId))
		{
			MemoryStream s = new MemoryStream();
			MessagePackSerializer.Typeless.Serialize(s,originId);
			var newObject = MessagePackSerializer.Typeless.Deserialize(s) as Entity;

			if(newId == default(EntityId))
			{
				newId = EntityId.NextLocalId;
			}
			newObject.Id = newId;
			AddReference(newObject);
			return newObject;
		}



		/// <summary>
		/// Replaces Resoure if it exists or adds it if not
		/// </summary>
		/// <param name="resource">The resource to add</param>
		public void ReplaceResource(Entity resource)
		{
			references.AddOrUpdate(resource.Id,new InnerReference<Entity>(resource),
			(a,b)=>new InnerReference<Entity>(resource));
		}


        /// <summary>
        /// Gets the resource.
        /// </summary>
        /// <returns>The resource.</returns>
        /// <param name="id">Identifier.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public T GetEntity<T> (EntityId id) where T : Entity {
			InnerReference<Entity> reference;

			if (!TryGetReference (id, out reference) || reference.Resource == null) {
				throw new ObjectNotFound (id);
			}


			if (reference.Resource is T) {
				return (T) reference.Resource;
			}
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
		public bool TryLoadReference (EntityId id, out InnerReference<Entity> data, bool cache = true) {
			var path = $"res/{id.ToString()}";
			if (!FileController.Exists (path)) {
				data = null;
				return false;
			}
			var bytes = DataController.Instance.LoadData (path);
			var result = MessagePack.MessagePackSerializer.Typeless.Deserialize (bytes)
			as InnerReference<Entity>;
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
		public bool TryGetEntity<T> (EntityId id, out T data) where T : Entity {
			InnerReference<Entity> reference;
			TryGetReference (id, out reference);

			// make sure it exists so <see cref="GetEntity<T>(id)"/> won't fail
			if (reference == null || reference.Resource == null) {
				data = null;
				return false;
			}


			data = GetEntity<T> (id);
			return true;
		}

		/// <summary>
		/// Attempts to find the resource, will create it default if not found and set correct id
		/// </summary>
		/// <param name="id">Id to search for</param>
		/// <typeparam name="T">What type the resource is</typeparam>
		/// <returns>The found/create resource</returns>
		public T GetOrDefault<T> (EntityId id) where T : Entity {
			T value;
			if (!TryGetEntity<T> (id, out value)) {
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
		public T GetOrCreate<T> (EntityId id, Func<T> creator) where T : Entity {
			T value;
			if (!TryGetEntity<T> (id, out value)) {
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
		public T GetOrCreate<T> (EntityId id, Func<T, T> alterer) where T : Entity {
			T value;
			if (!TryGetEntity<T> (id, out value)) {
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
		public bool Exists (EntityId id) {
			return references.ContainsKey (id);
		}

		/// <summary>
		/// Creates a new reference.
		/// </summary>
		/// <returns>The reference identifier.</returns>
		/// <param name="entity">Referencable object to store.</param>
		public virtual EntityId CreateReference (Entity entity, bool force = false) {
			long nextIndex = ThreadSaveIdGenerator.NextId;

			EntityId newReference;
			// generate new Id for the current server
			if(this.coreInstance == null){
				newReference  = new EntityId (0, nextIndex);
			} else {
				newReference  = new EntityId (this.coreInstance.Id.ServerId, nextIndex);
			}
			if (entity.Id != new EntityId () && !force)
				throw new Exception ("The entity already had an id, it may already have been registered. Call with force = true to ignore");
			entity.Id = newReference;

			InnerReference<Entity> referenceObject = new InnerReference<Entity> (entity);

			if (!AddReference (referenceObject))
				throw new Exception ("adding failed");
			return newReference;
		}

		/// <summary>
		/// Adds a reference and maybe its data to the reference dictionary.
		/// </summary>
		/// <returns><c>true</c>, if reference was added, <c>false</c> otherwise.</returns>
		/// <param name="reference">Reference.</param>
		public bool AddReference (InnerReference<Entity> reference) {
			return references.TryAdd (reference.Resource.Id, reference);
		}

		/// <summary>
		/// Adds a entity and to the reference dictionary.
		/// </summary>
		/// <param name="entity"></param>
		/// <returns><c>true</c>, if reference was added, <c>false</c> otherwise.</returns>
		public bool AddReference (Entity entity) {
			return AddReference (new InnerReference<Entity> (entity));
		}

		public bool TryGetReference (EntityId id, out InnerReference<Entity> result) {
			InnerReference<Entity> reference;
			references.TryGetValue (id, out reference);

			if (reference == null) {
				if (!TryLoadReference (id, out reference, true)) {
					result = null;
					return false;
				}
			}

			// special references 
			// Redirects occur if offline ids were used
			if (reference is RedirectReference<Entity>) {
				var redirectReference = reference as RedirectReference<Entity>;
				return TryGetReference (redirectReference.newId, out result);
			}

			result = reference;
			return true;
		}

		/// <summary>
		/// Executes a command for a <see cref="EntityId"/>.
		/// Will forward the command to the right server if the current one is not the main managing node of the resource.
		/// </summary>
		/// <param name="data">Command data to execute.</param>
		/// <param name="sender">The vertified sender of the message (for distribution purposes)</param>
		public virtual void ExecuteForReference (CommandData data, EntityId sender = default (EntityId)) {

			if (data.Recipient == default(EntityId)) {
				// special case it is myself (0 is local)
				coreInstance.ExecuteCommand (data);
				return;
			}


			var mainManagingNode = ManagingNodeFor(data.Recipient);
			var IAmTheManager = mainManagingNode == CurrentServerId;

			InnerReference<Entity> reference;
			TryGetReference (data.Recipient, out reference);

			if (reference == null) {
				// is the current server the managing server?
				if (!IAmTheManager) {
					// we are not the main managing server, pass it on to it
					CoflnetCore.Instance.SendCommand (data);
					return;
				}
				// we are the main managing server but this resource doesn't exist
				throw new ObjectNotFound (data.Recipient);
			}

			//var amIAManagingNode = IsManagingNodeFor(this.coreInstance.Id,data.rId);
			var isTheSenderTheManager = IsManagingNodeFor(sender,data.Recipient);


			var resource = reference.Resource;

			if (resource != null) {
				Command command = null;
				try{
					command = resource.GetCommandController ()
										.GetCommand (data.Type);
				} catch(CommandUnknownException)
				{
					throw new CommandUnknownException(data.Type,resource,data.MessageId);
				}
				if(IAmTheManager)
				{
					// I can do everything
					resource.ExecuteCommand(data,command);
					
					if(command.Settings.Distribute)
					{
						// update
						UpdateSubscribers(resource,data);
					}

					// Not the only managing node? 
					// delay execution until they confirm update
					// coreInstance.SendCommand

					// confirm execution escept it is a confirm itself
					if(!command.Settings.DisableExecuteConfirm)
					{
						coreInstance.SendCommand<ReceiveConfirm,ReceiveConfirmParams>(
							data.SenderId,new ReceiveConfirmParams(data.SenderId,data.MessageId),0,data.Recipient);
					}

					// done
					return;
				}

				if (!command.Settings.Distribute) {
					// if the command isn't updating something, it is save to execute
					resource.ExecuteCommand (data, command);

					if(!command.Settings.DisableExecuteConfirm)
					{
						coreInstance.SendCommand<ReceiveConfirm,ReceiveConfirmParams>(
							data.SenderId,new ReceiveConfirmParams(data.SenderId,data.MessageId),0,data.Recipient);
					}
					
					// the response should be returned now
					return;
				}

				// only execute changing commands command if we are the managing server 
				// or the managing server instructed us to do so
				if (command.Settings.Distribute &&
					(isTheSenderTheManager ||
					IAmTheManager)) {
					resource.ExecuteCommand (data, command);
					
					// execution succeeded
					// update subscribers
					if(IAmTheManager)
						UpdateSubscribers(resource,data);

					// acknowledge


				} else if(command.Settings.LocalPropagation){
					// this is a command on its way to the manager but also needs to be applied now
					resource.ExecuteCommand(data,command);
				}
			}
			/// IMPORTANT
			/// this could cause a loop if we are one of the sibblings managing nodes and are 
			/// distributing this to the managing nodes and it will distribute it back


			if(!IAmTheManager && (!isTheSenderTheManager) && !data.Recipient.IsLocal){
				// This message hasn't been on the manager yet, send it to him
				reference.ExecuteForEntity(data);
			}
		}


		

		/// <summary>
		/// Executed by managing node of the target
		/// </summary>
		/// <param name="data"></param>
		public void UpdateEntity(CommandData data, EntityId sender)
		{
			InnerReference<Entity> reference;
			TryGetReference (data.Recipient, out reference);
			if(reference == null || reference.Resource == null)
			{
				// we don't have this, unsubscribe
				coreInstance.SendCommand<UnsubscribeCommand>(sender);
				return;
			}

			// it has to be the references or my managing node
			if(!IsManagingNodeFor(sender,data.Recipient) && !IsManagingNodeFor(sender,coreInstance.Id))
			{
				return;
			}

			// don't forget to update the core instance
			data.CoreInstance = this.coreInstance;

			// this is here to go around the Permission checking
			var controller = reference.Resource.GetCommandController();
			controller.GetCommand(data.Type).Execute(data);

			// distribute it to other local subscribers 
			// this allows for a exponential fade out from the Resources managing node reducing load
			//  C                      C
			//    ⬉ 				 ⬈
			//		S  <--	M  --> S
			//    ⬋                  ⬊
			//  C 					   C
			// Master, Server, Clients (subscribed)
			UpdateSubscribers(reference.Resource,data,true);
		}




		/// <summary>
		/// Sends an update to all subscribed Resources
		/// </summary>
		/// <param name="resource"></param>
		/// <param name="data"></param>
		protected void UpdateSubscribers(Entity resource, CommandData data,bool onlyLocal = false)
		{
			if(resource.Access == null || resource.Access.Subscribers == null)
			{
				return;
			}
			foreach (var item in resource.Access.Subscribers)
			{
				if(onlyLocal && item.FullServerId == CurrentServerId || !onlyLocal)
				coreInstance.SendCommand<UpdateEntityCommand,CommandData>(item, data);
			}
		}
		

		/// <summary>
		/// Do the actual execution, returns <see cref="true"/> if distribution should be attempted false otherwise
		/// </summary>
		/// <param name="resource"></param>
		/// <param name="data"></param>
		/// <param name="sender"></param>
		/// <returns><see cref="true"/> if distribution should be attempted</returns>
		protected bool ExecuteForReference(Entity resource,CommandData data,EntityId sender)
		{
			var command = resource.GetCommandController ().GetCommand (data.Type);

			// only execute changing commands command if we are the managing server 
			// or the managing server instructed us to do so
			if (command.Settings.Distribute &&
				(sender.ServerId == resource.Id.ServerId && sender.IsServer) ||
				resource.Id.ServerId == ConfigController.ApplicationSettings.id.ServerId) {
				resource.ExecuteCommand (data, command);
			} else if (!command.Settings.Distribute) {
				// if the command isn't updating we are done here
				resource.ExecuteCommand (data, command);
				return false;
			}
			return true;
			//resource.ExecuteCommand()
			//controller.ExecuteCommand(command, data);
		}


		protected EntityId CurrentServerId
		{
			get
			{
				if(coreInstance == null)
					return default(EntityId);
				return coreInstance.Id;
			}
		}


		public EntityId ManagingNodeFor(EntityId reference)
		{
			return ManagingNodeFor(new Reference<Entity>(reference));
		}

		/// <summary>
		/// The Id of the managing node for a given reference
		/// </summary>
		/// <returns>The node for.</returns>
		/// <param name="reference">Reference.</param>
		protected EntityId ManagingNodeFor (Reference<Entity> reference) {
			CoflnetServer server;
			TryGetEntity<CoflnetServer> (new EntityId (reference.EntityId.ServerId, 0), out server);

			if(server != null && server.State == CoflnetServer.ServerState.UP)
			{
				// the default is the creation server if it is up
				return server.Id;
			}

			// second is a sibbling node to the server
			if (server != null && server.State != CoflnetServer.ServerState.UP) {
				// search general failover servers
				foreach (var siblingServer in server.SibblingServers) {
					var id = new EntityId (siblingServer, 0);
					if (IsUp (id)) {
						return id;
					}
				}
			}
			/// try to find another server if main managing node is down
			if (reference.InnerReference is RedundantInnerReference<Entity>) {
				foreach (var sibbling in (reference.InnerReference as RedundantInnerReference<Entity>)
						.SiblingNodes
						.ConvertAll<EntityId> ((id) => new EntityId (id, 0))) {
					TryGetEntity<CoflnetServer> (sibbling, out server);
					// return the first that is up
					if (server.State == CoflnetServer.ServerState.UP) {
						return sibbling;
					}
				}
			}


			// all failed, we have to wait for the creation server to come back up again
			return reference.EntityId.FullServerId;
		}

		public bool IsManagingNodeFor (EntityId nodeId, EntityId resourceId) {
			if(!nodeId.IsServer)
			{
				// its not even a server, cancle
				return false;
			}
			
			if (nodeId.ServerId == resourceId.ServerId)
				return true;

			CoflnetServer mainManagingNode;
			TryGetEntity<CoflnetServer> (resourceId.FullServerId, out mainManagingNode);

			if(mainManagingNode == null)
			{
				// we have no further information about this node
				return false;
			}

			// if it is a sibbling server it is
			foreach (var item in mainManagingNode.SibblingServers)
			{
				if(nodeId.ServerId == item)
					return true;
			}

			// also if the resource appointed it
			InnerReference<Entity> reference;
			if(TryGetReference(resourceId,out reference))
			{
				var redRef =  reference as RedundantInnerReference<Entity>;
				if(redRef != null)
				{
					foreach (var item in redRef.SiblingNodes)
					{
						if(item == nodeId.ServerId){
							return true;
						}
					}
				}
			}



			return false;
		}

		/// <summary>
		/// Wherether or not the server 
		/// </summary>
		/// <param name="serverId"></param>
		/// <returns></returns>
		protected bool IsUp (EntityId serverId) {
			CoflnetServer server;
			TryGetEntity<CoflnetServer> (serverId, out server);
			return server != null && server.State == CoflnetServer.ServerState.UP;
		}

		public class ObjectNotFound : CoflnetException {
			public ObjectNotFound (EntityId id) : base ("object_not_found", $"The resource {id} wasn't found on this server", null, 404) { }
		}

		/// <summary>
		/// A strong typed reference of the target <see cref="Entity"/>
		/// </summary>
		/// <param name="referenceId"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public InnerReference<T> GetNewReference<T> (EntityId referenceId) where T : Entity {
			return new InnerReference<T> (references[referenceId].Resource as T);
		}

		public InnerReference<Entity> GetInnerReference (EntityId referenceId) {
			return references[referenceId];
		}

		public void UpdateIdAndAddRedirect (EntityId oldId, EntityId newId) {
			references[newId] = references[oldId];
			// update the id
			references[newId].Resource.Id = newId;

			// replace old with a pointer
			references[oldId] = new RedirectReference<Entity> () { newId = newId };
		}

		public ICollection<EntityId> AllIds () {
			return references.Keys;
		}
	}

	public class InstanceAllreadyExistsException : Exception {
		public InstanceAllreadyExistsException (string message = "There already is an Instance of this class, there can only be one") : base (message) { }
	}
}