namespace Coflnet.Client
{
	/// <summary>
	/// Coflnet client.
	/// Main class to work with from the outside.
	/// </summary>
	public class CoflnetClient : CoflnetCore
	{
		private CommandController commandController;

		/// <summary>
		/// Connection to a coflnet server
		/// </summary>
		private ClientSocket socket;

		public static CoflnetClient ClientInstance;


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
		}


		static CoflnetClient()
		{
			ClientInstance = new CoflnetClient();
			Instance = ClientInstance;
		}

		public CoflnetClient() : this(new CommandController(), ClientSocket.Instance)
		{
		}

		public CoflnetClient(CommandController commandController, ClientSocket socket)
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
	}
}


