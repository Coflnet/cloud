using Coflnet.Client;

namespace Coflnet.Dev
{
    public class ClientCoreProxy : ClientCore
    {
		private SourceReference _ID;

		public override SourceReference Id {
			get{return _ID;}
			set{_ID = value;}
		}


        public ClientCoreProxy()
        {
        }

        public ClientCoreProxy(CommandController commandController, ClientSocket socket) : base(commandController, socket)
        {
        }

        public ClientCoreProxy(CommandController commandController, ClientSocket socket, ReferenceManager manager) : base(commandController, socket, manager)
        {
        }

		public override void SendCommand(MessageData data, long serverId = 0)
		{
			// set the correct sender
			data.sId = this.Id;
			// go around the network 
			DevCore.DevInstance.SendCommand(data,serverId);
		}

		public override void SendCommand<C, T>(SourceReference receipient, T data, long id = 0, SourceReference sender = default(SourceReference))
		{
			
			DevCore.DevInstance.SendCommand<C,T>(receipient,data,id,sender);
		}
    }


}