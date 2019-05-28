﻿using System.Linq;

namespace Coflnet
{
	/// <summary>
	/// Is managing server or self permission.
	/// Only allows Access if the sender is the resource itself or its managing server
	/// </summary>
	public class IsManagingServerOrSelfPermission : Permission
	{
		public static IsManagingServerOrSelfPermission Instance;

		static IsManagingServerOrSelfPermission()
		{
			Instance = new IsManagingServerOrSelfPermission();
		}


		public override bool CheckPermission(MessageData data, Referenceable target)
		{
			return IsManagingServerPermission.Instance.CheckPermission(data,target)|| IsSelfPermission.Instance.CheckPermission(data,target);
		}

		public override string GetSlug()
		{
			return "isManagingServerOrSelf";
		}
	}

}