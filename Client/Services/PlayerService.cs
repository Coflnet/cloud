﻿using System;
using System.Collections.Generic;

namespace Coflnet.Client
{
	/// <summary>
	/// Contains methods to manage player/users besides the logged in ones eg friends
	/// </summary>
	public class PlayerService
	{
		public static PlayerService Instance { get; set; }

		static PlayerService()
		{
			Instance = new PlayerService();
		}

		public PublicUserInfo GetProfileOf(EntityId id)
		{
			return EntityManager.Instance.GetEntity<PublicUserInfo>(id);
		}

		/// <summary>
		/// Generates a list of public userinfos out of the friend list of the current user
		/// </summary>
		/// <returns>The list.</returns>
		public List<PublicUserInfo> FriendList()
		{
			CoflnetUser currentUser = UserService.Instance.CurrentUser;
			return currentUser.Friends.ConvertAll((Reference<CoflnetUser> input) =>
			{
				PublicUserInfo userInfo;
				EntityManager.Instance.TryGetEntity<PublicUserInfo>(input.EntityId, out userInfo);
				if (userInfo == null)
				{
					userInfo = new PublicUserInfo()
					{
						Id = input.EntityId,
						userName = "unknown"
					};
				}
				return userInfo;
			});
		}
	}

}