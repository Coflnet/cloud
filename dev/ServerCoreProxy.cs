using Coflnet.Server;

namespace Coflnet.Dev
{
    public class ServerCoreProxy : ServerCore
	{
		public override void SendCommand(MessageData data, long serverId = 0)
		{
			// set the correct sender
			data.sId = this.Id;
			// go around the network 
			DevCore.DevInstance.SendCommand(data,serverId);
		}

		public ServerCoreProxy(ReferenceManager referenceManager) : base(referenceManager)
        {
        }

        public ServerCoreProxy()
        {
        }
    }


}