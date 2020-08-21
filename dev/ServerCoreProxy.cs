using Coflnet.Server;

namespace Coflnet.Dev
{
    public class ServerCoreProxy : ServerCore
	{
		public override void SendCommand(CommandData data, long serverId = 0)
		{
			// set the correct sender
			data.SenderId = this.Id;
			// go around the network 
			DevCore.DevInstance.SendCommand(data,serverId);
		}

		public ServerCoreProxy(EntityManager referenceManager) : base(referenceManager)
        {
        }

        public ServerCoreProxy()
        {
        }
    }


}