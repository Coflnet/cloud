﻿using System;
using System.Collections.Generic;
using Coflnet.Core;
using Coflnet.Core.Commands;

namespace Coflnet.Client {
	/// <summary>
	/// Coflnet client.
	/// Main class to work with from the outside on a client device.
	/// Also has the Id of the client device.
	/// </summary>
	public class ClientCore : CoflnetCore {
		static void HandleReceiveMessageData (MessageData data) { }

		private CommandController commandController;

		/// <summary>
		/// Connection to a coflnet server
		/// </summary>
		private ClientSocket socket;

		public static ClientCore ClientInstance;

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

		public ClientCore () : this (new CommandController (globalCommands), ClientSocket.Instance) { }

		public ClientCore (CommandController commandController, ClientSocket socket) : this(commandController,socket,ReferenceManager.Instance) {
		}

		public ClientCore (CommandController commandController, ClientSocket socket, ReferenceManager manager) {
			this.commandController = commandController;
			this.socket = socket;
			socket.AddCallback(OnMessage);
			this.ReferenceManager = manager;
			this.ReferenceManager.coreInstance = this;
		}



		public static void Init () {
			ClientInstance.SetCommandsLive ();
			ClientInstance.socket.Reconnect ();
			LocalizationManager.Instance.LoadCompleted ();

			if (ClientInstance != null)
				ClientInstance.CheckInstallation ();
		}

		public void SetCommandsLive () {
			commandController.RegisterCommand<ReferenceManager.UpdateResourceCommand>();

			foreach (var item in CoreExtentions.Commands)
			{
				item.RegisterCommands(commandController);
			}

			foreach (var extention in ClientExtentions.Commands) {
				extention.RegisterCommands (commandController);
			}

			// set the current device id if nothing is set
			if(this.Id == default(SourceReference))
				this.Id = ConfigController.DeviceId;


			// add commands behind the device
			if(this.ReferenceManager.Exists(this.Id))
			{
				ReferenceManager.GetResource<Device>(this.Id)
					.GetCommandController()
					.AddBackfall(GetCommandController());
			} else 
			{
				UnityEngine.Debug.Log("There is no device yet for the clientCore");
				ReferenceManager.AddReference (this);
			}
		}


		public void CheckInstallation () {
			if (ConfigController.UserSettings != null &&
				ConfigController.UserSettings.userId.ResourceId != 0) {
				// we are registered
				return;
			}
			UnityEngine.Debug.Log("Detected no setup, setting up now");
			// This is a fresh install, register at the managing server after showing privacy statement
			FirstStartSetupController.Instance.Setup ();

		}

		/// <summary>
		/// Stop this instance, saves data and closes connections 
		/// </summary>
		public static void Stop () {
			Save ();
			ClientInstance.socket.Disconnect ();
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

		public void OnMessage (MessageData data) {
			// special case before we are logged in 
			if (data.rId == SourceReference.Default) {
				ExecuteCommand (data);
			}
			ReferenceManager.ExecuteForReference (data);
		}

		/// <summary>
		/// Executes the command found in the <see cref="MessageData.t"/>
		/// Returns the <see cref="Command"/> when done
		/// </summary>
		/// <returns>The command.</returns>
		/// <param name="data">Data.</param>
		public override Command ExecuteCommand (MessageData data) {

			// special case: command targets user itself 

			var controller = GetCommandController ();
			var command = controller.GetCommand (data.t);

			controller.ExecuteCommand (command, data, this);

			return command;
		}

		public override CommandController GetCommandController () {
			return CommandController;
		}

		public override void SendCommand (MessageData data, long serverId = 0) {
			// persist data
			MessageDataPersistence.Instance.SaveMessage (data);

			// if the sender is a local one try to update it (the server may block the command otherwise)
			if(data.sId.IsLocal && data.sId  != default(SourceReference))
			{
				Referenceable res;
				// getting the object from the old id will be redirected to the new object with new id (if exists)
				this.ReferenceManager.TryGetResource<Referenceable>(data.sId,out res);
				if(res != null)
				{
					data.sId = res.Id;
				}
			}

			try {
				socket.SendCommand (data);
			} catch (System.InvalidOperationException) {
				// send failed, reconnect and try again
				socket.Reconnect ();
				UnityEngine.Debug.Log ("Reconnecting");

				socket.SendCommand (data);
			}
		}

		public override void SendCommand<C, T> (SourceReference receipient, T data, long id = 0,SourceReference sender = default(SourceReference)) {
			ServerController.Instance.SendCommand<C, T> (receipient, data);
		}

		public override void SendCommand<C> (SourceReference receipient, byte[] data) {
			ServerController.Instance.SendCommand<C> (receipient, data);
		}

		/// <summary>
		/// Sets the application identifier.
		/// </summary>
		/// <param name="idString">Identifier.</param>
		public void SetApplicationId (string idString) {
			var id = new SourceReference (idString);
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
		/// <returns>Proxy Referenceable</returns>
		public Referenceable CreateResource<C>() where C : CreationCommand
		{
			return this.CreateResource<C,CreationCommand.CreationParamsBase>(new CreationCommand.CreationParamsBase());
		}



		/// <summary>
		/// Generates a new Resources On the server and returns a temporary proxy resource.
		/// When the server created the Resource it will be replaced locally.
		/// </summary>
		/// <param name="options">Options to pass along</param>
		/// <typeparam name="C"></typeparam>
		/// <returns>Temporary proxy object storing executed commands</returns>
		public Referenceable CreateResource<C,T>(T options) where C : CreationCommand where T:CreationCommand.CreationParamsBase
		{

			options.OldId = SourceReference.NextLocalId;


			// create it locally
			// first craft MessageData
			var data = MessageData.CreateMessageData<C,T>(this.Id,options,0,this.Id);
			var core = new CreationCore(){ReferenceManager=this.ReferenceManager};

			data.CoreInstance = core;

			// exeute it
			ExecuteCommand(data);
			
			// remove the RedirectReferenceable again (todo)

			options.OldId = core.createdId;
			

			// create it on the server
			SendCommand<C,T>(ConfigController.ManagingServer,options,0,this.Id);

			return ReferenceManager.GetResource<Referenceable>(core.createdId);
		}

		private class CreationCore : ClientCore
		{
			public SourceReference createdId;

			public override void SendCommand<C, T>(SourceReference receipient, T data, long id = 0, SourceReference sender = default(SourceReference))
			{
				// this only exists as a "callback" 
				createdId = ((KeyValuePair<SourceReference,SourceReference>)((object)data)).Value;

				UnityEngine.Debug.Log($"The created resource has the id: {createdId}");
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

            public override void SendCommand(MessageData data, long serverId = 0)
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
        public Referenceable CreateResource<C,T>(T options, Action<T> afterCreate) where C : CreationCommand where T:CreationCommand.CreationParamsBase
		{
			var temp =  CreateResource<C,T>(options);

			ReturnCommandService.Instance.AddCallback(temp.Id.ResourceId,
				d=>{
					// clone the resource
					ReferenceManager.Instance.GetResource(d.GetAs<SourceReference>());
					afterCreate.Invoke(d.GetAs<T>());
				});

			return temp;
		}

		public override void CloneAndSubscribe(SourceReference id, Action<Referenceable> afterClone = null)
		{
			// create temporary proxy to receive commands bevore cloning is finished
			ReferenceManager.AddReference(new SubscribeProxy(id));

			// this is different on server sides
			SendCommand<Sub2Command,SourceReference>(ConfigController.ManagingServer,id,0,this.Id);
			UnityEngine.Debug.Log($"Subscribing client from {Id}");

			// now clone it
			FinishSubscribing(id,afterClone);
		}
	}
}