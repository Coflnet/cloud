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

		public override bool CheckPermission (MessageData data, Referenceable target) {

			return data.sId == target.Id.FullServerId ||
				ConfigController.Users.Where (u => u.managingServers != null && u.managingServers.Contains (data.rId.ServerId)).Count () != 0 &&
				data.sId.ResourceId == 0;
		}

		public override string Slug {
			get {

				return "isManagingServer";
			}
		}
	}
}