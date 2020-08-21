using System;
using MessagePack;

namespace Coflnet
{
    public abstract class ServerCommand : Command {
		protected override CommandSettings GetSettings () {
			return GetServerSettings ();
		}

		public abstract ServerCommandSettings GetServerSettings ();

		public new void SendToServer (long serverId, byte[] data, string type) {
			ServerController.Instance.SendCommandToServer (new CommandData (type, data), serverId);
		}

		/// <summary>
		/// Sends some data back to the user calling the command
		/// </summary>
		/// <param name="original">Original message data pointer.</param>
		/// <param name="data">Data.</param>
		public void SendBack (CommandData original, byte[] data) {
			byte[] withId = MessagePackSerializer.Serialize<Response> (new Response (original.MessageId, data));
			original.SenderId.ExecuteForEntity (new CommandData ("response", withId));
			//SendToUser(original.sId, withId, "response");
		}

		/// <summary>
		/// Sends an integer back to the user calling the command
		/// </summary>
		/// <param name="original">Original message data.</param>
		/// <param name="data">Data.</param>
		public void SendBack (CommandData original, int data) {
			SendBack (original, BitConverter.GetBytes (data));
		}

		public class ServerCommandSettings : CommandSettings {
			private short _cost;

			/// <summary>
			/// How much this command costs, is at least one.
			/// </summary>
			/// <value>The cost.</value>
			public short Cost {
				get {
					return _cost;
				}
				private set {
					_cost = value < (short) 0 ? (short) 1 : value;
				}
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="T:Coflnet.ServerCommand.ServerCommandSettings"/> class.
			/// </summary>
			/// <param name="threadSave">If set to <c>true</c> the command can be executed simotaniously in multiple threads.</param>
			/// <param name="encrypted">If set to <c>true</c> the command is end to end encrypted.</param>
			/// <param name="cost">How many usages are used by this command.</param>
			/// <param name="permissions">Permissions needed to execute the command.</param>
			public ServerCommandSettings (bool threadSave = false, bool encrypted = false, short cost = 1, params Permission[] permissions) : base (threadSave, encrypted, false, permissions) {
				Cost = cost;
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="T:Coflnet.ServerCommand.ServerCommandSettings"/> class.
			/// </summary>
			/// <param name="threadSave">If set to <c>true</c> the command is thread save.</param>
			/// <param name="cost">Cost of the command.</param>
			/// <param name="permissions">Permissions.</param>
			public ServerCommandSettings (bool threadSave = false, short cost = 1, params Permission[] permissions) : base (threadSave, false, false, permissions) {
				Cost = cost;
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="T:Coflnet.ServerCommand.ServerCommandSettings"/> class.
			/// </summary>
			/// <param name="permissions">Permissions.</param>
			public ServerCommandSettings (params Permission[] permissions) : this (false, false, 1, permissions) { }
		}
	}

}