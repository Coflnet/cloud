using System.Runtime.Serialization;

namespace Coflnet
{
	[DataContract]
	public abstract class SecuredResource : Referenceable
	{
		/// <summary>
		/// Incoming commands may have to have a valid token signed with the private part of this <see cref="KeyPair"/>
		/// The <see cref="KeyPair.secretKey"/> might not be present on the current machine.
		/// </summary>
		[DataMember(Name="kp")]
		public SigningKeyPair keyPair;
	}
}