using System.Collections.Generic;
using MessagePack;

namespace Coflnet
{
	/// <summary>
	/// Inner reference used for serializing the reference object with additional data
	/// </summary>
	[MessagePackObject]
	public class InnerReference<T> where T : Entity
	{
		/// <summary>
		/// Gets or sets the inner resource.
		/// This is a workaround because it is easier to serialize <see cref="object"/> 
		/// than derived classes of <see cref="Entity"/> 
		/// </summary>
		/// <value>The inner resource.</value>
		[Key(0)]
		public object InnerResource { get; set; }

		[IgnoreMember]
		public virtual T Resource
		{
			get
			{
				return InnerResource as T;
			}
			set
			{
				InnerResource = value;
			}
		}

		public InnerReference() { }

		public InnerReference(T entity)
		{
			Resource = entity;
		}


		/// <summary>
		/// Executes a command on the server containing the resource referenced by this object
		/// </summary>
		/// <param name="data">Command data to send</param>
		/// <param name="changingCommand">Wherever or not this command is changing</param>
		public virtual void ExecuteForEntity(CommandData data, bool changingCommand = true)
		{
			CoflnetCore.Instance.SendCommand(data, Resource.Id.ServerId);
		}

		public virtual bool IsAllowedToAccess(EntityId requestingReference, AccessMode mode = AccessMode.READ)
		{
			return Resource.IsAllowedAccess(requestingReference, mode);
		}
	}


	/// <summary>
	/// Redundant Inner reference used for serializing the reference object with additional data.
	/// Includes siblings (failover) nodes.
	/// </summary>
	[MessagePackObject]
	public class RedundantInnerReference<T> : InnerReference<T> where T : Entity
	{
		/// TODO add list of local subscribed <see cref="ReceiveableResource"/>
	// <summary>
	/// Currently updates are not distributed to end devices
	/// </summary>
	/// <value></value>/ 	
		[Key(1)]
		public List<long> SiblingNodes { get; set; }

		public RedundantInnerReference() { }

		public RedundantInnerReference(T entity)
		{
			Resource = entity;
		}


		/// <summary>
		/// Executes a command on the server containing the resource referenced by this object
		/// </summary>
		/// <param name="data">Command data to send</param>
		/// <param name="changingCommand">Wherever or not this command is changing</param>
		public override void ExecuteForEntity(CommandData data, bool changingCommand = true)
		{
			// read commands are different from write commands
			if (changingCommand)
			{
				CoflnetCore.Instance.SendCommand(data, Resource.Id.ServerId);
			}
			else
			{
				// choose one of the sibling nodes if there
				if (SiblingNodes != null && SiblingNodes.Count != 0)
				{
					CoflnetCore.Instance.SendCommand(data, SiblingNodes.GetRandom());
				}
			}
		}


		public override bool IsAllowedToAccess(EntityId requestingReference, AccessMode mode = AccessMode.READ)
		{
			foreach (var nodeId in SiblingNodes)
			{
				// sibling nodes are allowed to do everything
				if (nodeId == requestingReference.ServerId && requestingReference.IsServer)
					return true;
			}

			return Resource.IsAllowedAccess(requestingReference, mode);
		}
	}

	[MessagePackObject]
	public class RedirectReference<T> : InnerReference<T> where T:Entity
	{
		[Key(1)]
		public EntityId newId;

        
    }

	public class RedirectEntity : Entity
        {
			public EntityId newId;

            public override CommandController GetCommandController()
            {
                return EntityManager.Instance.GetEntity<Entity>(newId).GetCommandController();
            }
        }
}