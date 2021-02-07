using System;
using System.Collections.Generic;
using Coflnet.Core;
using Coflnet.Core.Commands;

namespace Coflnet.Client {
	/// <summary>
	/// Coflnet client.
	/// Main class to work with from the outside on a client device.
	/// Also has the Id of the client installation.
	/// </summary>
	public class ClientCore : CoflnetCore {
		static void HandleReceiveCommandData (CommandData data) { }

		private CommandController commandController;

		/// <summary>
		/// Connection to a coflnet server
		/// </summary>
		private ClientSocket socket;


		private static ClientCore _cc;

		public static ClientCore ClientInstance {get{return _cc;}
		set{
			_cc = value;
		}}

		public CommandController CommandController {
			get {
				return commandController;
			}
		}

		static ClientCore () {
			ClientInstance = new ClientCore ();
			Instance = ClientInstance;
			// setup
			ClientSocket.Instance.AddCallback (ClientInstance.OnMessage);
		}

		public ClientCore () : this (new CommandController (CoreCommands), ClientSocket.Instance) { }

		public ClientCore (CommandController commandController, ClientSocket socket) : this(commandController,socket,EntityManager.Instance) {
		}

		public ClientCore (CommandController commandController, ClientSocket socket, EntityManager manager) {
			this.commandController = commandController;
			this.socket = socket;
			socket.AddCallback(OnMessage);
			this.EntityManager = manager;
			this.EntityManager.coreInstance = this;
		}



		public static void Init () {
			ClientInstance.SetCommandsLive ();
            System.Threading.Tasks.Task.Run(ClientInstance.socket.Reconnect);
			I18nController.Instance.LoadCompleted ();

			if (ClientInstance != null)
				ClientInstance.CheckInstallation ();
		}

		/// <summary>
		/// Enables alle Commands from extentions
		/// </summary>
		public void SetCommandsLive () {
			commandController.RegisterCommand<EntityManager.UpdateEntityCommand>();

			foreach (var item in CoreExtentions.Commands)
			{
				item.RegisterCommands(commandController);
			}

			foreach (var extention in ClientExtentions.Commands) {
				extention.RegisterCommands (commandController);
			}

			// add commands behind the device
			if(this.EntityManager.Exists(this.Id))
			{
				// only add it if it is a device (not null)
				EntityManager.GetEntity<Device>(this.Id)
					?
					.GetCommandController()
					.AddFallback(GetCommandController());
			} else 
			{
								EntityManager.AddReference (this);
			}
		}


		public void CheckInstallation () {
			if (ConfigController.UserSettings != null &&
				ConfigController.UserSettings.userId.LocalId != 0) {
				// we are registered
				return;
			}
						// This is a fresh install, register at the managing server after showing privacy statement
			FirstStartSetupController.Instance.Setup ();

		}

		/// <summary>
		/// Stop this instance, saves data and closes connections 
		/// </summary>
		public static void Stop () {
			Save ();
			ClientInstance.socket.Disconnect ();
			Instance.InvokeOnExit();
		}

		public static void Save () {
			ConfigController.Save ();
		}

		/// <summary>
		/// Sets the managing servers.
		/// </summary>
		/// <param name="serverIds">Server identifiers.</param>
		public void SetManagingServer (params long[] serverIds) {
			ConfigController.UserSettings.managingServers.AddRange (serverIds);
		}

		public void OnMessage (CommandData data) {
			// special case before we are logged in 
			if (data.Recipient == EntityId.Default) {
				ExecuteCommand (data);
			}
			EntityManager.ExecuteForReference (data);
		}

		/// <summary>
		/// Executes the command found in the <see cref="CommandData.Type"/>
		/// Returns the <see cref="Command"/> when done
		/// </summary>
		/// <returns>The command.</returns>
		/// <param name="data">Data.</param>
		public override Command ExecuteCommand (CommandData data) {

			// special case: command targets user itself 

			var controller = GetCommandController ();
			var command = controller.GetCommand (data.Type);

			controller.ExecuteCommand (command, data, this);

			return command;
		}

		public override CommandController GetCommandController () {
			return CommandController;
		}

		public override void SendCommand (CommandData data, long serverId = 0) {
			// persist data
			CommandDataPersistence.Instance.SaveMessage (data);

			// if the sender is a local one try to update it (the server may block the command otherwise)
			if(data.SenderId.IsLocal && data.SenderId  != default(EntityId))
			{
				Entity res;
				// getting the object from the old id will be redirected to the new object with new id (if exists)
				this.EntityManager.TryGetEntity<Entity>(data.SenderId,out res);
				if(res != null)
				{
					data.SenderId = res.Id;
				}
			}

			try {
				socket.SendCommand (data);
			} catch (System.InvalidOperationException) {
				// send failed, reconnect and try again
				socket.Reconnect ();
				
				socket.SendCommand (data);
			}
		}

		public override void SendCommand<C, T> (EntityId receipient, T data,EntityId sender = default(EntityId), long id = 0) {
			ServerController.Instance.SendCommand<C, T> (receipient, data,sender);
		}

		/// <summary>
		/// Sets the application identifier.
		/// </summary>
		/// <param name="idString">Identifier.</param>
		public void SetApplicationId (string idString) {
			var id = new EntityId (idString);
			if (ConfigController.UserSettings.managingServers.Count == 0) {
				ConfigController.UserSettings.managingServers.Add (id.ServerId);
			}
			this.Access.Owner = id;
			ConfigController.ApplicationSettings.id = id;
		}


		/// <summary>
		/// Creates a new Object on the server that doesn't need extra params for creation
		/// </summary>
		/// <typeparam name="C">Command that creates the resource</typeparam>
		/// <returns>Proxy <see cref="Entity"/></returns>
		public Entity CreateEntity<C>(EntityId owner = default(EntityId)) where C : CreationCommand
		{
			return this.CreateEntity<C,CreationCommand.CreationParamsBase>(new CreationCommand.CreationParamsBase(),owner);
		}



		/// <summary>
		/// Generates a new Resources On the server and returns a temporary proxy resource.
		/// When the server created the Resource it will be replaced locally.
		/// </summary>
		/// <param name="options">Options to pass along</param>
		/// <typeparam name="C"></typeparam>
		/// <returns>Temporary proxy object storing executed commands</returns>
		public Entity CreateEntity<C,T>(T options, EntityId sender = default(EntityId)) 
									where C : CreationCommand where T:CreationCommand.CreationParamsBase
		{
			options.options.OldId = EntityId.NextLocalId;

			var ownerId = this.Id;
			if(ownerId == default(EntityId))
			{
				ownerId = ConfigController.ManagingServer;
			}

			// create it locally
			// first craft CommandData
			var normaldata = CommandData.CreateCommandData<C,T>(ownerId,options,0,this.Id);

			// wrap it in a special message data that captures the id
			var data = new CreationCommandData(normaldata);
			var core = this;//new CreationCore(){ReferenceManager=this.ReferenceManager};
			//core.SetCommandsLive();

			data.CoreInstance = this;

			// execute it on the owner resource if possible
			if(EntityManager.Exists(sender))
				EntityManager.GetResource(sender).ExecuteCommand(data);
			else {
				Logger.Log("oh shot");
				core.ExecuteCommand(data);
			}

			// exeute it
			//core.ExecuteCommand(data);
			
			// remove the Redirect<see cref="Entity"/> again (todo)

			options.options.OldId = data.createdId;
			
			// create it on the server
			SendCommand<C,T>(ownerId,options,sender);

			return EntityManager.GetEntity<Entity>(data.createdId);
		}

		private class CreationCore : ClientCore
		{
			public EntityId createdId;

			public override void SendCommand<C, T>(EntityId receipient, T data, EntityId sender = default(EntityId), long id = 0)
			{
				// this only exists as a "callback" 
				createdId = ((KeyValuePair<EntityId,EntityId>)((object)data)).Value;

							}

		
		}

		private class CreationCommandData : CommandData
		{
			public EntityId createdId;

			public override void SendBack(CommandData data)
			{
				createdId = data.GetAs<KeyValuePair<EntityId,EntityId>>().Value;
							}

			public CreationCommandData(CommandData data) : base(data)
			{

			}
		}

		/* 
        private class CreationCore : ClientCore
        {
			SourceReference createdResourceId;

            public override CommandController GetCommandController()
            {
                throw new NotImplementedException();
            }

            public override void SendCommand(CommandData data, long serverId = 0)
            {
                if(data.t == "creationResponse")
				{
					createdResourceId = data.GetAs<SourceReference>();
					return;
				}
            }
        }*/

        /// <summary>
        /// Generates a new Resources On the server and returns a temporary proxy resource.
        /// When the server created the Resource it will be replaced locally.
        /// Allows additional callback which is executed when the creation is completed.
        /// It won't be executed after a program restart since it isn't persisted in any way.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="afterCreate">Callback executed when creation is done and program isn't restarted in between. Can be used for updating the UI.</param>
        /// <typeparam name="C"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Entity CreateEntity<C,T>(T options, Action<T> afterCreate) where C : CreationCommand where T:CreationCommand.CreationParamsBase
		{
			var temp =  CreateEntity<C,T>(options);

			ReturnCommandService.Instance.AddCallback(temp.Id.LocalId,
				d=>{
					// clone the resource
					EntityManager.Instance.GetResource(d.GetAs<EntityId>());
					afterCreate.Invoke(d.GetAs<T>());
				});

			return temp;
		}

		public override void CloneAndSubscribe(EntityId id, Action<Entity> afterClone = null)
		{
			// create temporary proxy to receive commands bevore cloning is finished
			EntityManager.AddReference(new SubscribeProxy(id));

			// this is different on server sides
			SendCommand<Sub2Command,EntityId>(ConfigController.ManagingServer,id,this.Id);
			
			// now clone it
			FinishSubscribing(id,afterClone);
		}
	}
}