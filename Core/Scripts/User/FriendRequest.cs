
using System.Collections.Generic;

namespace Coflnet
{

	public class FriendRequest
	{
		protected Reference<CoflnetUser> requestingUser;
		protected Reference<CoflnetUser> targetUser;
		// a user can block just the request
		public enum RequestStatus
		{
			awaiting,
			ignored,
			accepted,
			denied,
			blocked
		}

		public RequestStatus status;

		public FriendRequest(Reference<CoflnetUser> requestingUser, Reference<CoflnetUser> targetUser)
		{
			this.requestingUser = requestingUser;
			this.targetUser = targetUser;
		}

		public Reference<CoflnetUser> RequestingUser
		{
			get
			{
				return requestingUser;
			}
		}

		public Reference<CoflnetUser> TargetUser
		{
			get
			{
				return targetUser;
			}
		}

		public RequestStatus Status
		{
			get
			{
				return status;
			}
		}
	}


}