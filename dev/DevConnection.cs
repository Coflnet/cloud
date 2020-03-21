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
        public List<SourceReference> AuthenticatedIds { get;set; }

        public CoflnetEncoder Encoder => CoflnetEncoder.Instance;

        public Dictionary<SourceReference, Token> Tokens{get;set;}

        public void SendBack(MessageData data)
        {
			var temp = data.rId;
			data.rId = data.sId;
			data.sId = temp;
			UnityEngine.Debug.Log("sending now to " + data.rId);
            DevCore.Instance.SendCommand(data);
        }
    }


}