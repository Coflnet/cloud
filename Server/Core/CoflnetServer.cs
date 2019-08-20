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
			CompositeResolver.RegisterAndSetAsDefault(PrimitiveObjectResolver.Instance, StandardResolver.Instance);

			Commands = new CommandController(globalCommands);
			ServerInstance = new ServerCore();
			Instance = ServerInstance;
			Instance.Id = ConfigController.ApplicationSettings.id;




			// chang messagedata persistence
			MessageDataPersistence.Instance = MessagePersistence.ServerInstance;
		}

		public ServerCore() : this(ReferenceManager.Instance)
		{

		}

		public ServerCore(ReferenceManager referenceManager)
		{
			this.ReferenceManager = referenceManager;
			this.ReferenceManager.coreInstance = this;
		}


		/// <summary>
		/// Initializes this instance.
		/// Should be called on startup of the application will reset the server if called twice
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
		public static void Init(SourceReference serverId)
		{
			Instance.Id = serverId;
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
			Commands.RegisterCommand<ReferenceManager.UpdateResourceCommand>();
			Commands.RegisterCommand<Sub2Command>();

			foreach (var item in CoreExtentions.Commands)
			{
				item.RegisterCommands(Commands);
			}

			foreach (var item in ExtraModules.Commands)
			{
				item.RegisterCommands(Commands);
			}
			this.ReferenceManager.AddReference(this);
			UnityEngine.Debug.Log($"Set Live {this.Id}");
		}

		public override CommandController GetCommandController()
		{
			return Commands;
		}


		public override void SendCommand(MessageData data, long serverId = 0)
		{
			if (CoflnetSocket.TrySendCommand(data, serverId))
				return;

			UnityEngine.Debug.Log("couldn't send " + data.t);

			// Command couldn't be sent we have to persist it         
			MessagePersistence.ServerInstance.SaveMessage(data);
		}

		public override void SendCommand<C, T>(SourceReference receipient, T data, long id = 0, SourceReference sender = default(SourceReference))
		{
			var commandInstance = ((C)Activator.CreateInstance(typeof(C)));

			var messageData = MessageData.SerializeMessageData<T>(data, commandInstance.Slug, id);

			messageData.rId = receipient;
			messageData.sId = sender;


			if (receipient.ServerId == this.Id.ServerId && commandInstance.Settings.LocalPropagation)
			{

				ThreadController.Instance.ExecuteCommand(commandInstance, messageData);
			}

			SendCommand(messageData);
		}

		public override void SendCommand<C>(SourceReference receipient, byte[] data)
		{
			var commandInstance = ((C)Activator.CreateInstance(typeof(C)));
			var messageData = new MessageData(receipient, data, commandInstance.Slug);

			SendCommand(messageData);
		}



	}




}
