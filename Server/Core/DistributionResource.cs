using System.Collections;
using System.Collections.Generic;

namespace Coflnet {
	
	/// <summary>
	/// Special <see cref="Referenceable"/> which Distributes everyCommand it receives to its members
	/// </summary>
	public class DistributionResource : Referenceable {
		protected static CommandController _commandController;

		public override CommandController GetCommandController () {
			return _commandController;
		}

		static DistributionResource () {
			_commandController = new CommandController ();
			_commandController.RegisterCommand<Distribute> ();
		}

		public class Distribute : Command {
			public override void Execute (MessageData data) {
				throw new System.NotImplementedException ();
			}

			protected override CommandSettings GetSettings () {
				return new CommandSettings ();
			}

			public override string Slug => "distribute";
		}


	}
}
/* 
	namespace Coflnet.Server {
		public class JoinChatImplementation : Coflnet.Chat.JoinChat {
			public override void Execute (MessageData data) {
				var chat = ReferenceManager.Instance.GetResource<Chat> (data.rId);
				chat.Access.Subscribers.Add (data.sId);
			}
		}
	}
	*/