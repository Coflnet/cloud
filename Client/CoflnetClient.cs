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

		public override void SendCommand(MessageData data)
		{
			socket.SendCommand(data);
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


