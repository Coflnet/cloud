using System.Linq;

namespace Coflnet {
	public class IsInFriendListPermission : Permission {
		public static IsInFriendListPermission Instance;

		static IsInFriendListPermission () {
			Instance = new IsInFriendListPermission ();
		}

		public override bool CheckPermission (CommandData data, Entity target) {
			var user = target as CoflnetUser;
			if (user == null) {
				return false;
			}

			// do we even need to be friends?
			if (!user.OnlyFriendsMessage) {
				return true;
			}

			return user.Friends.Where (friend =>
				friend.EntityId == data.SenderId
			).Any ();

		}

		public override string Slug {
			get {

				return "isInFriendList";
			}
		}
	}
}