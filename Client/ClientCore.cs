namespace Coflnet.Client
{
	/// <summary>
	/// Coflnet client.
	/// Main class to work with from the outside.
	/// </summary>
	public class ClientCore : CoflnetCore
	{
		static void HandleReceiveMessageData(MessageData data)
		{
		}


		private CommandController commandController;

		/// <summary>
		/// Connection to a coflnet server
		/// </summary>
		private ClientSocket socket;

		public static ClientCore ClientInstance;


		public CommandController CommandController
		{
			get
			{
				return commandController;
			}
		}

		public static void Init()
		{
			ClientInstance.socket.Reconnect();
			LocalizationManager.Instance.LoadCompleted();
			ClientInstance.CheckInstallation();
		}


		public void CheckInstallation()
		{
			if (ConfigController.UserSettings.userId != SourceReference.Default)
			{
				// we are registered
				return;
			}
			// This is a fresh install, register at the managing server after showing privacy statement
			SetupStartController.Instance.Setup();

		}



		/// <summary>
		/// Stop this instance, saves data and closes connections 
		/// </summary>
		public static void Stop()
		{
			Save();
			ClientInstance.socket.Disconnect();
		}

		public static void Save()
		{
			ConfigController.Save();
		}


		static ClientCore()
		{
			ClientInstance = new ClientCore();
			Instance = ClientInstance;
			ClientSocket.Instance.AddCallback(OnMessage);
		}


		public static void OnMessage(MessageData data)
		{
			ReferenceManager.Instance.ExecuteForReference(data);
		}


		public ClientCore() : this(new CommandController(), ClientSocket.Instance)
		{
		}

		public ClientCore(CommandController commandController, ClientSocket socket)
		{
			this.commandController = commandController;
			this.socket = socket;
		}

		/// <summary>
		/// Sets the managing servers.
		/// </summary>
		/// <param name="serverIds">Server identifiers.</param>
		public void SetManagingServer(params long[] serverIds)
		{
			ConfigController.UserSettings.managingServers.AddRange(serverIds);
		}

		public override CommandController GetCommandController()
		{
			return CommandController;
		}

		public override void SendCommand(MessageData data, long serverId = 0)
		{
			try
			{
				socket.SendCommand(data);
			}
			catch (System.InvalidOperationException)
			{
				// send failed, reconnect and try again
				socket.Reconnect();
				UnityEngine.Debug.Log("Reconnecting");

				socket.SendCommand(data);
			}
		}

		public override void SendCommand<C, T>(SourceReference receipient, T data)
		{
			ServerController.Instance.SendCommand<C, T>(receipient, data);
		}

		public override void SendCommand<C>(SourceReference receipient, byte[] data)
		{
			ServerController.Instance.SendCommand<C>(receipient, data);
		}

		/// <summary>
		/// Sets the application identifier.
		/// </summary>
		/// <param name="idString">Identifier.</param>
		public void SetApplicationId(string idString)
		{
			var id = new SourceReference(idString);
			if (ConfigController.UserSettings.managingServers.Count == 0)
			{
				ConfigController.UserSettings.managingServers.Add(id.ServerId);
			}
			this.Access.Owner = id;
			ConfigController.ApplicationSettings.id = id;
		}
	}
}


