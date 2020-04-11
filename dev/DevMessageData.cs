namespace Coflnet.Dev
{
    public class DevMessageData : ServerMessageData
		{
			// TODO
			public SimulationInstance sender;

			public DevMessageData(MessageData normal,SimulationInstance sender = null) : base(normal)
			{
				this.sender = sender;
			}

			public override void SendBack(MessageData data)
			{
				if(sender != null){
										data.CoreInstance = sender.core;
					data.sId = rId;//sender.core.Id;
					sender.core.ReferenceManager.ExecuteForReference(data);
				} else {
										base.SendBack(data);
				}
			}
		}


}