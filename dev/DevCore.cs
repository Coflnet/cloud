﻿using System;
using System.Collections.Generic;
using Coflnet.Client;
using Coflnet.Server;

namespace Coflnet.Dev {
	/// <summary>
	/// Development Core for testing purposses.
	/// It simulates A client alongside with a server.
	/// </summary>
	public class DevCore : CoflnetCore {
		private ClientCore clientCore;
		private ServerCore serverCore;

		/// <summary>
		/// This is the user and the serverid simulated
		/// </summary>
		private SourceReference userId;

		private SimulationInstance lastAddedClient;

		/// <summary>
		/// Cotains all simulated devices/server/users
		/// </summary>
		public Dictionary<SourceReference,SimulationInstance> simulationInstances;


		public static DevCore DevInstance {get;private set;}

		static DevCore () {

		}

		public DevCore(){
			this.ReferenceManager = ReferenceManager.Instance;
		}

		private class DummyPrivacyScreen : IPrivacyScreen {
			public void ShowScreen (Action<int> whenDone) {
				return;
			}
		}

		/// <summary>
		/// Will add default screens
		/// </summary>
		public static void SetupDefaults () {
			// configure cores
			PrivacyService.Instance.privacyScreen = new DummyPrivacyScreen ();
		}

		/// <summary>
		/// Initialized the global <see cref="CoflnetCore.Instance"/> as a `devCore`.
		/// Will reset the development enviroment when called again (to support multiple unit tests)
		/// </summary>
		/// <param name="id">Application/Server Id to use</param>
		/// <param name="preventDefaultSetup"><c>ture</c> when default settings (dummys) should NOT be set such as <see cref="DummyPrivacyScreen"/></param>
		public static void Init (SourceReference id, bool preventDefaultSetup = false) {
			ConfigController.ActiveUserId = id;
			ConfigController.ApplicationSettings.id = id.FullServerId;

			UnityEngine.Debug.Log($"setting up with {id}");

			if (!preventDefaultSetup) {
				SetupDefaults ();
			}

			// to have instances loaded
			var toLoadInstance = ClientCore.ClientInstance;
			var serverInstance = ServerCore.ServerInstance;
			if(DevInstance == null){
				DevInstance = new DevCore (); 
				DevInstance.simulationInstances = new Dictionary<SourceReference, SimulationInstance>();
				CoflnetCore.Instance = DevInstance;
			} else {
				// reset if there was an devinstance bevore
				DevInstance.simulationInstances.Clear();
				ServerCore.ServerInstance = new ServerCore();
				ClientCore.ClientInstance = new ClientCore();
			}

			DevInstance.AddServerCore(id.FullServerId );
			if(!id.IsServer){
				DevInstance.AddClientCore(id);
			} else {
				DevInstance.AddClientCore(SourceReference.Default);
			}

			
			ServerCore.Init ();
			ClientCore.Init ();

		}

		/// <summary>
		/// Adds a new Server to the simulation and initializes it.
		/// </summary>
		/// <param name="id">Id of the server</param>
		public void AddServerCore(SourceReference id)
		{
			var newServerCore = new ServerCore(new ReferenceManager($"res{simulationInstances.Count}"))
			{Id=id};
			AddCore(newServerCore);

			newServerCore.SetCommandsLive();
			
		}

		/// <summary>
		/// Adds a new Client to the simulation
		/// </summary>
		/// <param name="id">Id of the client</param>
		public void AddClientCore(SourceReference id)
		{
			var newClientCore = new ClientCore(new CommandController(globalCommands),ClientSocket.Instance,new ClientReferenceManager($"res{simulationInstances.Count}"))
			{Id=id};
			UserService.Instance.ClientCoreInstance = newClientCore;
			var addedInstance = AddCore(newClientCore);
			// activate
			newClientCore.SetCommandsLive();
			lastAddedClient=addedInstance;
		}

		/// <summary>
		/// Adds a core to the simulation
		/// </summary>
		/// <param name="core">CoflnetCore to add </param>
		public SimulationInstance AddCore(CoflnetCore core)
		{
			var newInstance = new SimulationInstance(){
				core = core
			};
			this.simulationInstances.Add(core.Id,newInstance);
			return newInstance;
		}

		public override CommandController GetCommandController () {
			return globalCommands;
		}

		private static int executionCount = 0;

		private SourceReference lastExecutor;

		/// <summary>
		/// Will execute commands on the simulated cores
		/// </summary>
		/// <param name="data">Data to send</param>
		/// <param name="serverId">optional serverId, ignored in this implementation</param>
		public override void SendCommand (MessageData data, long serverId = 0) {

			UnityEngine.Debug.Log("Devcore tries to execute " + data);

			if (executionCount > 100) {
				throw new Exception ($"to many commands, probalby a loop {data}");
			}
			executionCount++;

			// guess sender if there is none
			if (data.rId == userId) {
				data.sId = new SourceReference (data.rId.ServerId, 0);
			}

			var serverData =  new DevMessageData(data);
				serverData.Connection = new DevConnection();
				data = serverData;
			

			if(data.t == "registerUser" || data.t == "loginUser" || data.t == "response"){

				serverData.sender = lastAddedClient;
			}

			if (data.t == "registeredUser" || data.t == "loginUserResponse" || data.t =="response") {
				data.rId = ConfigController.ActiveUserId;
				
				//
			}

			//UnityEngine.Debug.Log(data);

			if(simulationInstances.ContainsKey(data.rId)){
				data.CoreInstance=simulationInstances[data.rId].core;
				simulationInstances[data.rId].core.ReferenceManager.ExecuteForReference(data);
				
			} else if(simulationInstances.ContainsKey(SourceReference.Default) && simulationInstances[SourceReference.Default].core.Id == data.rId)
			{
				simulationInstances[data.rId] = simulationInstances[SourceReference.Default];
				data.CoreInstance=simulationInstances[data.rId].core;
				simulationInstances[data.rId].core.ReferenceManager.ExecuteForReference(data);
			}
			else if(simulationInstances.ContainsKey(data.rId.FullServerId)){
				UnityEngine.Debug.Log("abc " + data);

				foreach (var item in simulationInstances.Keys)
				{
					UnityEngine.Debug.Log("abc " + item);
				}

				data.CoreInstance=simulationInstances[data.rId.FullServerId].core;
				simulationInstances[data.rId.FullServerId].core.ReferenceManager.ExecuteForReference(data);
				
			}else if(data is DevMessageData && (data as DevMessageData).sender != null){
				// depending on the sending type this might not help
				(data as DevMessageData).sender.core.ReferenceManager.ExecuteForReference(data);
			}
			else{
				throw new Exception ($"the target {data.rId} is not registered in the development enviroment {data.t}");
			}


/* 
			UnityEngine.Debug.Log ($"executing for reference {data.t}");
			if (data.rId == ConfigController.ActiveUserId || data.rId == ConfigController.ApplicationSettings.id)
				ReferenceManager.Instance.ExecuteForReference (data);
			else {
				throw new Exception ($"the target {data.rId} is not registered in the development enviroment {data.t}");
			}*/
		}

		/// <summary>
		/// Messagedata used within the development enviroment.
		/// Useful for knowing who sent int in the simulated  enviroment before ids are set
		/// </summary>
		class DevMessageData : ServerMessageData
		{
			// TODO
			public SimulationInstance sender;

			public DevMessageData(MessageData normal,SimulationInstance sender = null) : base(normal)
			{
				this.sender = sender;
			}

			public override void SendBack(MessageData data)
			{
				if(sender != null){
					UnityEngine.Debug.Log($"on {sender.core.Id} {sender.core.GetType().Name} {data} ");
					data.CoreInstance = sender.core;
					data.sId = rId;//sender.core.Id;
					sender.core.ReferenceManager.ExecuteForReference(data);
				} else {
					UnityEngine.Debug.Log("normal send back");
					base.SendBack(data);
				}
			}
		}

		public override void SendCommand<C, T> (SourceReference receipient, T data, long id = 0, SourceReference sender = default(SourceReference)) {
			UnityEngine.Debug.Log ($"executing for ");
			var commandInstance = ((C) Activator.CreateInstance (typeof (C)));

			var messageData = MessageData.SerializeMessageData<T> (data, commandInstance.GetSlug (), id);

			messageData.rId = receipient;
			messageData.sId = sender;

			 
			if (receipient.ServerId == this.Id.ServerId && commandInstance.Settings.LocalPropagation) {
				messageData.CoreInstance = simulationInstances[messageData.rId].core;
				UnityEngine.Debug.Log("using thread");
				ThreadController.Instance.ExecuteCommand (commandInstance, messageData);
			}

			SendCommand (messageData);
		}

		public override void SendCommand<C> (SourceReference receipient, byte[] data) {
			var commandInstance = ((C) Activator.CreateInstance (typeof (C)));
			var messageData = new MessageData (receipient, data, commandInstance.GetSlug ());

			SendCommand (messageData);
		}
	}

    class DevConnection : IClientConnection
    {
        public CoflnetUser User
        {
            get;set;
        }

        public Device Device { get;set; }
        public List<SourceReference> AuthenticatedIds { get;set; }

        public CoflnetEncoder Encoder => CoflnetEncoder.Instance;

        public void SendBack(MessageData data)
        {
			var temp = data.rId;
			data.rId = data.sId;
			data.sId = temp;
			UnityEngine.Debug.Log("sending now to " + data.rId);
            DevCore.Instance.SendCommand(data);
        }
    }



	public class SimulationInstance 
	{
		public CoflnetCore core {set;get;}
	}
}