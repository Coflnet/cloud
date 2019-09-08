
using Coflnet.Core;

namespace Coflnet
{
	/// <summary>
	/// Public user info represents a user profile that is public and can be subscribed to.
	/// </summary>
	public class PublicUserInfo : PublicInfo
	{
		public SourceReference profilePicture;
		public string userName;
		public string status;

		public override CommandController GetCommandController()
		{
			throw new System.NotImplementedException();
		}
	}
}