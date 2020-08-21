using System.Collections;
using System.Collections.Generic;


namespace Coflnet.Server.Recover
{
	/// <summary>
	/// Controlls the recovery ṕrocess when a server went down
	/// </summary>
	public class RecoverController
	{

		private List<RecoverProcess> processes;

		public void Recover(long serverId)
		{

		}

		/// <summary>
		/// Collects referenced objects for one particular server.
		/// </summary>
		/// <param name="data">Message data from a recover controller containing the serverId that has to be recovered</param>
		public void CollectReferencesCommand(CommandData data)
		{
			long serverId = data.GetAs<long>();
			ServerController.Instance.SendCommandToServer(new CommandData("recover_response", new byte[0]), serverId);
		}

	}

	public class RecoverProcess
	{
		protected long lastTransactionIndex;
		protected long currentTransactionIndex;

		protected Dictionary<EntityId, Reference<Entity>> references;

		public void Merge(List<Reference<Entity>> toMerge)
		{
			foreach (var item in toMerge)
			{
				if (!references.ContainsKey(item.EntityId))
				{
					references.Add(item.EntityId, item);
				}
			}
		}

		#region Attributes
		public long LastTransactionIndex
		{
			get
			{
				return lastTransactionIndex;
			}
		}

		public long CurrentTransactionIndex
		{
			get
			{
				return currentTransactionIndex;
			}
		}

		public Dictionary<EntityId, Reference<Entity>> References
		{
			get
			{
				return references;
			}
		}
		#endregion
	}
}


