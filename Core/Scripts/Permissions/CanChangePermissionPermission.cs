using System.Linq;

namespace Coflnet
{
	public class CanChangePermissionPermission : Permission
	{
		public static CanChangePermissionPermission Instance;

		static CanChangePermissionPermission()
		{
			Instance = new CanChangePermissionPermission();
		}


		public override bool CheckPermission(MessageData data, Referenceable target)
		{
			return target.IsAllowedAccess(data.sId, AccessMode.CHANGE_PERMISSIONS);

		}

		public override string GetSlug()
		{
			return "canChangePermission";
		}
	}

}