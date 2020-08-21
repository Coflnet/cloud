namespace Coflnet
{
    public class RemoveAccess : Command {
		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="CommandData"/> passed over the network .</param>
		public override void Execute (CommandData data) {
			data.GetTargetAs<Entity> ().Access.Authorize (data.GetAs<EntityId> (), AccessMode.NONE);
		}

		/// <summary>
		/// Special settings and Permissions for this <see cref="Command"/>
		/// </summary>
		/// <returns>The settings.</returns>
		protected override CommandSettings GetSettings () {
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