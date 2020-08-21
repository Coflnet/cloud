using System.Linq;

namespace Coflnet {
	public class CanChangePermissionPermission : Permission {
		public static CanChangePermissionPermission Instance;

		static CanChangePermissionPermission () {
			Instance = new CanChangePermissionPermission ();
		}

		public override bool CheckPermission (CommandData data, Entity target) {
			return target.IsAllowedAccess (data.SenderId, AccessMode.CHANGE_PERMISSIONS);

		}

		public override string Slug {
			get {

				return "canChangePermission";
			}
		}
	}

}