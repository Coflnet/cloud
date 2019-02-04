

using System.Collections.Generic;

namespace Coflnet.Server
{
	public class MessagePersistence
	{

		protected string subDirectory = "";

		public static MessagePersistence Instance;


		static MessagePersistence()
		{
			Instance = new MessagePersistence();
		}


		/// <summary>
		/// Save the specified data.
		/// </summary>
		/// <param name="data">Data to save.</param>
		public void Save(MessageData data)
		{
			FileController.AppendLineAs<MessageData>(PathToSource(data.rId), data);
		}

		public IEnumerable<MessageData> MessagesFor(SourceReference id)
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
				yield return null;
		}

		/// <summary>
		/// Deletes all messages for a given Referenceable
		/// </summary>
		/// <param name="id">Identifier.</param>
		public void DeleteMessages(SourceReference id)
		{
			FileController.Delete(PathToSource(id));
		}


		protected string PathToSource(SourceReference id)
		{
			return $"{id.Region}/{id.LocationInRegion}/{id.ServerRelativeToLocation}/{id.ResourceId}";
		}
	}
}
