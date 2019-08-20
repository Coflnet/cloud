using System.Collections.Generic;

namespace Coflnet
{
    public class GrantAccess : Command {
		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="MessageData"/> passed over the network .</param>
		public override void Execute (MessageData data) {
			var param = data.GetAs<KeyValuePair<SourceReference, AccessMode>> ();
			data.GetTargetAs<Referenceable> ().Access.Authorize (param.Key, param.Value);
		}

		/// <summary>
		/// Special settings and Permissions for this <see cref="Command"/>
		/// </summary>
		/// <returns>The settings.</returns>
		public override CommandSettings GetSettings () {
			return new CommandSettings (CanChangePermissionPermission.Instance);
		}
		/// <summary>
		/// Gets the globally unique slug (short human readable id).
		/// </summary>
		/// <returns>The slug .</returns>
		public override string Slug {
			get {

				return "GrantAccess";
			}
		}
	}
}