using System.Linq;

namespace Coflnet {
	public class IsNotBockedPermission : Permission {
		public static IsNotBockedPermission Instance;

		static IsNotBockedPermission () {
			Instance = new IsNotBockedPermission ();
		}

		public override bool CheckPermission (CommandData data, Entity target) {
			var user = target as CoflnetUser;
			if (user == null) {
				return false;
			}
			return !user.IsBlocked (new Reference<CoflnetUser> (data.SenderId));

		}

		public override string Slug {
			get {

				return "isBlocked";
			}
		}
	}
}