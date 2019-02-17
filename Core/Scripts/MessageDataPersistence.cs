using System.Collections.Generic;
using System.Linq;

namespace Coflnet
{
	/// <summary>
	/// Persists Message data
	/// </summary>
	public class MessageDataPersistence
	{
		public static MessageDataPersistence Instance;


		static MessageDataPersistence()
		{
			Instance = new MessageDataPersistence();
		}

		/// <summary>
		/// Gets all messages for.
		/// </summary>
		/// <returns>The messages for a resource.</returns>
		/// <param name="id">Identifier.</param>
		public IEnumerable<MessageData> GetMessagesFor(SourceReference id)
		{
			foreach (var item in DataController.Instance.LoadObject<MessageData[]>("datas" + id.ToString()))
			{
				yield return item;
			}
		}

		/// <summary>
		/// Saves the message.
		/// </summary>
		/// <param name="messageData">Message data.</param>
		public void SaveMessage(MessageData messageData)
		{
			var loaded = GetMessagesFor(messageData.rId).ToArray();

			DataController.Instance.SaveObject("datas" + messageData.rId.ToString(), loaded);
		}
	}
}