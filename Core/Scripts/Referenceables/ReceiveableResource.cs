using Coflnet;
using MessagePack;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Coflnet {
	/// <summary>
	/// Receiveable resource.
	/// Represents a <see cref="Entity"/> that is capeable of receiving/sending commands on its own.
	/// eg. a <see cref="Device"/>
	/// Incomming commands are stored for as long as they aren't confirmed to be received by the target.
	/// 
	/// </summary>
	[MessagePackObject]
	[DataContract]
	public abstract class ReceiveableResource : Entity {
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

		public override Command ExecuteCommand (CommandData data,Command passedCommand = null) {
			// each incoming command will be forwarded to the resource
			try {
				var command = base.ExecuteCommand (data,passedCommand);
				if (command.Settings.Distribute) {
					CoflnetCore.Instance.SendCommand (data);
				}
				return command;
			} catch (CommandUnknownException) {
				if(data.SenderId == data.Recipient)
                    throw;
                var sent =data.CoreInstance.Services.Get<ICommandTransmit>().SendCommand(data);
                Logger.Log($"command {data.Type} not found on {this.Id}, sent {sent}");
                // this command is unkown to the us, if we are not the target persist it and send it later
                if(data.Recipient != data.CoreInstance.Id)
					CommandDataPersistence.Instance.SaveMessage(data);

			}
			return null;
		}

		public ReceiveableResource (EntityId owner) : base (owner) { }

		public ReceiveableResource () : base () { }

		public class GetMessages : Command {
			public override void Execute (CommandData data) {
				foreach (var item in CommandDataPersistence.Instance.GetMessagesFor (data.Recipient)) {
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
		/// Will send the provided Command and replace the <see cref="CommandData.SenderId"/> with the id of the resource
		/// </summary>
		/// <param name="data"></param>
		public void SendCommand (CommandData data) {
			data.SenderId = this.Id;
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
		public virtual bool AcceptCommand(CommandData data)
		{
			return IncludeNotExclude && CommandList.Contains(data.Type) 
			|| !IncludeNotExclude && !CommandList.Contains(data.Type);
		}
	}
}