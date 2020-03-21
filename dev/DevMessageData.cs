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
					UnityEngine.Debug.Log($"on {sender.core.Id} {sender.core.GetType().Name} {data} ");
					data.CoreInstance = sender.core;
					data.sId = rId;//sender.core.Id;
					sender.core.ReferenceManager.ExecuteForReference(data);
				} else {
					UnityEngine.Debug.Log("normal send back");
					base.SendBack(data);
				}
			}
		}


}