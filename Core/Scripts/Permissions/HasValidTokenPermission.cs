using Coflnet;
namespace Coflnet.Core.Permissions
{
	public class HasValidTokenPermission : Permission {
		/// <summary>
		/// An instance of this <see cref="Permission"/> class since usually only one is required.
		/// </summary>
		public static HasValidTokenPermission Instance;
		static HasValidTokenPermission () {
			Instance = new HasValidTokenPermission ();
		}

		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="MessageData"/> passed over the network .</param>
		/// <param name="target">The local <see cref="Referenceable"/> on which to test on .</param>
		public override bool CheckPermission (MessageData data, Referenceable target) {
			var securedRes = target as SecuredResource;

			return  securedRes != null && data.GetAs<Token>().Validate(securedRes.keyPair.publicKey);
		}

		public override string Slug => "HasValidTokenPermission";
	}
}
