using Coflnet;
using MessagePack;

namespace Coflnet
{
	/// <summary>
	/// Can't be a ServerCommand because no user yet exists
	/// </summary>   
	public class ReceiveConfirm : Command
	{
		public override void Execute(MessageData data)
		{
			var dataParams = data.GetAs<ReceiveConfirmParams>();
			UnityEngine.Debug.Log($"deleting message {MessagePackSerializer.ToJson(dataParams.messageId)} from {dataParams.sender} for " + MessagePackSerializer.ToJson(data.sId));

			MessageDataPersistence.Instance.Remove(data.sId, dataParams.sender, dataParams.messageId);
		}

		public override CommandSettings GetSettings()
		{
			return new CommandSettings();
		}

		public override string GetSlug()
		{
			return "receivedCommand";
		}
	}


	[MessagePackObject]
	public class ReceiveConfirmParams
	{
		[Key(0)]
		public SourceReference sender;
		[Key(1)]
		public long messageId;

		public ReceiveConfirmParams(SourceReference sender, long messageId)
		{
			this.sender = sender;
			this.messageId = messageId;
		}

		public ReceiveConfirmParams() { }
	}

}

