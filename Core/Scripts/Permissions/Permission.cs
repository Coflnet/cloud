using System.Collections;
using System.Collections.Generic;

namespace Coflnet
{
	public abstract class Permission
	{
		public virtual string Slug {
			get
			{
				return this.GetType().Name;
			} 
		}
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

			return this.Slug == item.Slug;
		}

		public override int GetHashCode()
		{
			return this.Slug.GetHashCode();
		}
	}

}
