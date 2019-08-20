namespace Coflnet
{
    public class RemoveAccess : Command {
		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="MessageData"/> passed over the network .</param>
		public override void Execute (MessageData data) {
			data.GetTargetAs<Referenceable> ().Access.Authorize (data.GetAs<SourceReference> (), AccessMode.NONE);
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

				return "RemoveAccess";
			}
		}
	}
}