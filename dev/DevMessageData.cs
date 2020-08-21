namespace Coflnet.Dev
{
    public class DevCommandData : ServerCommandData
		{
			// TODO
			public SimulationInstance sender;

			public DevCommandData(CommandData normal,SimulationInstance sender = null) : base(normal)
			{
				this.sender = sender;
			}

			public override void SendBack(CommandData data)
			{
				if(sender != null){
										data.CoreInstance = sender.core;
					data.SenderId = Recipient;//sender.core.Id;
					sender.core.EntityManager.ExecuteForReference(data);
				} else {
										base.SendBack(data);
				}
			}
		}


}