
using System;
using Coflnet;

namespace Coflnet.Client {
	public class ClientReferenceManager : ReferenceManager
    {

        public ClientReferenceManager() : this("res")
		{
		}

        public ClientReferenceManager(string path) : base(path)
		{
		}


		public override void ExecuteForReference(MessageData data, SourceReference sender = default(SourceReference))
		{
			if (data.rId.ServerId == 0) {
				// special case it is myself (0 is local)
				coreInstance.ExecuteCommand (data);
				return;
			}

			UnityEngine.Debug.Log ($"searching {coreInstance.Id} (ClientRefmanager) for {data.rId}");
			InnerReference<Referenceable> reference;
			TryGetReference (data.rId, out reference);

			if (reference == null || reference.Resource == null) {
				// we dont't have it
				return;
			}

			// execute the command if it is localPropagation
			var command = reference
						.Resource
						.GetCommandController ()
						.GetCommand (data.t);

			if(command.Settings.LocalPropagation)
			{
				command.Execute(data);
			}
		}


        /// <summary>
		/// Creates a new reference.
		/// </summary>
		/// <returns>The reference identifier.</returns>
		/// <param name="referenceable">Referencable object to store.</param>
		public override SourceReference CreateReference(Referenceable referenceable, bool force = false)
		{
			// TODO: maybe initiate a request for an server generated unique id

			long nextIndex = ThreadSaveIdGenerator.NextId;
            // as client we are unable to assing uuids so we use 0 indicating it isn't set
			SourceReference newReference = new SourceReference(0, nextIndex);

			if (referenceable.Id != new SourceReference() && !force)
				throw new Exception("The referenceable already had an id, it may already have been registered. Call with force = true to ignore");
			referenceable.Id = newReference;

			InnerReference<Referenceable> referenceObject = new InnerReference<Referenceable>(referenceable);

			if (!AddReference(referenceObject))
				throw new Exception("adding failed");
			return newReference;
		}
    }
}
