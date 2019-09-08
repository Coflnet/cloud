using Coflnet;
using MessagePack;

namespace Coflnet {
	/// <summary>
	/// Receiveable resource.
	/// Represents a <see cref="Referenceable"/> that is capeable of receiving commands on its own
	/// </summary>
	[MessagePackObject]
	public abstract class ReceiveableResource : Referenceable {
		protected static CommandController persistenceCommands;

		/// <summary>
		/// Public Signing key of the resource
		/// </summary>
		[Key("pk")]
		public byte[] publicKey;

		static ReceiveableResource () {
			persistenceCommands = new CommandController ();
			persistenceCommands.RegisterCommand<GetMessages> ();
		}

		public override Command ExecuteCommand (MessageData data) {
			UnityEngine.Debug.Log ("running receivable");
			// each incoming command will be forwarded to the resource
			try {
				var command = base.ExecuteCommand (data);
				if (command.Settings.Distribute) {
					UnityEngine.Debug.Log ($"sending command ");
					CoflnetCore.Instance.SendCommand (data);
				}
				return command;
			} catch (CommandUnknownException e) {
				UnityEngine.Debug.Log ($"didn't find Command {e.Slug} ");
				
				// this command is unkown to the us, if we are not the target persist it and send it later
				if(data.rId != data.CoreInstance.Id)
					MessageDataPersistence.Instance.SaveMessage(data);

			}
			return null;
		}

		public ReceiveableResource (SourceReference owner) : base (owner) { }

		public ReceiveableResource () : base () { }

		public class GetMessages : Command {
			public override void Execute (MessageData data) {
				foreach (var item in MessageDataPersistence.Instance.GetMessagesFor (data.rId)) {
					data.SendBack (item);
				}
			}

			protected override CommandSettings GetSettings () {
				return new CommandSettings (IsSelfPermission.Instance);
			}

			public override string Slug {
				get {

					return "getMessages";
				}
			}

		}

		/// <summary>
		/// Will send the provided Command and replace the <see cref="MessageData.sId"/> with the id of the resource
		/// </summary>
		/// <param name="data"></param>
		public void SendCommand (MessageData data) {
			data.sId = this.Id;
			CoflnetCore.Instance.SendCommand (data);
		}
	}
}