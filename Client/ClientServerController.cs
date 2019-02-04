using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Coflnet.Client
{
	public class ServerController : Coflnet.ServerController
	{
		protected long managingServerId;

		public static ServerController Instance { get; }

		public void SendToServer(MessageData data)
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


