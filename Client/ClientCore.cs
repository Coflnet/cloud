namespace Coflnet.Client {
	/// <summary>
	/// Coflnet client.
	/// Main class to work with from the outside on a client device.
	/// </summary>
	public class ClientCore : CoflnetCore {
		static void HandleReceiveMessageData (MessageData data) { }

		private CommandController commandController;

		/// <summary>
		/// Connection to a coflnet server
		/// </summary>
		private ClientSocket socket;

		public static ClientCore ClientInstance;

		public CommandController CommandController {
			get {
				return commandController;
			}
		}

		static ClientCore () {
			ClientInstance = new ClientCore ();
			Instance = ClientInstance;
			// setup
			ClientSocket.Instance.AddCallback (ClientInstance.OnMessage);
		}

		public ClientCore () : this (new CommandController (globalCommands), ClientSocket.Instance) { }

		public ClientCore (CommandController commandController, ClientSocket socket) : this(commandController,socket,ReferenceManager.Instance) {
		}

		public ClientCore (CommandController commandController, ClientSocket socket, ReferenceManager manager) {
			this.commandController = commandController;
			this.socket = socket;
			socket.AddCallback(OnMessage);
			this.ReferenceManager = manager;
			this.ReferenceManager.coreInstance = this;
		}



		public static void Init () {
			ClientInstance.SetCommandsLive ();
			ClientInstance.socket.Reconnect ();
			LocalizationManager.Instance.LoadCompleted ();

			if (ClientInstance != null)
				ClientInstance.CheckInstallation ();
		}

		public void SetCommandsLive () {
			UnityEngine.Debug.Log ("setting client commands");
			foreach (var extention in ClientExtentions.Commands) {
				extention.RegisterCommands (commandController);
				UnityEngine.Debug.Log ("added client extention");
			}

			this.Id = ConfigController.ActiveUserId;
			ReferenceManager.AddReference (this);
		}

		public void CheckInstallation () {
			if (ConfigController.UserSettings != null &&
				ConfigController.UserSettings.userId.ResourceId != 0) {
				// we are registered
				return;
			}
			// This is a fresh install, register at the managing server after showing privacy statement
			FirstStartSetupController.Instance.Setup ();

		}

		/// <summary>
		/// Stop this instance, saves data and closes connections 
		/// </summary>
		public static void Stop () {
			Save ();
			ClientInstance.socket.Disconnect ();
		}

		public static void Save () {
			ConfigController.Save ();
		}

		/// <summary>
		/// Sets the managing servers.
		/// </summary>
		/// <param name="serverIds">Server identifiers.</param>
		public void SetManagingServer (params long[] serverIds) {
			ConfigController.UserSettings.managingServers.AddRange (serverIds);
		}

		public void OnMessage (MessageData data) {
			// special case before we are logged in 
			if (data.rId == SourceReference.Default) {
				ExecuteCommand (data);
			}
			ReferenceManager.ExecuteForReference (data);
		}

		/// <summary>
		/// Executes the command found in the <see cref="MessageData.t"/>
		/// Returns the <see cref="Command"/> when done
		/// </summary>
		/// <returns>The command.</returns>
		/// <param name="data">Data.</param>
		public override Command ExecuteCommand (MessageData data) {

			// special case: command targets user itself 

			var controller = GetCommandController ();
			var command = controller.GetCommand (data.t);

			controller.ExecuteCommand (command, data, this);

			return command;
		}

		public override CommandController GetCommandController () {
			return CommandController;
		}

		public override void SendCommand (MessageData data, long serverId = 0) {
			try {
				socket.SendCommand (data);
			} catch (System.InvalidOperationException) {
				// send failed, reconnect and try again
				socket.Reconnect ();
				UnityEngine.Debug.Log ("Reconnecting");

				socket.SendCommand (data);
				// persist data
				MessageDataPersistence.Instance.SaveMessage (data);
			}
		}

		public override void SendCommand<C, T> (SourceReference receipient, T data, long id = 0,SourceReference sender = default(SourceReference)) {
			ServerController.Instance.SendCommand<C, T> (receipient, data);
		}

		public override void SendCommand<C> (SourceReference receipient, byte[] data) {
			ServerController.Instance.SendCommand<C> (receipient, data);
		}

		/// <summary>
		/// Sets the application identifier.
		/// </summary>
		/// <param name="idString">Identifier.</param>
		public void SetApplicationId (string idString) {
			var id = new SourceReference (idString);
			if (ConfigController.UserSettings.managingServers.Count == 0) {
				ConfigController.UserSettings.managingServers.Add (id.ServerId);
			}
			this.Access.Owner = id;
			ConfigController.ApplicationSettings.id = id;
		}
	}
}