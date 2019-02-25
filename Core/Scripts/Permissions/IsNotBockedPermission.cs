using System.Linq;

namespace Coflnet
{
	public class IsNotBockedPermission : Permission
	{
		public static IsNotBockedPermission Instance;

		static IsNotBockedPermission()
		{
			Instance = new IsNotBockedPermission();
		}


		public override bool CheckPermission(MessageData data, Referenceable target)
		{
			var user = target as CoflnetUser;
			if (user == null)
			{
				UnityEngine.Debug.Log("target isn't a user");
				return false;
			}
			return !user.IsBlocked(new Reference<CoflnetUser>(data.sId));

		}

		public override string GetSlug()
		{
			return "isBlocked";
		}
	}

}