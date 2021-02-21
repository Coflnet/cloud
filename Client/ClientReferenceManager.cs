
using System;
using Coflnet;

namespace Coflnet.Client {
	public class ClientReferenceManager : EntityManager
    {

        public ClientReferenceManager() : this("res")
		{
		}

        public ClientReferenceManager(string path) : base(path)
		{
		}


		public override void ExecuteForReference(CommandData data, EntityId sender = default(EntityId))
		{
			if (data.Recipient.ServerId == 0) {
				// special case it is myself (0 is local)
				coreInstance.ExecuteCommand (data);
				return;
			}


						InnerReference<Entity> reference;
			TryGetReference (data.Recipient, out reference);

			if (reference == null || reference.Resource == null) {
				// we dont't have it
				return;
			}

			var command = reference
						.Resource
						.GetCommandController ()
						.GetCommand (data.Type);

			// execute the command if it is localPropagation or comming from the managing node
			if(command.Settings.LocalPropagation || IsManagingNodeFor(data.SenderId,data.Recipient) || command.Slug == "response")
			{
				command.Execute(data);
			} else {
				Logger.Error($"Received `{data.Type}` command from {data.SenderId} but thats not the manager ");
			}
		}


        /// <summary>
		/// Creates a new reference.
		/// </summary>
		/// <returns>The reference identifier.</returns>
		/// <param name="entity">Referencable object to store.</param>
		public override EntityId CreateReference(Entity entity, bool force = false)
		{
			// TODO: maybe initiate a request for an server generated unique id

			long nextIndex = ThreadSaveIdGenerator.NextId;
            // as client we are unable to assing uuids so we use 0 indicating it isn't set
			EntityId newReference = new EntityId(0, nextIndex);

			if (entity.Id != new EntityId() && !force)
				throw new Exception("The entity already had an id, it may already have been registered. Call with force = true to ignore");
			entity.Id = newReference;

			InnerReference<Entity> referenceObject = new InnerReference<Entity>(entity);

			if (!AddReference(referenceObject))
				throw new Exception("adding failed");
			return newReference;
		}
    }
}
