using System.Collections.Generic;

namespace Coflnet.Dev
{
    class DevConnection : IClientConnection
    {
        public CoflnetUser User
        {
            get;set;
        }

        public Device Device { get;set; }
        public List<EntityId> AuthenticatedIds { get;set; }

        public CoflnetEncoder Encoder => CoflnetEncoder.Instance;

        public Dictionary<EntityId, Token> Tokens{get;set;}

        public void SendBack(CommandData data)
        {
			var temp = data.Recipient;
			data.Recipient = data.SenderId;
			data.SenderId = temp;
			            DevCore.Instance.SendCommand(data);
        }
    }


}