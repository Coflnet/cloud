using System.Linq;

namespace Coflnet {
	/// <summary>
	/// Is self permission.
	/// Only allows Access if the sender is the resource itself
	/// </summary>
	public class IsManagingServerPermission : Permission {
		public static IsManagingServerPermission Instance;

		static IsManagingServerPermission () {
			Instance = new IsManagingServerPermission ();
		}

		public override bool CheckPermission (CommandData data, Entity target) {

			return data.SenderId == target.Id.FullServerId ||
				ConfigController.Users.Where (u => u.managingServers != null && u.managingServers.Contains (data.Recipient.ServerId)).Count () != 0 &&
				data.SenderId.LocalId == 0;
		}

		public override string Slug {
			get {

				return "isManagingServer";
			}
		}
	}
}