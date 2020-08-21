

using System.Collections.Generic;

namespace Coflnet.Server
{
	/// <summary>
	/// Server version of <see cref="CommandDataPersistence"/> it is faster
	/// </summary>
	public class MessagePersistence : CommandDataPersistence
	{

		protected string subDirectory = "";

		public static MessagePersistence ServerInstance;


		static MessagePersistence()
		{
			ServerInstance = new MessagePersistence();
			Instance = ServerInstance;
		}


		/// <summary>
		/// Save the specified data. Will drop the data if the receiver and sender are the same.
		/// </summary>
		/// <param name="commandData">Data to save.</param>
		public override void SaveMessage(CommandData commandData)
		{
			// messages to oneself won't be saved
			if (commandData.SenderId == commandData.Recipient)
			{
				return;
			}
			var serverData = commandData as ServerCommandData;
			if (serverData == null)
			{
				serverData = new ServerCommandData(commandData);
			}
			FileController.AppendLineAs<ServerCommandData>(PathToSource(commandData.Recipient), serverData);
		}

		public override IEnumerable<CommandData> GetMessagesFor(EntityId id)
		{
			var path = PathToSource(id);
			if (FileController.Exists(path))
			{
				foreach (var item in FileController.ReadLinesAs<CommandData>(PathToSource(id)))
				{
					if (item.Recipient == id)
					{
						yield return item;
					}
				}
			}
			else
				yield break;
		}

		/// <summary>
		/// Deletes all messages for a given <see cref="Entity"/>
		/// </summary>
		/// <param name="id">Identifier.</param>
		public void DeleteMessages(EntityId id)
		{
			FileController.Delete(PathToSource(id));
		}


		public override void Remove(EntityId receipient, EntityId sender, long id)
		{
			DataController.Instance.RemoveFromFile<CommandData>(PathToSource(receipient), m => m.MessageId != id && sender != m.SenderId);
		}


		protected string PathToSource(EntityId id)
		{
			return $"{id.Region}/{id.LocationInRegion}/{id.ServerRelativeToLocation}/{id.LocalId}";
		}
	}
}
