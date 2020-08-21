using Coflnet;
using MessagePack;

namespace Coflnet {
	/// <summary>
	/// Can't be a ServerCommand because no user yet exists
	/// </summary>   
	public class ReceiveConfirm : Command {
		/// <summary>
		/// Static CommandSlug to access it from code
		/// </summary>
		public static string CommandSlug => "receivedCommand";

		public override void Execute (CommandData data) {
			var dataParams = data.GetAs<ReceiveConfirmParams> ();

			CommandDataPersistence.Instance.Remove (data.SenderId, dataParams.sender, dataParams.messageId);
		}

		protected override CommandSettings GetSettings () {
			return new CommandSettings (true,false,false,false,true);
		}

		public override string Slug => CommandSlug;

	}

	[MessagePackObject]
	public class ReceiveConfirmParams {
		[Key (0)]
		public EntityId sender;
		[Key (1)]
		public long messageId;

		public ReceiveConfirmParams (EntityId sender, long messageId) {
			this.sender = sender;
			this.messageId = messageId;
		}

		public ReceiveConfirmParams () { }
	}

}