using System;
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
				ServerCore.ServerInstance = new ServerCoreProxy();
				ClientCore.ClientInstance = new ClientCoreProxy();
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
		public SimulationInstance AddClientCore(SourceReference id)
		{
			var newClientCore = new ClientCoreProxy(new CommandController(globalCommands),ClientSocket.Instance,new ClientReferenceManager($"res{simulationInstances.Count}"))
			{Id=id};
			UserService.Instance.ClientCoreInstance = newClientCore;
			var addedInstance = AddCore(newClientCore);
			// activate
			newClientCore.SetCommandsLive();
			lastAddedClient=addedInstance;

			return addedInstance;
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

			var devData =  new DevMessageData(data);
				devData.Connection = new DevConnection();
				data = devData;
			

			if(data.t == "registerUser" || data.t == "loginUser" || data.t == "response"){

				devData.sender = lastAddedClient;
			}

			if (data.t == "registeredUser" || data.t == "loginUserResponse" || data.t =="response") {
				data.rId = ConfigController.ActiveUserId;
				
				//
			}

			//UnityEngine.Debug.Log(data);

			if(simulationInstances.ContainsKey(data.rId)){
				// the receiver is known, send it to him
				simulationInstances[data.rId].ReceiveCommand(devData);
				
			} else if(simulationInstances.ContainsKey(SourceReference.Default) && simulationInstances[SourceReference.Default].core.Id == data.rId)
			{
				// the receiver is unknown but is asigned the last added client since it hasn't got an ID yet
				simulationInstances[data.rId] = simulationInstances[SourceReference.Default];
				simulationInstances[data.rId].ReceiveCommand(devData);
			}
			else if(simulationInstances.ContainsKey(data.rId.FullServerId)){
				// the receiver itself doesn't exist, but the server for it does
				simulationInstances[data.rId.FullServerId].ReceiveCommand(devData);
				
			}else if(data is DevMessageData && (data as DevMessageData).sender != null){
				// no idea what id this is supposed to go but the container has a sender
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

	public class DevMessageData : ServerMessageData
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

	public class ServerCoreProxy : ServerCore
	{
		public override void SendCommand(MessageData data, long serverId = 0)
		{
			// set the correct sender
			data.sId = this.Id;
			// go around the network 
			DevCore.DevInstance.SendCommand(data,serverId);
		}

		public ServerCoreProxy(ReferenceManager referenceManager) : base(referenceManager)
        {
        }

        public ServerCoreProxy()
        {
        }
    }

    public class ClientCoreProxy : ClientCore
    {
        public ClientCoreProxy()
        {
        }

        public ClientCoreProxy(CommandController commandController, ClientSocket socket) : base(commandController, socket)
        {
        }

        public ClientCoreProxy(CommandController commandController, ClientSocket socket, ReferenceManager manager) : base(commandController, socket, manager)
        {
        }

		public override void SendCommand(MessageData data, long serverId = 0)
		{
			// set the correct sender
			data.sId = this.Id;
			// go around the network 
			DevCore.DevInstance.SendCommand(data,serverId);
		}

		public override void SendCommand<C, T>(SourceReference receipient, T data, long id = 0, SourceReference sender = default(SourceReference))
		{
			
			DevCore.DevInstance.SendCommand<C,T>(receipient,data,id,sender);
		}
    }


}