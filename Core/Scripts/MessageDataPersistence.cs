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
		public virtual IEnumerable<MessageData> GetMessagesFor(SourceReference id)
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
		public virtual void SaveMessage(MessageData messageData)
		{
			var loaded = GetMessagesFor(messageData.rId).ToArray();

			DataController.Instance.SaveObject("datas" + messageData.rId.ToString(), loaded);
		}

		/// <summary>
		/// Remove the specified message with matching sender and id if present.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="id">Identifier.</param>
		public virtual void Remove(SourceReference receipient, SourceReference sender, long id)
		{
			DataController.Instance.RemoveFromFile<MessageData>(Path(receipient), m => m.mId == id && m.sId == sender);
		}

		private string Path(SourceReference rId)
		{
			return "datas" + rId.ToString();
		}
	}
}