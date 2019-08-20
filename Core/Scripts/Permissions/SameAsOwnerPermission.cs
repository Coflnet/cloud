using Coflnet;
namespace Coflnet.Core.Permissions
{
	public class SameAsOwnerPermission : Coflnet.Permission
    {
		/// <summary>
		/// An instance of this <see cref="Permission"/> class since usually only one is required.
		/// </summary>
		public static SameAsOwnerPermission Instance;
		static SameAsOwnerPermission () {
			Instance = new SameAsOwnerPermission ();
		}

		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="MessageData"/> passed over the network .</param>
		/// <param name="target">The local <see cref="Referenceable"/> on which to test on .</param>
		public override bool CheckPermission (MessageData data, Referenceable target) {
			return false;// data.CoreInstance.ReferenceManager.GetResource<Referecneable>(target.GetAccess().Owner).IsAllowedAccess (data.sId, AccessMode.READ);
		}

		public override string Slug => "SameAsOwnerPermission";
	}
}