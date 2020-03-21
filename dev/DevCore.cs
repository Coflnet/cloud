﻿using System;
using System.Collections.Generic;
using System.Linq;
using Coflent.Client;
using Coflnet.Client;
using Coflnet.Server;

namespace Coflnet.Dev
{
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

		/// <summary>
		/// All messages sent over the network previously
		/// </summary>
		public List<MessageData> pastMessages = new List<MessageData>();


		public static DevCore DevInstance {get;private set;}

		static DevCore () {

			// postfix the datapath to not corrupt other data
			FileController.dataPaht += "/dev";
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
		/// <param name="preventDefaultScreens"><c>true</c> when default settings (dummys) should NOT be set such as <see cref="DummyPrivacyScreen"/></param>
		/// <param name="preventInit"><c>true</c> when The Inits of Client and Server-Cores should not be invoked and client should be prepared like a fresh install</param>
		public static void Init (SourceReference id, bool preventDefaultScreens = false, bool preventInit = false) {
			
			//[Deprecated]
			ConfigController.ActiveUserId = id;
			// sets the primary managing server
			ConfigController.ApplicationSettings.id = id.FullServerId;


			UnityEngine.Debug.Log($"setting up with {id}");

			if (!preventDefaultScreens) {
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
				ServerCore.ServerInstance = new ServerCoreProxy();
				ClientCore.ClientInstance = new ClientCoreProxy();
			}

			DevInstance.AddServerCore(id.FullServerId );
			if(!id.IsServer){
				DevInstance.AddClientCore(id);
			} else {
				DevInstance.AddClientCore(SourceReference.Default);
			}

			if(!preventInit)
			{
				ServerCore.Init ();
				ClientCore.Init ();
			}

		}

		/// <summary>
		/// Adds a new Server to the simulation and initializes it.
		/// </summary>
		/// <param name="id">Id of the server</param>
		public SimulationInstance AddServerCore(SourceReference id)
		{
			var newServerCore = new ServerCoreProxy(new ReferenceManager($"res{simulationInstances.Count}"))
			{Id=id};
			var simulationInstance =AddCore(newServerCore);

			newServerCore.SetCommandsLive();

			return simulationInstance;
			
		}

		/// <summary>
		/// Adds a new Client to the simulation
		/// </summary>
		/// <param name="id">Id of the client</param>
		/// <param name="createDevice">If an instance of <see cref="CoflnetUser"/> should be created on  the server as well</param>
		public SimulationInstance AddClientCore(SourceReference id, bool createDevice = false)
		{
			var newClientCore = new ClientCoreProxy(new CommandController(CoreCommands),ClientSocket.Instance,new ClientReferenceManager($"res{simulationInstances.Count}"))
			{Id=id};

			SetCoreForService(newClientCore);
			// for Services using the default Instance
			ClientCore.Instance = newClientCore;



			var addedInstance = AddCore(newClientCore);


			if(createDevice)
			{
				// create and add the user server and client side
				var device = new Device(){Id=id};
				SimulationInstance server;
				if(simulationInstances.TryGetValue(id.FullServerId,out server))
				{
					server.core.ReferenceManager.AddReference(device);
				}
				UnityEngine.Debug.Log("Added device " + device.Id);
				if(!newClientCore.ReferenceManager.AddReference(device)){
					UnityEngine.Debug.Log(newClientCore.ReferenceManager.GetReferences());
					throw new Exception($"failed to add device {device.Id}");
				}
			}

			// activate commands
			newClientCore.SetCommandsLive();
			lastAddedClient=addedInstance;

			

			return addedInstance;
		}


		public void SetCoreForService(ClientCore core)
		{

			UserService.Instance.ClientCoreInstance = core;
			InstallService.Instance = new InstallService(core);
			DeviceService.Instance = new DeviceService(core);
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

			// record it
			pastMessages.Add(data);
			
			if(data.sId == data.rId)
			{
				// resource is trying to send to itself
				serverId = data.rId.ServerId;
			}

			if (executionCount > 100) {
				throw new Exception ($"to many commands, probalby a loop {data}");
			}
			executionCount++;

			// guess sender if there is none
			if (data.rId == userId) {
				data.sId = new SourceReference (data.rId.ServerId, 0);
			}

			var devData =  new DevMessageData(data);
				devData.Connection = new DevConnection();
				data = devData;
			
			/*
			if(data.type == "registerUser" || data.type == "loginUser" || data.type == "response"){

				devData.sender = lastAddedClient;
			}

			if (data.type == "registeredUser" || data.type == "loginUserResponse" || data.type =="response") {
				data.rId = ConfigController.ActiveUserId;
				
				//
			}*/

			//UnityEngine.Debug.Log(data);
			// search for the serverId first
			if(serverId != 0 && simulationInstances.ContainsKey(new SourceReference(serverId,0))){
				UnityEngine.Debug.Log($"on {serverId.ToString("X")} ");
				simulationInstances[new SourceReference(serverId,0)].ReceiveCommand(devData);
			}
			else if(simulationInstances.ContainsKey(data.rId)){
				UnityEngine.Debug.Log($"on {data.rId} ");
				// the receiver is known, send it to him
				simulationInstances[data.rId].ReceiveCommand(devData);

			} else if(simulationInstances.ContainsKey(SourceReference.Default)
				|| simulationInstances.Where(i=>i.Value.core.Id == data.rId).Any())// && simulationInstances[SourceReference.Default].core.Id == data.rId)
			{
				// the receiver is unknown but is asigned the last added client since it hasn't got an ID yet
				SimulationInstance value;
				
				if(! simulationInstances.TryGetValue(default(SourceReference),out value))
				{
					value = simulationInstances.Where(i=>i.Value.core.Id == data.rId).First().Value;
				}

				simulationInstances[data.rId] = value;
				simulationInstances[data.rId].ReceiveCommand(devData);


				simulationInstances.Remove(SourceReference.Default);
				
			}
			else if(simulationInstances.ContainsKey(data.rId.FullServerId)){
				// the receiver itself doesn't exist, but the server for it does
				simulationInstances[data.rId.FullServerId].ReceiveCommand(devData);
				
			}else if(data is DevMessageData && (data as DevMessageData).sender != null){
				// no idea what id this is supposed to go but the container has a sender
				(data as DevMessageData).sender.core.ReferenceManager.ExecuteForReference(data);
			}
			else{
				throw new Exception ($"the target {data.rId} is not registered in the development enviroment {data.type}");
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
		

		public override void SendCommand<C, T> (SourceReference receipient, T data, long id = 0, SourceReference sender = default(SourceReference)) {
			UnityEngine.Debug.Log ($"executing for ");
			var commandInstance = ((C) Activator.CreateInstance (typeof (C)));

			var messageData = MessageData.SerializeMessageData<T> (data, commandInstance.Slug, id);

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
			var messageData = new MessageData (receipient, data, commandInstance.Slug);

			SendCommand (messageData);
		}
	}


}