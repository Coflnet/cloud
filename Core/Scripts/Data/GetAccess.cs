namespace Coflnet
{
    /// <summary>
    /// Get the <see cref="Access"/> Property of an object which isn't part of the object itself by default.
    /// </summary>
    public class GetAccess : ReturnCommand {
		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="MessageData"/> passed over the network .</param>
		public override MessageData ExecuteWithReturn (MessageData data) {
			data.SerializeAndSet<Access> (data.GetTargetAs<Referenceable> ().Access);
			return data;
		}

		/// <summary>
		/// Special settings and Permissions for this <see cref="Command"/>
		/// </summary>
		/// <returns>The settings.</returns>
		protected override CommandSettings GetSettings () {
			return new CommandSettings (WritePermission.Instance);
		}
		/// <summary>
		/// Gets the globally unique slug (short human readable id).
		/// </summary>
		/// <returns>The slug .</returns>
		public override string Slug {
			get {

				return "GetAccess";
			}
		}
	}
}