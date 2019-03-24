﻿using System.Linq;

namespace Coflnet
{
	/// <summary>
	/// Is self permission.
	/// Only allows Access if the sender is the resource itself
	/// </summary>
	public class IsManagingServerPermission : Permission
	{
		public static IsManagingServerPermission Instance;

		static IsManagingServerPermission()
		{
			Instance = new IsManagingServerPermission();
		}


		public override bool CheckPermission(MessageData data, Referenceable target)
		{
			return ConfigController.Users.Find(user => user.userId == target.Id).managingServers.Contains(data.sId.ServerId) && data.sId.ResourceId == 0;
		}

		public override string GetSlug()
		{
			return "isManagingServer";
		}
	}

}