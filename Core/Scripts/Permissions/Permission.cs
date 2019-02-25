using System.Collections;
using System.Collections.Generic;

namespace Coflnet
{
	public abstract class Permission
	{
		public abstract string GetSlug();
		/// <summary>
		/// Checks for a permission.
		/// </summary>
		/// <returns><c>true</c>, if permission was checked, <c>false</c> otherwise.</returns>
		/// <param name="target">The target object</param>
		public abstract bool CheckPermission(MessageData data, Referenceable target);

		public override bool Equals(object obj)
		{
			var item = obj as Permission;

			if (item == null)
			{
				return false;
			}

			return this.GetSlug() == item.GetSlug();
		}

		public override int GetHashCode()
		{
			return this.GetSlug().GetHashCode();
		}
	}

}
