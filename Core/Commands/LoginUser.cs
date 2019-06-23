using Coflnet;
using MessagePack;

namespace Coflnet {
	public class LoginUser : Command {
		public override void Execute (MessageData data) {
			throw new CommandExistsOnServer ();
		}

		public override CommandSettings GetSettings () {
			return new CommandSettings ();
		}

		public override string Slug {
			get {

				return "loginUser";
			}
		}
	}

	public class LoginUserResponse : Command {
		public override void Execute (MessageData data) {
			ConfigController.ActiveUserId = data.GetAs<SourceReference> ();
		}

		public override CommandSettings GetSettings () {
			return new CommandSettings (IsManagingServerPermission.Instance);
		}

		public override string Slug {
			get {

				return "loginUserResponse";
			}
		}
	}

	[MessagePackObject]
	public class LoginParams {
		[Key (0)]
		public SourceReference id;
		[Key (1)]
		public byte[] secret;

		public LoginParams (SourceReference id, byte[] secret) {
			this.id = id;
			this.secret = secret;
		}

		public LoginParams () { }
	}
}