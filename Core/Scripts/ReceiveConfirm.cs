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
			UnityEngine.Debug.Log("deleting message :)for " + MessagePackSerializer.ToJson(dataParams.sender));

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
		public long messageId;
		[Key(1)]
		public SourceReference sender;
		private SourceReference sId;
		private long mId;

		public ReceiveConfirmParams(SourceReference sId, long mId)
		{
			this.sId = sId;
			this.mId = mId;
		}

		public ReceiveConfirmParams() { }
	}

}

