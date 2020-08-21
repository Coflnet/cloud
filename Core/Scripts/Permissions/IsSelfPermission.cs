using System.Linq;

namespace Coflnet {
	/// <summary>
	/// Is self permission.
	/// Only allows Access if the sender is the resource itself
	/// </summary>
	public class IsSelfPermission : Permission {
		public static IsSelfPermission Instance;

		static IsSelfPermission () {
			Instance = new IsSelfPermission ();
		}

		public override bool CheckPermission (CommandData data, Entity target) {
			return data.SenderId == target.Id;
		}

		public override string Slug => "isSelf";
	}
}

