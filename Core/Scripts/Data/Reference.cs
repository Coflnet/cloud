using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Coflnet
{
	/// <summary>
	/// Strong type for remote objects based on <see cref="SoureReference"/> as <see cref="EntityId"/>
	/// </summary>
	/// <typeparam name="T">The type this Reference represents</typeparam>
    [DataContract (Name = "ref", Namespace = "")]
	public class Reference<T> where T : Entity {
		/// <summary>
		/// Internal reference used for storing referenced object 
		/// if present in memory on the current server
		/// </summary>
		/// <value></value>
		[IgnoreDataMember]
		public InnerReference<T> InnerReference { get; set; }

		/// <summary>
		/// Executes a command on the server containing the resource referenced by this object
		/// </summary>
		/// <param name="data">Command data to send</param>
		public virtual void ExecuteForEntity (CommandData data) {
			EntityManager.Instance.ExecuteForReference (data);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.Server.Reference`1"/>
		/// </summary>
		/// <param name="resource">Resource.</param>
		/*public Reference(T resource, SourceReference referenceId)
		{
			this.resource = resource;
			this.referenceId = referenceId;
			//referenceId = ReferenceManager.Instance.AddReference(resource);
		}*/

		public Reference (T resource) {
			this.InnerReference = new InnerReference<T> () { Resource = resource };
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.Server.Reference`1"/> class without actually knowing the data.
		/// </summary>
		/// <param name="referenceId">Reference identifier.</param>
		public Reference (EntityId referenceId) {
			this.EntityId = referenceId;
		}

		public Reference () {

		}

		[IgnoreDataMember]
		public T Resource {
			get {
				if (InnerReference == null) {
					InnerReference = EntityManager.Instance.GetNewReference<T> (this.EntityId);
				}
				return InnerReference.Resource;
			}
			set {
				InnerReference.Resource = value;
			}
		}

		[DataMember (Name = "id")]
		public EntityId EntityId {
			get;
			set;
		}

		/// <summary>
		/// Gets the referenced resource as some type if present on the server, null otherwise
		/// </summary>
		/// <returns>The referenced data as specific object.</returns>
		/// <typeparam name="Y">The 1st type parameter.</typeparam>
		public Y GetAsIfPresent<Y> () where Y : class {
			return Resource as Y;
		}

		/// <summary>
		/// Is this instance of type Y.
		/// </summary>
		/// <returns>If this reference contains a resource with type Y.</returns>
		/// <typeparam name="Y">The type parameter to test against.</typeparam>
		public bool Is<Y> () {
			return Resource is Y;
		}

        public override bool Equals(object obj)
        {
            var reference = obj as Reference<T>;
            return reference != null &&
                   EqualityComparer<EntityId>.Default.Equals(EntityId, reference.EntityId);
        }

        public override int GetHashCode()
        {
            var hashCode = -1388343608;
            hashCode = hashCode * -1521134295 + EqualityComparer<EntityId>.Default.GetHashCode(EntityId);
            return hashCode;
        }
    }

	[DataContract(Name="sref",Namespace="")]
	public class SecureReference<T> : Reference<T> where T:SecuredResource
	{
		[DataMember(Name="kp")]
		public SigningKeyPair KeyPair;

        public SecureReference(SigningKeyPair keyPair)
        {
            KeyPair = keyPair;
        }

		public SecureReference()
        {
        }

		/// <summary>
		/// Executes a command on the referenced <see cref="SecuredResource"/> 
		/// </summary>
		/// <param name="sender"></param>
		/// <typeparam name="TCommand"></typeparam>
        public virtual void ExecuteForEntity<TCommand>(EntityId sender = default(EntityId)) where TCommand:Command
        {
			Token token;
			if(KeyPair.secretKey == null)
			{
				token = TokenManager.Instance.GetToken(EntityId);
			} else 
			{
				// we can generate a new token
				token = TokenManager.Instance.GenerateNewToken(EntityId, sender, KeyPair);
			}


			CoflnetCore.Instance.SendCommand<TCommand,Token>(
                EntityId,
                token,
                0,
                sender);
        }
    }


}