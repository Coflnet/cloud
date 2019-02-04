using System;


namespace Coflnet
{

	/// <summary>
	/// Is this connection an user permission.
	/// </summary>
	public class IsUserPermission : Permission
	{
		public override bool CheckPermission(MessageData data, Referenceable target)
		{
			return true;
		}

		public override string GetSlug()
		{
			return "isUser";
		}
	}

	public class IsOwnerPermission : Permission
	{
		public override bool CheckPermission(MessageData data, Referenceable target)
		{
			return target.Access.Owner == data.sId;
		}

		public override string GetSlug()
		{
			return "isOwner";
		}
	}

	public class IsDevicePermission : Permission
	{
		public override bool CheckPermission(MessageData data, Referenceable target)
		{
			return true;
		}

		public override string GetSlug()
		{
			return "isDevice";
		}
	}


	public class IsMasterPermission : Permission
	{
		public override bool CheckPermission(MessageData data, Referenceable target)
		{
			return true;
		}

		public override string GetSlug()
		{
			return "isMaster";
		}
	}

}

