using System;
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
    public class DevCore : CoflnetCore
    {
        /// <summary>
        /// This is the user and the serverid simulated
        /// </summary>
        private EntityId userId;

        private SimulationInstance lastAddedClient;

        /// <summary>
        /// Cotains all simulated devices/server/users
        /// </summary>
        public Dictionary<EntityId, SimulationInstance> simulationInstances;

        /// <summary>
        /// All messages sent over the network previously
        /// </summary>
        public List<CommandData> pastMessages = new List<CommandData>();

        public static DevCore DevInstance { get; private set; }

        static DevCore()
        {

            // postfix the datapath to not corrupt other data
            FileController.dataPath += "/dev";
        }

        public DevCore()
        {
            this.EntityManager = EntityManager.Instance;
        }

        private class DummyPrivacyScreen : IPrivacyScreen
        {
            public void ShowScreen(Action<int> whenDone)
            {
                return;
            }
        }

        /// <summary>
        /// Will add default screens
        /// </summary>
        public static void SetupDefaults()
        {
            // configure cores
            PrivacyService.Instance.privacyScreen = new DummyPrivacyScreen();
        }

        /// <summary>
        /// Initialized the global <see cref="CoflnetCore.Instance"/> as a `devCore`.
        /// Will reset the development enviroment when called again (to support multiple unit tests)
        /// </summary>
        /// <param name="id">Application/Server Id to use</param>
        /// <param name="preventDefaultScreens"><c>true</c> when default settings (dummys) should NOT be set such as <see cref="DummyPrivacyScreen"/></param>
        /// <param name="preventInit"><c>true</c> when The Inits of Client and Server-Cores should not be invoked and client should be prepared like a fresh install</param>
        /// <param name="testSetup"><c>true</c> when initialization should be skipped. (preconfigure testing enviroment)</param>
        public static void Init(EntityId id, bool preventDefaultScreens = false, bool preventInit = false, bool testSetup = false)
        {

            //[Deprecated]
            ConfigController.ActiveUserId = id;
            // sets the primary managing server
            ConfigController.ApplicationSettings.id = id.FullServerId;

            if (!preventDefaultScreens)
            {
                SetupDefaults();
            }

            // to have instances loaded
            var toLoadInstance = ClientCore.ClientInstance;
            var serverInstance = ServerCore.ServerInstance;
            if (DevInstance == null)
            {
                DevInstance = new DevCore();
                DevInstance.simulationInstances = new Dictionary<EntityId, SimulationInstance>();
                CoflnetCore.Instance = DevInstance;
            }
            else
            {
                // reset if there was an devinstance bevore
                DevInstance.simulationInstances.Clear();
                ServerCore.ServerInstance = new ServerCoreProxy();
                ClientCore.ClientInstance = new ClientCoreProxy();
            }

            DevInstance.AddServerCore(id.FullServerId);
            if (!id.IsServer)
            {
                DevInstance.AddClientCore(id, testSetup);
                // we are the client
                if (testSetup)
                    DevInstance.Id = id;
            }
            else
            {
                DevInstance.AddClientCore(EntityId.Default);
            }

            if (!preventInit)
            {
                ServerCore.Init();
                ClientCore.Init();
            }

            Instance = DevInstance;
        }

        /// <summary>
        /// Adds a new Server to the simulation and initializes it.
        /// </summary>
        /// <param name="id">Id of the server</param>
        public SimulationInstance AddServerCore(EntityId id)
        {
            var newServerCore = new ServerCoreProxy(new EntityManager($"res{simulationInstances.Count}")) { Id = id };
            var simulationInstance = AddCore(newServerCore);

            newServerCore.SetCommandsLive();

            return simulationInstance;

        }

        /// <summary>
        /// Adds a new Client to the simulation
        /// </summary>
        /// <param name="id">Id of the client</param>
        /// <param name="createDevice">If an instance of <see cref="CoflnetUser"/> should be created on  the server as well</param>
        public SimulationInstance AddClientCore(EntityId id, bool createDevice = false)
        {
            var newClientCore = new ClientCoreProxy(new CommandController(CoreCommands), ClientSocket.Instance, new ClientReferenceManager($"res{simulationInstances.Count}")) { Id = id };

            SetCoreForService(newClientCore);
            // for Services using the default Instance
            ClientCore.Instance = newClientCore;

            var addedInstance = AddCore(newClientCore);

            if (createDevice)
            {
                // create and add the user server and client side
                var device = new Device() { Id = id };
                SimulationInstance server;
                if (simulationInstances.TryGetValue(id.FullServerId, out server))
                {
                    server.core.EntityManager.AddReference(device);
                }
                if (!newClientCore.EntityManager.AddReference(device))
                {
                    throw new Exception($"failed to add device {device.Id}");
                }
            }

            // activate commands
            newClientCore.SetCommandsLive();
            lastAddedClient = addedInstance;

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
            var newInstance = new SimulationInstance()
            {
                core = core
            };
            this.simulationInstances.Add(core.Id, newInstance);
            return newInstance;
        }

        public override CommandController GetCommandController()
        {
            return globalCommands;
        }

        private static int executionCount = 0;


        /// <summary>
        /// Will execute commands on the simulated cores
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <param name="serverId">optional serverId, ignored in this implementation</param>
        public override void SendCommand(CommandData data, long serverId = 0)
        {

            // record it
            pastMessages.Add(data);

            if (data.SenderId == data.Recipient)
            {
                // resource is trying to send to itself
                serverId = data.Recipient.ServerId;
            }

            if (executionCount > 100)
            {
                throw new Exception($"to many commands, probalby a loop {data}");
            }
            executionCount++;

            // guess sender if there is none
            if (data.Recipient == userId)
            {
                data.SenderId = new EntityId(data.Recipient.ServerId, 0);
            }

            var devData = new DevCommandData(data);
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

            //			// search for the serverId first
            if (serverId != 0 && simulationInstances.ContainsKey(new EntityId(serverId, 0)))
            {
                simulationInstances[new EntityId(serverId, 0)].ReceiveCommand(devData);
            }
            else if (simulationInstances.ContainsKey(data.Recipient))
            {
                // the receiver is known, send it to him
                simulationInstances[data.Recipient].ReceiveCommand(devData);

            }
            else if (simulationInstances.ContainsKey(EntityId.Default) ||
              simulationInstances.Where(i => i.Value.core.Id == data.Recipient).Any()) // && simulationInstances[SourceReference.Default].core.Id == data.rId)
            {
                // the receiver is unknown but is asigned the last added client since it hasn't got an ID yet
                SimulationInstance value;

                if (!simulationInstances.TryGetValue(default(EntityId), out value))
                {
                    value = simulationInstances.Where(i => i.Value.core.Id == data.Recipient).First().Value;
                }

                simulationInstances[data.Recipient] = value;
                simulationInstances[data.Recipient].ReceiveCommand(devData);

                simulationInstances.Remove(EntityId.Default);

            }
            else if (simulationInstances.ContainsKey(data.Recipient.FullServerId))
            {
                // the receiver itself doesn't exist, but the server for it does
                simulationInstances[data.Recipient.FullServerId].ReceiveCommand(devData);

            }
            else if (data is DevCommandData && (data as DevCommandData).sender != null)
            {
                // no idea what id this is supposed to go but the container has a sender
                (data as DevCommandData).sender.core.EntityManager.ExecuteForReference(data);
            }
            else
            {
                Logger.Error(Newtonsoft.Json.JsonConvert.SerializeObject(data));
                throw new Exception($"the target {data.Recipient} is not registered in the development enviroment {data.Type}");
            }

            /* 
									if (data.rId == ConfigController.ActiveUserId || data.rId == ConfigController.ApplicationSettings.id)
							ReferenceManager.Instance.ExecuteForReference (data);
						else {
							throw new Exception ($"the target {data.rId} is not registered in the development enviroment {data.t}");
						}*/
        }

        /// <summary>
        /// <see cref="CommandData"/> used within the development enviroment.
        /// Useful for knowing who sent int in the simulated  enviroment before ids are set
        /// </summary>

        public override void SendCommand<C, T>(EntityId receipient, T data, EntityId sender = default(EntityId), long id = 0)
        {
            var commandInstance = ((C)Activator.CreateInstance(typeof(C)));

            var commandData = CommandData.SerializeCommandData<T>(data, commandInstance.Slug, id);

            commandData.Recipient = receipient;
            if (sender == default(EntityId))
                commandData.SenderId = this.Id;
            else
                commandData.SenderId = sender;

            if (receipient.ServerId == this.Id.ServerId && commandInstance.Settings.LocalPropagation)
            {
                commandData.CoreInstance = simulationInstances[commandData.Recipient].core;
                ThreadController.Instance.ExecuteCommand(commandInstance, commandData);
            }

            SendCommand(commandData);
        }

        public CoflnetCore GetInstance(EntityId coreId)
        {
            return simulationInstances[coreId].core;
        }
    }

}