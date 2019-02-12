using System;


namespace Coflnet
{

	/// <summary>
	/// Is this connection an user permission.
	/// </summary>
	public class IsUserPermission : Permission
	{
		public static IsUserPermission Instance;

		static IsUserPermission()
		{
			Instance = new IsUserPermission();
		}


		public override bool CheckPermission(MessageData data, Referenceable target)
		{
			return target is CoflnetUser;
		}

		public override string GetSlug()
		{
			return "isUser";
		}
	}

	public class IsOwnerPermission : Permission
	{
		public static IsOwnerPermission Instance;

		static IsOwnerPermission()
		{
			Instance = new IsOwnerPermission();
		}

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
		public static IsDevicePermission Instance;

		static IsDevicePermission()
		{
			Instance = new IsDevicePermission();
		}

		public override bool CheckPermission(MessageData data, Referenceable target)
		{
			return target is Device;
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




	/// <summary>
	/// Or permission used to require not all but just one of two permissions
	/// </summary>
	public class OrPermission : Permission
	{
		private Permission firstPermission;
		private Permission secondPermission;

		public override bool CheckPermission(MessageData data, Referenceable target)
		{
			return firstPermission.CheckPermission(data, target) || secondPermission.CheckPermission(data, target);
		}

		public override string GetSlug()
		{
			return $"{firstPermission.GetSlug()}Or{secondPermission.GetSlug()}";
		}

		public OrPermission(Permission firstPermission, Permission secondPermission)
		{
			this.firstPermission = firstPermission;
			this.secondPermission = secondPermission;
		}
	}

	/// <summary>
	/// Is authenticated permission.
	/// Used to prevent unauthenticated users/devices from executing the command
	/// </summary>
	public class IsAuthenticatedPermission : Permission
	{
		public static IsAuthenticatedPermission Instance;

		static IsAuthenticatedPermission()
		{
			Instance = new IsAuthenticatedPermission();
		}


		public override bool CheckPermission(MessageData data, Referenceable target)
		{
			return data.sId.ServerId != 0;
		}

		public override string GetSlug()
		{
			return "isAuthenticated";
		}
	}
}

