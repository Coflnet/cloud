

using System.Collections.Concurrent;


namespace Coflnet
{
	/// <summary>
	/// Tries to migrate the problem of receiving a command twice but only having to execute it once.
	/// </summary>
	public class DoubleExecutionPreventer
	{
		private ConcurrentDictionary<SourceReference, ulong> senderIndex = new ConcurrentDictionary<SourceReference, ulong>();
		private ConcurrentDictionary<SourceReference, ConcurrentDictionary<ulong, bool>> foundIds;

		/// <summary>
		/// Alreadies the received command.
		/// </summary>
		/// <returns><c>true</c>, if  command was alreadyed received, <c>false</c> otherwise.</returns>
		/// <param name="data">Data.</param>
		public bool AlreadyReceivedCommand(MessageData data)
		{
			ulong index;
			ulong messageId = (ulong)data.mId;
			if (senderIndex.TryGetValue(data.sId, out index))
			{
				senderIndex.TryAdd(data.sId, messageId);
				foundIds.TryAdd(data.sId, new ConcurrentDictionary<ulong, bool>());
				foundIds[data.sId].TryAdd(messageId, true);
				return false;
			}
			if (index < messageId)
			{
				senderIndex.TryUpdate(data.sId, messageId, index);
				foundIds[data.sId].TryAdd(messageId, true);
				return false;
			}
			else
			{
				bool found = foundIds[data.sId][messageId];
				if (!found)
				{
					foundIds[data.sId][messageId] = true;
				}
				return found;
			}
		}
	}
}