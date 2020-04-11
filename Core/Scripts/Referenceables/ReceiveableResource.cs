using Coflnet;
using MessagePack;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Coflnet {
	/// <summary>
	/// Receiveable resource.
	/// Represents a <see cref="Referenceable"/> that is capeable of receiving/sending commands on its own
	/// </summary>
	[MessagePackObject]
	[DataContract]
	public abstract class ReceiveableResource : Referenceable {
		protected static CommandController persistenceCommands;

		/// <summary>
		/// Protects the <see cref="ReceiveableResource"/> from being spammed with unknown commands
		/// </summary>
		[Key("ca")]
		public AcceptCommandBehaviour commandAccept;

		/// <summary>
		/// Public Signing key of the resource
		/// </summary>
		[Key("pk")]
		public byte[] publicKey;

		static ReceiveableResource () {
			persistenceCommands = new CommandController (globalCommands);
			persistenceCommands.RegisterCommand<GetMessages> ();
			persistenceCommands.RegisterCommand<ReceiveConfirm>();
		}

		public override Command ExecuteCommand (MessageData data) {
			// each incoming command will be forwarded to the resource
			try {
				var command = base.ExecuteCommand (data);
				if (command.Settings.Distribute) {
					CoflnetCore.Instance.SendCommand (data);
				}
				return command;
			} catch (CommandUnknownException e) {
				
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
			data.CoreInstance.SendCommand (data);
		}
	}

	public class AcceptCommandBehaviour
	{
		/// <summary>
		/// List of <see cref="Command"/>s to include or exclude depending on <see cref="IncludeNotExclude"/>
		/// </summary>
		public HashSet<string> CommandList;

		/// <summary>
		/// <see cref="true"/> if the list is to include. 
		/// <see cref="false"/> if the <see cref="CommandList"/> has to be excluded
		/// </summary>
		public bool IncludeNotExclude = true;

		/// <summary>
		/// Determines if the Command Data should be forwareded to the resource or not
		/// </summary>
		/// <param name="data">Data to test</param>
		/// <returns><see cref="true"/> if it should be forwarded <see cref="false"/> otherwise</returns>
		public virtual bool AcceptCommand(MessageData data)
		{
			return IncludeNotExclude && CommandList.Contains(data.type) 
			|| !IncludeNotExclude && !CommandList.Contains(data.type);
		}
	}
}