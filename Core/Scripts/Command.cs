using MessagePack;

namespace Coflnet
{
    public abstract class Command {
		public delegate void CommandMethod (MessageData messageData);

		private CommandSettings _settings;

		/// <summary>
		/// Gets the command settings.
		/// </summary>
		/// <value>The settings.</value>
		public CommandSettings Settings {
			get {
				if (_settings == null)
					_settings = GetSettings ();
				return _settings;
			}
		}

		/// <summary>
		/// Invokes the actual code to do some logic
		/// </summary>
		/// <param name="data">Data.</param>
		public abstract void Execute (MessageData data);
		/// <summary>
		/// Returns an unique identifier for this command.
		/// Usually the namespace + the class name if not too long.
		/// try to stay under 16 characters
		/// </summary>
		/// <returns>The slug.</returns>
		public abstract string Slug { get; }
		/// <summary>
		/// Gets the command settings for this command.
		/// Will generate a new Settings Object. 
		/// Use the attribute Settings to get the caches settings.
		/// </summary>
		/// <returns>The settings.</returns>
		protected abstract CommandSettings GetSettings ();

		/// <summary>
		/// Sends a command to the server
		/// </summary>
		/// <param name="data">Data.</param>
		/// <param name="type">Type.</param>
		public void SendToServer (byte[] data, string type) {
			//ServerController.Instance.SendCommandToServer(new MessageData(type,data), server);
		}

		/// <summary>
		/// Sends a command to a specific server
		/// </summary>
		/// <param name="serverId">Server identifier.</param>
		/// <param name="data">Data.</param>
		/// <param name="type">Type.</param>
		public void SendToServer (long serverId, byte[] data, string type) {
			ServerController.Instance.SendCommandToServer (new MessageData (type, data), serverId);
		}

		/// <summary>
		/// Sends a command to another user
		/// </summary>
		/// <param name="uId">U identifier.</param>
		/// <param name="data">Data.</param>
		/// <param name="type">Type.</param>
		public void SendToUser (string uId, byte[] data, string type) {
			//UserController.instance.SendToUser(data);
		}

		/// <summary>
		/// Response to some previous command.
		/// </summary>
		[MessagePackObject]
		public class Response {
			[Key (0)]
			public long Id;
			[Key (1)]
			public byte[] data;

			public Response (long id, byte[] data) {
				Id = id;
				this.data = data;
			}
		}

		/// <summary>
		/// Custom Settings for command.
		/// Has the command to be executed on the main thread (ui)?
		/// Is it changing and has it to be distributed?
		/// Does it have to be encrypted?
		/// Can and should it be ran locally first, bevore the managing node (eg. to update the ui)? 
		/// </summary>
		public class CommandSettings {
			/// <summary>
			/// Wherether this command can be run in multiple threads at the same time
			/// or if that would create race conditions.
			/// </summary>
			public bool ThreadSave {
				get;
				private set;
			}

			/// <summary>
			/// Gets or sets a value indicating whether the target resource will be changed by this command.
			/// </summary>
			/// <value><c>true</c> if command updates the resource and has to be distributed; otherwise, <c>false</c>.</value>
			public bool Distribute {
				get;
				protected set;
			}

			/// <summary>
			/// Wherether or not this command is end-to-end encrypted
			/// </summary>
			public bool Encrypted {
				get;
				private set;
			}

			/// <summary>
			/// Gets or sets a value indicating whether this <see cref="T:Coflnet.Command.CommandSettings"/> should be executed locally (and on eg other devices from the same user) if resource is present locally.
			/// Eg username update -> should also update local username instantly.
			/// This may result in command executed twice!
			/// So don't use this for in/decrementing counters or similar
			/// </summary>
			/// <value><c>true</c> if command should be executed locally before sending or not; otherwise, <c>false</c>.</value>
			public bool LocalPropagation {
				get;
				protected set;
			}


			/// <summary>
			/// Wherether or not to confirm the receiveiment of the command.
			/// If set to <see cref="false"/> the command has to send the acknowledgement itself
			/// </summary>
			/// <value><c>true</c> if the receipient should not automatically confirm the executionof the command.</value>
			public bool DisableExecuteConfirm {
				get;
				protected set;
			}

			/// <summary>
			/// Gets the permissions needed for this command
			/// </summary>
			/// <value>The permissions which need to execute as true for this command to be executed.</value>
			public Permission[] Permissions {
				get;
				private set;
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="T:Coflnet.Command.CommandSettings"/> class.
			/// </summary>
			/// <param name="threadSave">Set to <c>true</c> if command can be run in multiple threads at the same time. Set it to false if you interact with the UI.</param>
			/// <param name="encrypted">Set to <c>true</c> if command is end-to-end encrypted and should be decrypted prior to execution.</param>
			/// <param name="localPropagation">Set to <c>true</c>if command should also be executed locally if resource is present. Should be true if update to the ui is required.</param>
			public CommandSettings (bool threadSave = false, bool encrypted = false, bool localPropagation = false) {
				ThreadSave = threadSave;
				Encrypted = encrypted;
				LocalPropagation = localPropagation;
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="T:Coflnet.Command.CommandSettings"/> class.
			/// </summary>
			/// <param name="threadSave">If set to <c>true</c> thread save.</param>
			/// <param name="encrypted">If set to <c>true</c> encrypted.</param>
			/// <param name="localPropagation">If set to <c>true</c> local propagation.</param>
			/// <param name="permissions">Permissions.</param>
			public CommandSettings (bool threadSave, bool encrypted, bool localPropagation, params Permission[] permissions) {
				ThreadSave = threadSave;
				Encrypted = encrypted;
				LocalPropagation = localPropagation;
				Permissions = permissions;
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="T:Coflnet.Command.CommandSettings"/> class.
			/// </summary>
			/// <param name="threadSave">If set to <c>true</c> command is thread save.</param>
			/// <param name="distribute">If set to <c>true</c> is changing (will be distributed).</param>
			/// <param name="encrypted">If set to <c>true</c> encrypted.</param>
			/// <param name="localPropagation">If set to <c>true</c> local propagation.</param>
			/// <param name="permissions">Permissions.</param>
			public CommandSettings (bool threadSave, bool distribute, bool encrypted, bool localPropagation, params Permission[] permissions) {
				ThreadSave = threadSave;
				Distribute = distribute;
				Encrypted = encrypted;
				LocalPropagation = localPropagation;
				Permissions = permissions;
			}


						/// <summary>
			/// Initializes a new instance of the <see cref="T:Coflnet.Command.CommandSettings"/> class.
			/// </summary>
			/// <param name="threadSave">If set to <c>true</c> command is thread save.</param>
			/// <param name="distribute">If set to <c>true</c> is changing (will be distributed).</param>
			/// <param name="encrypted">If set to <c>true</c> encrypted.</param>
			/// <param name="localPropagation">If set to <c>true</c> local propagation.</param>
			/// <param name="disableExecuteConfirm">If set to <c>true</c> execution of the command wont be confirmed.</param>
			/// <param name="permissions">Permissions.</param>
			public CommandSettings (bool threadSave, bool distribute, bool encrypted, bool localPropagation,bool disableExecuteConfirm, params Permission[] permissions): this(threadSave,distribute,encrypted,localPropagation,permissions) {
				this.DisableExecuteConfirm = disableExecuteConfirm;
			}

			public CommandSettings (params Permission[] permissions) {
				Permissions = permissions;
			}

		}
	}

}