using System.Collections;
using System.Collections.Generic;


namespace Coflnet.Client
{
	public class ServerController : Coflnet.ServerController
	{
		protected long managingServerId;

		public static new ServerController Instance { get; }

		public void SendToServer(CommandData data)
		{
			SendCommandToServer(data, managingServerId);
		}

		static ServerController()
		{
			Instance = new ServerController();
			Instance.managingServerId = ConfigController.UserSettings.managingServers[0];
		}
	}
}


