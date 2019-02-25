using System.Linq;

namespace Coflnet
{
	/// <summary>
	/// Is self permission.
	/// Only allows Access if the sender is the resource itself
	/// </summary>
	public class IsSelfPermission : Permission
	{
		public static IsSelfPermission Instance;

		static IsSelfPermission()
		{
			Instance = new IsSelfPermission();
		}


		public override bool CheckPermission(MessageData data, Referenceable target)
		{
			return data.sId == target.Id;
		}

		public override string GetSlug()
		{
			return "isSelf";
		}
	}

}