using System;
using MessagePack.Resolvers;
using System.Linq;

namespace Coflnet.Server
{
	/// <summary>
	/// Server core.
	/// Main class to interact with the coflnet server module
	/// </summary>
	public class ServerCore : CoflnetCore
	{
		public static ServerCore ServerInstance { get; protected set; }

		public static CommandController Commands
		{
			get;
			protected set;
		}



		static ServerCore()
		{
			CompositeResolver.RegisterAndSetAsDefault(PrimitiveObjectResolver.Instance, StandardResolver.Instance);

			Commands = new CommandController();
			ServerInstance = new ServerCore();
			Instance = ServerInstance;
			Instance.Id = ConfigController.ApplicationSettings.id;

			Commands.RegisterCommand<RegisterUser>();


			foreach (var item in ExtraModules.Commands)
			{
				item.RegisterCommands(Commands);
			}
		}

		/// <summary>
		/// Initializes this instance.
		/// Should be called on startup of the application
		/// </summary>
		public static void Init()
		{
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


		protected void SetCommandsLive()
		{
			ReferenceManager.Instance.AddReference(this);
		}

		public override CommandController GetCommandController()
		{
			return Commands;
		}


		public override void SendCommand(MessageData data, long serverId = 0)
		{
			if (CoflnetSocket.TrySendCommand(data, serverId))
				return;
			// Command couldn't be sent we have to persist it         
			MessagePersistence.Instance.Save(data);
		}

		public override void SendCommand<C, T>(SourceReference receipient, T data)
		{
			var commandInstance = ((C)Activator.CreateInstance(typeof(C)));

			var messageData = MessageData.SerializeMessageData<T>(data, commandInstance.GetSlug());



			if (receipient.ServerId == this.Id.ServerId && commandInstance.Settings.LocalPropagation)
			{
				ThreadController.Instance.ExecuteCommand(commandInstance, messageData);
			}

			SendCommand(messageData);
		}

		public override void SendCommand<C>(SourceReference receipient, byte[] data)
		{
			var commandInstance = ((C)Activator.CreateInstance(typeof(C)));
			var messageData = new MessageData(receipient, data, commandInstance.GetSlug());

			SendCommand(messageData);
		}



	}




}
