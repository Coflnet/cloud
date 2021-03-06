﻿using System;

namespace Coflnet {

	/// <summary>
	/// Is this connection an user permission.
	/// </summary>
	public class IsUserPermission : Permission {
		public static IsUserPermission Instance;

		static IsUserPermission () {
			Instance = new IsUserPermission ();
		}

		public override bool CheckPermission (CommandData data, Entity target) {
			return target is CoflnetUser;
		}

		public override string Slug => "isUser";

	}

	public class IsOwnerPermission : Permission {
		public static IsOwnerPermission Instance;

		static IsOwnerPermission () {
			Instance = new IsOwnerPermission ();
		}

		public override bool CheckPermission (CommandData data, Entity target) {
			return target.Access.Owner == data.SenderId;
		}

		public override string Slug => "isOwner";

	}

	public class ReadPermission : Permission {
		public static ReadPermission Instance;

		static ReadPermission () {
			Instance = new ReadPermission ();
		}

		public override bool CheckPermission (CommandData data, Entity target) {
			return target.IsAllowedAccess (data.SenderId, AccessMode.READ);
		}

		public override string Slug => "readPermission";

	}

	public class WritePermission : Permission {
		public static WritePermission Instance;

		static WritePermission () {
			Instance = new WritePermission ();
		}

		public override bool CheckPermission (CommandData data, Entity target) {
			return target != null && target.IsAllowedAccess (data.SenderId, AccessMode.WRITE);
		}

		public override string Slug => "writePermission";

	}


	public class IsDevicePermission : Permission {
		public static IsDevicePermission Instance;

		static IsDevicePermission () {
			Instance = new IsDevicePermission ();
		}

		public override bool CheckPermission (CommandData data, Entity target) {
			return target is Device;
		}

		public override string Slug => "isDevice";

	}

	public class IsMasterPermission : Permission {
		public override bool CheckPermission (CommandData data, Entity target) {
			return true;
		}

		public override string Slug {
			get {

				return "isMaster";
			}
		}
	}

	/// <summary>
	/// Or permission used to require not all but just one of two permissions
	/// </summary>
	public class OrPermission : Permission {
		private Permission firstPermission;
		private Permission secondPermission;

		public override bool CheckPermission (CommandData data, Entity target) {
			return firstPermission.CheckPermission (data, target) || secondPermission.CheckPermission (data, target);
		}

		public override string Slug => $"{firstPermission.Slug}Or{secondPermission.Slug}";

		public OrPermission (Permission firstPermission, Permission secondPermission) {
			this.firstPermission = firstPermission;
			this.secondPermission = secondPermission;
		}
	}

	/// <summary>
	/// Is authenticated permission.
	/// Used to prevent unauthenticated users/devices from executing the command
	/// </summary>
	public class IsAuthenticatedPermission : Permission {
		public static IsAuthenticatedPermission Instance;

		static IsAuthenticatedPermission () {
			Instance = new IsAuthenticatedPermission ();
		}

		public override bool CheckPermission (CommandData data, Entity target) {
			return data.SenderId.ServerId != 0;
		}

		public override string Slug => "isAuthenticated";

	}
}