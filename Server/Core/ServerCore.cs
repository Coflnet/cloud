using System;
using MessagePack.Resolvers;
using System.Linq;
using Coflnet;
using Coflnet.Core.Commands;
using Coflnet.Core;

namespace Coflnet.Server
{
	/// <summary>
	/// Server core.
	/// Main class to interact with the coflnet server module
	/// </summary>
	public class ServerCore : CoflnetCore
	{
		public static ServerCore ServerInstance { get; set; }

		public static CommandController Commands
		{
			get;
			protected set;
		}



		static ServerCore()
		{
			Commands = new CommandController(CoreCommands);
			ServerInstance = new ServerCore();
			Instance = ServerInstance;
			Instance.Id = ConfigController.ApplicationSettings.id;




			// chang <see cref="CommandData"/> persistence
			CommandDataPersistence.Instance = MessagePersistence.ServerInstance;
		}

		public ServerCore() : this(EntityManager.Instance)
		{

		}

		public ServerCore(EntityManager referenceManager)
		{
			this.EntityManager = referenceManager;
			this.EntityManager.coreInstance = this;

            this.Services.AddOrOverride<ICommandTransmit>(CoflnetSocket.Instance);
		}


		/// <summary>
		/// Initializes this instance.
		/// Should be called on startup of the application, resets the server if called again
		/// </summary>
		public static void Init()
		{
			Init(ConfigController.ApplicationSettings.id);
		}


		/// <summary>
		/// Initializes this instance.
		/// Should be called on startup of the application will reset the server if called twice
		/// </summary>
		/// <param name="serverId">Server identifier.</param>
		public static void Init(EntityId serverId)
		{
			ServerInstance.Id = serverId;
			CoflnetSocket.socketServer.Start();
			ServerInstance.SetCommandsLive();
			Coflnet.ServerController.Instance = Coflnet.Server.ServerController.ServerInstance;
		}

		/// <summary>
		/// Stops the server and frees up resources
		/// Call this on Application exit
		/// </summary>
		public static void Stop()
		{
			CoflnetSocket.socketServer.Stop();
		}

		/// <summary>
		/// Loads and Sets the commands live.
		/// </summary>
		public void SetCommandsLive()
		{
			Commands.RemoveAllCommands();
			//Commands.RegisterCommand<ReceiveConfirm>();
			Commands.RegisterCommand<GetResourceCommand>();
			Commands.RegisterCommand<EntityManager.UpdateEntityCommand>();
			Commands.RegisterCommand<Sub2Command>();

			foreach (var item in CoreExtentions.Commands)
			{
				item.RegisterCommands(Commands);
			}

			foreach (var item in ExtraModules.Commands)
			{
				item.RegisterCommands(Commands);
			}
			this.EntityManager.AddReference(this);
		}

		public override CommandController GetCommandController()
		{
			return Commands;
		}


		public override void SendCommand(CommandData data, long serverId = 0)
		{
			if (CoflnetSocket.TrySendCommand(data, serverId))
				return;

			// Command couldn't be sent we have to persist it         
			MessagePersistence.ServerInstance.SaveMessage(data);
		}

		public override void SendCommand<C, T>(EntityId receipient, T data, EntityId sender = default(EntityId), long id = 0)
		{
			var commandInstance = ((C)Activator.CreateInstance(typeof(C)));

			var commandData = CommandData.SerializeCommandData<T>(data, commandInstance.Slug, id);

			commandData.Recipient = receipient;
			commandData.SenderId = sender;


			if (receipient.ServerId == this.Id.ServerId && commandInstance.Settings.LocalPropagation)
			{

				ThreadController.Instance.ExecuteCommand(commandInstance, commandData);
			}

			SendCommand(commandData);
		}
	}
}
