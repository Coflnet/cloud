

using System.Collections.Generic;

namespace Coflnet.Server
{
	/// <summary>
	/// Server version of <see cref="MessageDataPersistence"/> it is faster
	/// </summary>
	public class MessagePersistence : MessageDataPersistence
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
		/// <param name="messageData">Data to save.</param>
		public override void SaveMessage(MessageData messageData)
		{
			// messages to oneself won't be saved
			if (messageData.sId == messageData.rId)
			{
				return;
			}
			var serverData = messageData as ServerMessageData;
			if (serverData == null)
			{
				serverData = new ServerMessageData(messageData);
			}
			FileController.AppendLineAs<ServerMessageData>(PathToSource(messageData.rId), serverData);
		}

		public override IEnumerable<MessageData> GetMessagesFor(SourceReference id)
		{
			var path = PathToSource(id);
			if (FileController.Exists(path))
			{
				foreach (var item in FileController.ReadLinesAs<MessageData>(PathToSource(id)))
				{
					if (item.rId == id)
					{
						yield return item;
					}
				}
			}
			else
				yield break;
		}

		/// <summary>
		/// Deletes all messages for a given Referenceable
		/// </summary>
		/// <param name="id">Identifier.</param>
		public void DeleteMessages(SourceReference id)
		{
			FileController.Delete(PathToSource(id));
		}


		public override void Remove(SourceReference receipient, SourceReference sender, long id)
		{
			DataController.Instance.RemoveFromFile<MessageData>(PathToSource(receipient), m => m.mId != id && sender != m.sId);
		}


		protected string PathToSource(SourceReference id)
		{
			return $"{id.Region}/{id.LocationInRegion}/{id.ServerRelativeToLocation}/{id.ResourceId}";
		}
	}
}
