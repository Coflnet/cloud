using Coflnet;
using MessagePack;

namespace Coflnet {
	public class RegisteredUser : Command {
		public override void Execute (MessageData data) {
			UnityEngine.Debug.Log ("yay");
			var response = data.GetAs<RegisterUserResponse> ();
			ConfigController.UserSettings.userId = response.id;
			ConfigController.UserSettings.userSecret = response.secret;

			// Login

		}

		public override CommandSettings GetSettings () {
			return new CommandSettings ();
		}

		public override string Slug {
			get {

				return "registeredUser";
			}
		}
	}
}