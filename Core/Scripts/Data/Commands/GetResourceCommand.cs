namespace Coflnet
{
    public class GetResourceCommand : ReturnCommand {
		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="CommandData"/> passed over the network .</param>
		public override CommandData ExecuteWithReturn (CommandData data) {
			data.message = data.CoreInstance.EntityManager.SerializeWithoutLocalInfo (data.Recipient);
			
			
			return data;
			//MessagePack.MessagePackSerializer.Typeless.Serialize()                
		}

		/// <summary>
		/// Special settings and Permissions for this <see cref="Command"/>
		/// </summary>
		/// <returns>The settings.</returns>
		protected override CommandSettings GetSettings () {
			return new CommandSettings (ReadPermission.Instance);
		}
		/// <summary>
		/// Gets the globally unique slug (short human readable id).
		/// </summary>
		/// <returns>The slug .</returns>
		public override string Slug {
			get {

				return "GetResource";
			}
		}
	}
}