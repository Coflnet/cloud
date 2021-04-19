using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Coflnet
{
    [DataContract]
	public class Access {
		[DataMember]
		public EntityId Owner;

		/// <summary>
		/// enum values are given by
		/// same server, same location, everybody
		/// 1 = read
		/// 2 = write
		/// 4 = change access
		/// For any combination add them together
		/// </summary>
		public enum GeneralAccess {
			NONE = 0,
			SERVER_READ = 1,
			SERVER_WRITE = 2,
			LOCAL_READ = 0x1 << 3,
			LOCAL_WRITE = 0x2 << 3,

			ALL_READ= 0x1 << 6,
			ALL_WRITE = 0x2 << 6,
			
			ALL_READ_LOCAL_WRITE = ALL_READ | LOCAL_WRITE,
			ALL_READ_AND_WRITE = ALL_READ | ALL_WRITE,
			LOCAL_READ_AND_WRITE = LOCAL_READ | LOCAL_WRITE,
			ALL_READ_LOCAL_READ_AND_WRITE = ALL_READ_LOCAL_WRITE | LOCAL_WRITE,
			SERVER_READ_AND_WRITE = SERVER_READ | SERVER_WRITE,
			ALL_READ_SERVER_READ_AND_WRITE = SERVER_READ_AND_WRITE | ALL_READ
		}

		[DataMember]
		public GeneralAccess generalAccess;
		[DataMember]
		public Dictionary<EntityId, AccessMode> resourceAccess;
		/// <summary>
		/// SourceReferences which want to be notified if this resource changes
		/// </summary>
		/// <value>The subscribers.</value>
		[DataMember(Name="subs")]
		public List<EntityId> Subscribers { get; set; }

		public Access (EntityId owner) {
			Owner = owner;
		}

		public Access()
		{
			
		}


		/// <summary>
		/// Checks if some reference has some level of access to this resource
		/// </summary>
		/// <returns><c>true</c>, if allowed to access, <c>false</c> otherwise.</returns>
		/// <param name="requestingReference">Requesting reference.</param>
		/// <param name="mode">AccessMode.</param>
		/// <param name="target">The resource in question</param>
		/// <returns></returns>
		public bool IsAllowedToAccess(EntityId requestingReference,EntityId target, AccessMode mode = AccessMode.READ)
		{
			return IsAllowedToAccess(requestingReference,mode,target);
		}

		/// <summary>
		/// Checks if some reference has some level of access to this resource
		/// </summary>
		/// <returns><c>true</c>, if allowed to access, <c>false</c> otherwise.</returns>
		/// <param name="requestingReference">Requesting reference.</param>
		/// <param name="mode">AccessMode.</param>
		/// <param name="target">The resource in question, this may be required since since An Access can be used for multiple Resources.</param>
		public bool IsAllowedToAccess (EntityId requestingReference, AccessMode mode = AccessMode.READ,EntityId target = default(EntityId)) {

			// unauthorized senders can't access
			if(requestingReference == default(EntityId))
			{
				return false;
			}

			// The owner always has access
			if (requestingReference == Owner)
				return true;
				
			//is there a special case?
			// this allows to give everyone except a few specific Resources access
			if (resourceAccess != null) {
				if(resourceAccess.ContainsKey (requestingReference))
					return resourceAccess[requestingReference].CompareTo (mode) >= 0;
				else if(resourceAccess.ContainsKey (requestingReference.FullServerId))
					return resourceAccess[requestingReference.FullServerId].CompareTo (mode) >= 0;
			} 

			// the resource itself and its server have access by default, 
			// but the servers access can be removed
			if(target.FullServerId == requestingReference || target == requestingReference)
				return true;


			// check for "all"
			if (((((int)generalAccess) >> 6)  & (int) mode) > 0)
				return true;

			// check for local
			if (((((int)generalAccess) >> 3)  & (int) mode) > 0)
				return requestingReference.IsSameLocationAs (Owner);
			// check for same server
			if (((((int)generalAccess))  & (int) mode) > 0)
				return Owner.ServerId == requestingReference.ServerId;

			// requestingReference doesn't have access
			return false;
		}

		public void Authorize (EntityId sourceReference, AccessMode mode = AccessMode.READ) {
			if (resourceAccess == null)
				resourceAccess = new Dictionary<EntityId, AccessMode> ();
			resourceAccess[sourceReference] = mode;
			// also authorize the server
			AccessMode serverMode;
			if(resourceAccess.TryGetValue(sourceReference.FullServerId,out serverMode)){
				// activeate whatever mode new
				resourceAccess[sourceReference.FullServerId] |= mode;
			}
			else {
				resourceAccess[sourceReference.FullServerId] = mode;
			}
		}

		/// <summary>
		/// Subscribes the resource to this one
		/// </summary>
		/// <param name="sourceReference"></param>
		public void Subscribe(EntityId sourceReference)
		{
			if(Subscribers == null){
				Subscribers = new List<EntityId>();
			}
			if(Subscribers.Contains(sourceReference)){
				// already subscribed
				return;
			}
			Subscribers.Add(sourceReference);
		}

		/// <summary>
		/// Unsubscribes
		/// </summary>
		/// <param name="sourceReference"></param>
		public void Unsubscribe(EntityId sourceReference)
		{
			if(Subscribers == null){
				return;
			}
			Subscribers.Remove(sourceReference);
			if(Subscribers.Count == 0){
				Subscribers = null;
			}
		}

        

        public override string ToString()
        {
            var result = $"general:{generalAccess},Owner:{Owner}";
			if(resourceAccess != null){
				result += $",special:[ {string.Join(",", resourceAccess.Select(kv => kv.Key + "=" + kv.Value).ToArray())}]";
			}

			if(Subscribers != null){
				result += $", Subscribers: [{string.Join(",", Subscribers)}]";
			}
			return result;
        }

		/// <summary>
		/// Returns the <see cref="resourceAccess"/> Attribute, will generate a new if it is currently null
		/// </summary>
		/// <returns></returns>
		public Dictionary<EntityId,AccessMode> GetSpecialCases()
		{
			if(resourceAccess == null)
			{
				resourceAccess = new Dictionary<EntityId, AccessMode>();
			}
			return resourceAccess;
		}

        public override bool Equals(object obj)
        {
            var access = obj as Access;
            return access != null &&
                   EqualityComparer<EntityId>.Default.Equals(Owner, access.Owner) &&
                   generalAccess == access.generalAccess &&
                   EqualityComparer<Dictionary<EntityId, AccessMode>>.Default.Equals(resourceAccess, access.resourceAccess) &&
                   EqualityComparer<List<EntityId>>.Default.Equals(Subscribers, access.Subscribers);
        }

        public override int GetHashCode()
        {
            var hashCode = 2127058248;
            hashCode = hashCode * -1521134295 + EqualityComparer<EntityId>.Default.GetHashCode(Owner);
            hashCode = hashCode * -1521134295 + generalAccess.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Dictionary<EntityId, AccessMode>>.Default.GetHashCode(resourceAccess);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<EntityId>>.Default.GetHashCode(Subscribers);
            return hashCode;
        }
    }
}