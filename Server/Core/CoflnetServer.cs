﻿using System;
using MessagePack.Resolvers;

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
			CoflnetSocket.socketServer.Start();
			Commands = new CommandController();
			ServerInstance = new ServerCore();
			Instance = ServerInstance;
			Instance.Id = ConfigController.ApplicationSettings.id;
		}

		/// <summary>
		/// Initializes this instance.
		/// Should be called on startup of the application
		/// </summary>
		public static void Init()
		{
			ServerInstance.SetCommandsLive();
			Coflnet.ServerController.Instance = Coflnet.Server.ServerController.ServerInstance;
		}

		protected void SetCommandsLive()
		{
			Commands.RegisterCommand<RegisterUser>();
			ReferenceManager.Instance.AddReference(this);
		}

		public override CommandController GetCommandController()
		{
			return Commands;
		}


		public override void SendCommand(MessageData data)
		{
			if (CoflnetSocket.TrySendCommand(data))
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


		public class RegisterUser : ServerCommand
		{
			public override void Execute(MessageData data)
			{
				var sdata = data as ServerMessageData;
				sdata.Connection.SendBack(data);
			}

			public override ServerCommandSettings GetServerSettings()
			{
				return new ServerCommandSettings();
			}

			public override string GetSlug()
			{
				return "registerUser";
			}
		}
	}




}