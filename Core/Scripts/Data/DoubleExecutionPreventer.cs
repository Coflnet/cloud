

using System.Collections.Concurrent;


namespace Coflnet
{
	/// <summary>
	/// Tries to migrate the problem of receiving a command twice but only having to execute it once.
	/// </summary>
	public class DoubleExecutionPreventer
	{
		private ConcurrentDictionary<EntityId, ulong> senderIndex = new ConcurrentDictionary<EntityId, ulong>();
		private ConcurrentDictionary<EntityId, ConcurrentDictionary<ulong, bool>> foundIds;

		/// <summary>
		/// Alreadies the received command.
		/// </summary>
		/// <returns><c>true</c>, if  command was alreadyed received, <c>false</c> otherwise.</returns>
		/// <param name="data">Data.</param>
		public bool AlreadyReceivedCommand(CommandData data)
		{
			ulong index;
			ulong messageId = (ulong)data.MessageId;
			if (senderIndex.TryGetValue(data.SenderId, out index))
			{
				senderIndex.TryAdd(data.SenderId, messageId);
				foundIds.TryAdd(data.SenderId, new ConcurrentDictionary<ulong, bool>());
				foundIds[data.SenderId].TryAdd(messageId, true);
				return false;
			}
			if (index < messageId)
			{
				senderIndex.TryUpdate(data.SenderId, messageId, index);
				foundIds[data.SenderId].TryAdd(messageId, true);
				return false;
			}
			else
			{
				bool found = foundIds[data.SenderId][messageId];
				if (!found)
				{
					foundIds[data.SenderId][messageId] = true;
				}
				return found;
			}
		}
	}
}