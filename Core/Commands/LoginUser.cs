using Coflnet;
using MessagePack;

namespace Coflnet {
	public class LoginUser : Command {
		public override void Execute (CommandData data) {
			throw new CommandExistsOnServer ();
		}

		protected override CommandSettings GetSettings () {
			return new CommandSettings ();
		}

		public override string Slug {
			get {

				return "loginUser";
			}
		}
	}

	public class LoginUserResponse : Command {
		public override void Execute (CommandData data) {
			ConfigController.ActiveUserId = data.GetAs<EntityId> ();
		}

		protected override CommandSettings GetSettings () {
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
		public EntityId id;
		[Key (1)]
		public byte[] secret;

		public LoginParams (EntityId id, byte[] secret) {
			this.id = id;
			this.secret = secret;
		}

		public LoginParams () { }
	}
}