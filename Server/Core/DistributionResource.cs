using System.Collections;
using System.Collections.Generic;

namespace Coflnet {
	
	/// <summary>
	/// Special <see cref="Entity"/> which Distributes everyCommand it receives to its members
	/// </summary>
	public class DistributionResource : Entity {
		protected static CommandController _commandController;

		public override CommandController GetCommandController () {
			return _commandController;
		}

		static DistributionResource () {
			_commandController = new CommandController ();
			_commandController.RegisterCommand<Distribute> ();
		}

		public class Distribute : Command {
			public override void Execute (CommandData data) {
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
			public override void Execute (CommandData data) {
				var chat = ReferenceManager.Instance.GetEntity<Chat> (data.rId);
				chat.Access.Subscribers.Add (data.sId);
			}
		}
	}
	*/