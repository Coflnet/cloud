using System.Collections.Generic;
using System.Linq;

namespace Coflnet
{
	/// <summary>
	/// Persists Message data
	/// </summary>
	public class CommandDataPersistence
	{
		public static CommandDataPersistence Instance;


		static CommandDataPersistence()
		{
			Instance = new CommandDataPersistence();
		}

		/// <summary>
		/// Gets all messages for.
		/// </summary>
		/// <returns>The messages for a resource.</returns>
		/// <param name="id">Identifier.</param>
		public virtual IEnumerable<CommandData> GetMessagesFor(EntityId id)
		{
			foreach (var item in DataController.Instance.LoadObject<CommandData[]>(Path(id)))
			{
				yield return item;
			}
		}

		/// <summary>
		/// Saves the message.
		/// </summary>
		/// <param name="commandData">Message data.</param>
		public virtual void SaveMessage(CommandData commandData)
		{
			var loaded = GetMessagesFor(commandData.Recipient).ToArray();
			loaded.Append(commandData);

			DataController.Instance.SaveObject(Path(commandData.Recipient), loaded);
		}

		/// <summary>
		/// Remove the specified message with matching sender and id if present.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="id">Identifier.</param>
		public virtual void Remove(EntityId receipient, EntityId sender, long id)
		{
			DataController.Instance.RemoveFromFile<CommandData>(Path(receipient), m => m.MessageId == id && m.SenderId == sender);
		}


		/// <summary>
		/// Gets all Messages and tries to send them
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<CommandData> GetAllUnsent()
		{
			yield break;
		}

		private string Path(EntityId rId)
		{
			return "datas/" + rId.ToString();
		}
	}
}