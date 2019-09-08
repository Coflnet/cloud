using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Coflnet;

namespace Coflnet.Core 
{

	public class RollbackController {


		/// <summary>
		/// The <see cref="ReferenceManager"/> this controller should work with
		/// </summary>
		public ReferenceManager ReferenceManager;

		private ConcurrentDictionary<SourceReference,List<ObjectBackup>> backups;

		/// <summary>
		/// An instance of <see cref="RollbackController"/> class since usually only one is required.
		/// </summary>
		public static RollbackController Instance;
		static RollbackController () {
			Instance = new RollbackController ();
		}


		/// <summary>
		/// Rolls back an object to the specified msgId
		/// </summary>
		/// <param name="id">The id of the object to reverse</param>
		/// <param name="msgId">The messageId of the command that failed executing on the server</param>
		public void Rollback(SourceReference id, long msgId)
		{
			List<ObjectBackup> list;

			if(backups.TryGetValue(id,out list))
			{
				var lastState = list.Find(e=>e.msgId == msgId);

				ReferenceManager.ReplaceResource(
					ReferenceManager.GetResource<Referenceable>(
						lastState.backupObject));
			}
		}

		/// <summary>
		/// Adds a new rollback-point
		/// </summary>
		/// <param name="object">The old object state to save</param>
		/// <param name="msgId">The messageId of the command that will be executed next</param>
		public void Add(Referenceable @object, long msgId)
		{
			var copy = ReferenceManager.CopyResource(@object);

			var backup = new ObjectBackup(msgId,@object.Id,copy.Id);
			

			var list = backups.GetOrAdd(@object.Id,
						sr=>new List<ObjectBackup>());

			list.Add(backup);
		}

		/// <summary>
		/// Removes the rollback-point
		/// </summary>
		/// <param name="id">The id of the target object</param>
		/// <param name="msgId">The messageId of the command that got accepted by the server</param>
		public void Remove(SourceReference id,long msgId)
		{
			List<ObjectBackup> list;
			if(backups.TryGetValue(id,out list))
			{
				list.RemoveAll(element=>element.msgId == msgId);
			}
		}


		/// <summary>
		/// Saves the current state
		/// </summary>
		public void Save(DataController dc)
		{
			dc.SaveObject("rollbackLedger",backups);
		}

		public RollbackController()
		{
			backups = DataController.Instance.LoadObject<
						ConcurrentDictionary<SourceReference,List<ObjectBackup>>>(
							"rollbackLedger",
							()=>new ConcurrentDictionary<SourceReference,List<ObjectBackup>>());
			
			DataController.Instance.RegisterSaveCallback(Save);
		}


		private class ObjectBackup
		{
			public long msgId;
			public SourceReference targetObject;
			public SourceReference backupObject;

            public ObjectBackup(long msgId, SourceReference targetObject, SourceReference backupObject)
            {
                this.msgId = msgId;
                this.targetObject = targetObject;
                this.backupObject = backupObject;
            }
        }
	}
	
}