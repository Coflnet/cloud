using System;
using System.Collections.Generic;
using Coflnet.Client;
using Coflnet.Server;

namespace Coflnet.Dev {
	/// <summary>
	/// Development Core for testing purposses.
	/// It simulates A client alongside with a server.
	/// </summary>
	public class DevCore : CoflnetCore {
		private ClientCore clientCore;
		private ServerCore serverCore;

		/// <summary>
		/// This is the user and the serverid simulated
		/// </summary>
		private SourceReference userId;

		static DevCore () {

		}

		private class DummyPrivacyScreen : IPrivacyScreen {
			public void ShowScreen (Action<int> whenDone) {
				return;
			}
		}

		/// <summary>
		/// Will add default screens
		/// </summary>
		public static void SetupDefaults () {
			// configure cores
			PrivacyService.Instance.privacyScreen = new DummyPrivacyScreen ();
		}

		public static void Init (SourceReference id, bool preventDefaultSetup = false) {
			ConfigController.ActiveUserId = id;
			ConfigController.ApplicationSettings.id = new SourceReference (id.ServerId, 0);

			if (!preventDefaultSetup) {
				SetupDefaults ();
			}

			// to have instances loaded
			var toLoadInstance = ClientCore.ClientInstance;
			var serverInstance = ServerCore.ServerInstance;
			CoflnetCore.Instance = new DevCore ();

			ServerCore.Init ();
			ClientCore.Init ();

		}

		public override CommandController GetCommandController () {
			return globalCommands;
		}

		private static int executionCount = 0;

		/// <summary>
		/// Will execute commands directly as the server, except the receipient is the <see cref="userId">
		/// </summary>
		/// <param name="data">Data to send</param>
		/// <param name="serverId">optional serverId, ignored in this implementation</param>
		public override void SendCommand (MessageData data, long serverId = 0) {

			if (executionCount > 100) {
				throw new Exception ($"to many commands, probalby a loop {data}");
			}
			executionCount++;

			// set correct sender
			if (data.rId == userId) {

				data.sId = new SourceReference (data.rId.ServerId, 0);
			} else {
				
				var serverData =  new ServerMessageData(data);
				serverData.Connection = new DevConnection();
				data = serverData;
				data.sId = userId;
			}

			// special cases register and login
			// client has no id to that point
			if (data.t == "registeredUser") {
				ConfigController.ActiveUserId = data.GetAs<RegisterUserResponse> ().id;
				userId = ConfigController.ActiveUserId;
				ClientCore.ClientInstance.Id = ConfigController.ActiveUserId;
				ReferenceManager.Instance.GetResource (userId)
					.GetCommandController ()
					.AddBackfall (ClientCore.ClientInstance.GetCommandController ());
			}

			if (data.t == "registeredUser" || data.t == "loginUserResponse") {
				data.rId = ConfigController.ActiveUserId;
			}

			UnityEngine.Debug.Log ($"executing for reference {data.t}");
			if (data.rId == ConfigController.ActiveUserId || data.rId == ConfigController.ApplicationSettings.id)
				ReferenceManager.Instance.ExecuteForReference (data);
			else {
				throw new Exception ($"the target {data.rId} is not registered in the development enviroment {data.t}");
			}
		}

		public override void SendCommand<C, T> (SourceReference receipient, T data, long id = 0) {
			UnityEngine.Debug.Log ($"executing for ");
			var commandInstance = ((C) Activator.CreateInstance (typeof (C)));

			var messageData = MessageData.SerializeMessageData<T> (data, commandInstance.GetSlug (), id);

			messageData.rId = receipient;

			if (receipient.ServerId == this.Id.ServerId && commandInstance.Settings.LocalPropagation) {

				ThreadController.Instance.ExecuteCommand (commandInstance, messageData);
			}

			SendCommand (messageData);
		}

		public override void SendCommand<C> (SourceReference receipient, byte[] data) {
			var commandInstance = ((C) Activator.CreateInstance (typeof (C)));
			var messageData = new MessageData (receipient, data, commandInstance.GetSlug ());

			SendCommand (messageData);
		}
	}

    class DevConnection : IClientConnection
    {
        public CoflnetUser User
        {
            get;set;
        }

        public Device Device { get;set; }
        public List<SourceReference> AuthenticatedIds { get;set; }

        public CoflnetEncoder Encoder => CoflnetEncoder.Instance;

        public void SendBack(MessageData data)
        {
            DevCore.Instance.SendCommand(data);
        }
    }
}