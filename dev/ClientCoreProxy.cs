using Coflnet.Client;

namespace Coflnet.Dev
{
    public class ClientCoreProxy : ClientCore
    {
		private EntityId _ID;

		public override EntityId Id {
			get{return _ID;}
			set{_ID = value;}
		}


        public ClientCoreProxy()
        {
        }

        public ClientCoreProxy(CommandController commandController, ClientSocket socket) : base(commandController, socket)
        {
        }

        public ClientCoreProxy(CommandController commandController, ClientSocket socket, EntityManager manager) : base(commandController, socket, manager)
        {
        }

		public override void SendCommand(CommandData data, long serverId = 0)
		{
			// set the correct sender
			data.SenderId = this.Id;
			// go around the network 
			DevCore.DevInstance.SendCommand(data,serverId);
		}

		public override void SendCommand<C, T>(EntityId receipient, T data, long id = 0, EntityId sender = default(EntityId))
		{
			
			DevCore.DevInstance.SendCommand<C,T>(receipient,data,id,sender);
		}
    }


}